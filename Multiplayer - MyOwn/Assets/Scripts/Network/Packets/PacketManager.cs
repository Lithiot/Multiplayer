using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System;
using System.Linq;
using PacketDotNet.Utils;

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

    private Dictionary<uint, Dictionary<uint, byte[]>> reliablePacketsInWait = new Dictionary<uint, Dictionary<uint, byte[]>>();
    private Dictionary<uint, uint> expectedID = new Dictionary<uint, uint>();

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


        PacketSecurity security = new PacketSecurity();
        Crc32 crc = new Crc32();
        MemoryStream packetStream = new MemoryStream();

        byte[] hash = crc.ComputeHash(stream.ToArray());

        security.data = stream.ToArray();
        security.hash = hash;

        security.Serialize(packetStream);
        
        packetStream.Close();


        return packetStream.ToArray();
    }

    public void OnReceiveData(byte[] data, IPEndPoint ip)
    {
        PacketSecurity security = new PacketSecurity();
        Crc32 crc = new Crc32();
        MemoryStream packetStream = new MemoryStream(data);

        security.Deserialize(packetStream);

        byte[] myHash = crc.ComputeHash(security.data);

        if (!HashCheck(security.hash, myHash)) return;

        PacketHeader header = new PacketHeader();
        MemoryStream stream = new MemoryStream(security.data);

        header.Deserialize(stream);

        if (header.reliable)
            OnRecieveData_Reliable(security.data, header, stream, ip);
        else
            OnRecieveData_Unrealiable(header, stream, ip);
    }

    private bool HashCheck(byte[] packetHash, byte[] myHash) 
    {
        if (packetHash.Length != myHash.Length) return false;

        for (int i = 0; i < packetHash.Length; i++) 
        {
            if (packetHash[i] != myHash[i]) return false; 
        }

        return true;
    }

    private void OnRecieveData_Reliable(byte[] data, PacketHeader header, MemoryStream stream, IPEndPoint ip) 
    {
        if (header.packetType == (uint)ReliableType.Acknowledge)
            ProcessAcknowledgePacket(stream);
        else if (NetworkManager.instance.IsServer)
            ProcessReliablePacketAsServer(header);
        else
            ProcessReliablePacketAsClient(header);

        uint senderID = header.senderId;

        // if I didn't know about this client, register it and pass on the packet
        if (!expectedID.ContainsKey(senderID)) 
        {
            expectedID[senderID] = header.id + 1;
            reliablePacketsInWait[senderID] = new Dictionary<uint , byte[]>();

            if (OnPacketReceived.ContainsKey(header.objectId))
                OnPacketReceived[header.objectId].Invoke(header.packetType , stream , ip);

            return;
        }

        // Check if the new packet is the next packet that I was expecting
        if (expectedID[senderID] == header.id) 
        {
            // if it is, pass on the packet and setup the new variables
            if (OnPacketReceived.ContainsKey(header.objectId))
                OnPacketReceived[header.objectId].Invoke(header.packetType , stream , ip);

            expectedID[senderID]++;

            CheckPacketsInWait(senderID, ip);
        }
        else 
        {
            // if it's not, enqueue it and wait for next packet
            reliablePacketsInWait[senderID][header.id] = data;
        }
    }

    private void CheckPacketsInWait(uint senderID, IPEndPoint ip)
    {
        // Check if we have the next packet in queue
        if (reliablePacketsInWait[senderID].Count <= 0)
            return;

        Dictionary<uint, byte[]> packetsInWait = reliablePacketsInWait[senderID];

        while (packetsInWait.ContainsKey(expectedID[senderID])) 
        {
            PacketHeader header = new PacketHeader();
            MemoryStream stream = new MemoryStream(packetsInWait[expectedID[senderID]]);

            header.Deserialize(stream);

            if (OnPacketReceived.ContainsKey(header.objectId))
                OnPacketReceived[header.objectId].Invoke(header.packetType , stream , ip);

            packetsInWait.Remove(expectedID[senderID]);
            expectedID[senderID]++;
        }
    }

    private void OnRecieveData_Unrealiable(PacketHeader header, MemoryStream stream, IPEndPoint ip) 
    {
        if (OnPacketReceived.ContainsKey(header.objectId))
            OnPacketReceived[header.objectId].Invoke(header.packetType, stream, ip);
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