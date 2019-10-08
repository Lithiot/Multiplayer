using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System;

public class PacketManager : MonoBehaviour, IDataReceiver
{
    public static PacketManager instance;

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

    private uint currentPacketId = 0;
    private Dictionary<uint, Action<ushort, Stream, IPEndPoint>> OnPacketReceived = new Dictionary<uint, System.Action<ushort, Stream, IPEndPoint>>();

    private void Start()
    {
        NetworkManager.instance.onReceiveEvent += OnReceiveData;
    }

    public void AddListener(uint id, Action<ushort, Stream, IPEndPoint> callback)
    {
        if (!OnPacketReceived.ContainsKey(id))
            OnPacketReceived.Add(id, callback);
    }

    public void RemoveListener(uint id)
    {
        if (OnPacketReceived.ContainsKey(id))
            OnPacketReceived.Remove(id);
    }

    public void SendPacket(ISerializable packet, uint objectId)
    {
        byte[] sendBytes = Serialize(packet, objectId);

        if (NetworkManager.instance.IsServer)
        {
            NetworkManager.instance.Broadcast(sendBytes);
        }
        else
            NetworkManager.instance.SendToServer(sendBytes);
    }

    public void SendToClient(ISerializable packet, uint objectId, IPEndPoint ip)
    {
        byte[] sendBytes = Serialize(packet, objectId);

        if (NetworkManager.instance.IsServer)
            NetworkManager.instance.SendToClient(sendBytes, ip);
    }

    private byte[] Serialize(ISerializable packet, uint objectId)
    {
        PacketHeader header = new PacketHeader();
        MemoryStream stream = new MemoryStream();

        header.id = currentPacketId;
        header.objectId = objectId;
        header.senderId = ConnectionManager.instance.ClientId;
        header.packetType = packet.packetType;

        header.Serialize(stream);
        packet.Serialize(stream);

        stream.Close();

        return stream.ToArray();
    }

    public void OnReceiveData(byte[] data, IPEndPoint ip)
    {
        PacketHeader header = new PacketHeader();
        MemoryStream stream = new MemoryStream(data);

        header.Deserialize(stream);

        if (OnPacketReceived.ContainsKey(header.objectId))
            OnPacketReceived[header.objectId].Invoke(header.packetType, stream, ip);

        stream.Close();
    }
}
