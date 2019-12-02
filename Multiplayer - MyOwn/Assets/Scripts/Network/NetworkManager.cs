using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Text;

public class NetworkManager : MonoBehaviour, IDataReceiver
{
    public static NetworkManager instance;

    private void Awake()
    {
        if (instance && instance != this)
            Destroy(this);
        else
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
    }

    private bool isServer;
    public bool IsServer { get => isServer; }
    private int port;
    public int Port { get => port; }
    private IPAddress ipAddress;
    public IPAddress IpAddress { get => ipAddress; }

    public Action<byte[], IPEndPoint> onReceiveEvent;

    private UDPConnection connection;

    private readonly Dictionary<uint, Client> clients = new Dictionary<uint, Client>();

    public void StartServer(int port)
    {
        isServer = true;
        this.port = port;
        connection = new UDPConnection(port, this);
    }

    public void StartClient(IPAddress ip, int port)
    {
        isServer = false;
        this.port = port;
        this.ipAddress = ip;
        connection = new UDPConnection(ip, port, this);

        ConnectionManager.instance.SendConnectionRequest();
    }

    public void DisconnectFromServer()
    {
        connection = null;
    }

    public void AddClient(Client client)
    {
        clients.Add(client.id, client);
    }

    public void OnReceiveData(byte[] data, IPEndPoint ip)
    {
        if (onReceiveEvent != null)
            onReceiveEvent.Invoke(data, ip);
    }

    public void SendToServer(byte[] data)
    {
        connection.Send(data);
    }

    public void SendToClient(byte[] data, IPEndPoint ip)
    {
        connection.Send(data, ip);
    }

    public void Broadcast(byte[] data)
    {
        using (var iterator = clients.GetEnumerator())
        {
            while (iterator.MoveNext())
            {   
                connection.Send(data, iterator.Current.Value.ipEndPoint);
            }
        }
    }

    public IPEndPoint GetClientIpById(uint id)
    {
        if (clients.ContainsKey(id))
            return clients[id].ipEndPoint;
        else
            return null;
    }

    private void Update()
    {
        if (connection != null)
            connection.FlushReceivedData();
    }
}
