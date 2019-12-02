using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System;

enum ReliableType
{
    Acknowledge, Count
}

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

    public uint currentPacketId = 0;
    private Dictionary<uint, Action<ushort, Stream, IPEndPoint>> OnPacketReceived = new Dictionary<uint, System.Action<ushort, Stream, IPEndPoint>>();
    private Dictionary<uint, byte[]> packetsAwaitingAcknowledge = new Dictionary<uint, byte[]>();
    private uint constantForBitmasking = 1;

    // Server Properties
    private Dictionary<uint, uint> lastAckPerClient = new Dictionary<uint, uint>();
    private Dictionary<uint, uint> ackBitsPerClient = new Dictionary<uint, uint>();

    // Client Properties
    private uint lastServerAck = 0;
    private uint serverAckBits = 0;

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

    public void SendReliablePacket(ISerializable packet, uint objectId)
    {
        byte[] sendBytes = Serialize(packet, objectId, true, currentPacketId);

        packetsAwaitingAcknowledge.Add(currentPacketId, sendBytes);
        currentPacketId++;

        if (!NetworkManager.instance.IsServer)
            NetworkManager.instance.SendToServer(sendBytes);
        else
            NetworkManager.instance.Broadcast(sendBytes);
    }

    public void SendReliablePacket(ISerializable packet, uint objectId, IPEndPoint ip)
    {
            byte[] sendBytes = Serialize(packet, objectId, true, currentPacketId);

            if (NetworkManager.instance.IsServer)
                NetworkManager.instance.SendToClient(sendBytes, ip);
    }

    private void ResendReliablePacket(uint id)
    {
        NetworkManager.instance.SendToServer(packetsAwaitingAcknowledge[id]);
        Debug.Log("I resended packet with Id: " + id);
    }

    public void SendPacket(ISerializable packet, uint objectId, uint packetId = 0)
    {
        byte[] sendBytes = Serialize(packet, objectId, false, packetId);

        if (NetworkManager.instance.IsServer)
        {
            NetworkManager.instance.Broadcast(sendBytes);
        }
        else
            NetworkManager.instance.SendToServer(sendBytes);
    }

    public void SendToClient(ISerializable packet, uint objectId, IPEndPoint ip, uint packetId = 0)
    {
        byte[] sendBytes = Serialize(packet, objectId, false, packetId);

        if (NetworkManager.instance.IsServer)
            NetworkManager.instance.SendToClient(sendBytes, ip);
    }

    private byte[] Serialize(ISerializable packet, uint objectId, bool reliable, uint packetId)
    {
        PacketHeader header = new PacketHeader();
        MemoryStream stream = new MemoryStream();

        header.id = packetId;
        header.objectId = objectId;
        header.senderId = ConnectionManager.instance.ClientId;
        header.packetType = packet.packetType;
        header.reliable = reliable;

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

        if (header.reliable)
        {
            if (header.packetType == (uint)ReliableType.Acknowledge)
                ProcessAcknowledgePacket(stream);
            else if (NetworkManager.instance.IsServer)
                ProcessReliablePacketAsServer(header);
            else
                    ProcessReliablePacketAsClient(header);
        }

        if (OnPacketReceived.ContainsKey(header.objectId))
            OnPacketReceived[header.objectId].Invoke(header.packetType, stream, ip);

        stream.Close();
    }

    private void ProcessReliablePacketAsServer(PacketHeader header)
    {
        Debug.Log("I received reliable packet with Id: " + header.id);

        if (!lastAckPerClient.ContainsKey(header.senderId))
        {
            ackBitsPerClient.Add(header.senderId, 0);
            lastAckPerClient.Add(header.senderId, 0);
        }

        int difference = (int)(header.id - lastAckPerClient[header.senderId]);

        if (difference > 0)
        {
            ackBitsPerClient[header.senderId] = ackBitsPerClient[header.senderId] << difference;
            ackBitsPerClient[header.senderId] |= constantForBitmasking;
            lastAckPerClient[header.senderId] = header.id;
        }
        else if(difference > -32)
        {
            ackBitsPerClient[header.senderId] |= constantForBitmasking << (-difference);
        }

        SendAcknowledgePacket(header.senderId);
    }

    private void SendAcknowledgePacket(uint senderId)
    {
        ReliableAcknowledgePacket packet = new ReliableAcknowledgePacket();

        if (NetworkManager.instance.IsServer)
        {
            packet.lastAck = lastAckPerClient[senderId];
            packet.ackBits = ackBitsPerClient[senderId];
            SendReliablePacket(packet, 2, NetworkManager.instance.GetClientIpById(senderId));
        }
        else
        {
            packet.lastAck = lastServerAck;
            packet.ackBits = serverAckBits;
            SendReliablePacket(packet, 2);
        }

    }

    private void ProcessReliablePacketAsClient(PacketHeader header)
    {
        int difference = (int)(header.id - lastServerAck);

        if (difference > 0)
        {
            serverAckBits = serverAckBits << difference;
            serverAckBits |= 1u;
            lastServerAck = header.id;
        }
        else if (difference > -32)
        {
            serverAckBits |= 1u << (-difference);
        }

        SendAcknowledgePacket(header.senderId);
    }

    private void ProcessAcknowledgePacket(Stream stream)
    {
        Debug.Log("I received acknowledge packet");

        ReliableAcknowledgePacket packet = new ReliableAcknowledgePacket();
        packet.Deserialize(stream);

        for (int i = 0; i <= packet.lastAck; i++)
        {
            uint aux = packet.ackBits & (1u << i);

            uint id = packet.lastAck - (uint)i;

            if (aux != 0)
            {
                if (packetsAwaitingAcknowledge.ContainsKey(id))
                    packetsAwaitingAcknowledge.Remove(id);
            }
            else
            {
                ResendReliablePacket(id);
            }
        }
    }
}