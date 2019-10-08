using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class UDPConnection
{
    private struct DataReceived
    {
        public byte[] data;
        public IPEndPoint ipEndPoint;
    }

    private readonly UdpClient connection;
    private IDataReceiver receiver;

    private Queue<DataReceived> dataReceivedQueue = new Queue<DataReceived>();

    object handler = new object();

    public UDPConnection(int port, IDataReceiver receiver)
    {
        connection = new UdpClient(port);

        this.receiver = receiver;

        connection.BeginReceive(OnReceive, null);
    }

    public UDPConnection(IPAddress ip, int port, IDataReceiver receiver)
    {
        connection = new UdpClient();
        connection.Connect(ip, port);

        this.receiver = receiver;

        connection.BeginReceive(OnReceive, null);
    }

    public void Close()
    {
        connection.Close();
    }

    private void OnReceive(IAsyncResult ar)
    {
        DataReceived dataReceived = new DataReceived();
        dataReceived.data = connection.EndReceive(ar, ref dataReceived.ipEndPoint);

        connection.BeginReceive(OnReceive, null);

        lock (handler)
        {
            dataReceivedQueue.Enqueue(dataReceived);
        }
    }

    public void FlushReceivedData()
    {
        lock (handler)
        {
            while (dataReceivedQueue.Count > 0)
            {
                DataReceived dataReceived = dataReceivedQueue.Dequeue();
                if (receiver != null)
                {
                    receiver.OnReceiveData(dataReceived.data, dataReceived.ipEndPoint);
                }
            }
        }
    }

    public void Send(byte[] data)
    {
        connection.Send(data, data.Length);
    }

    public void Send(byte[] data, IPEndPoint ip)
    {
        connection.Send(data, data.Length, ip);
    }
}
