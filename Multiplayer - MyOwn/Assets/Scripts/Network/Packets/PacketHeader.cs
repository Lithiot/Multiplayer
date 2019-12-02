using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PacketHeader : ISerializable
{
    public uint id;
    public uint senderId;
    public uint objectId;
    public ushort packetType { get; set; }
    public bool reliable = false;

    public void Serialize(Stream stream)
    {
        BinaryWriter br = new BinaryWriter(stream);

        br.Write(id);
        br.Write(senderId);
        br.Write(objectId);
        br.Write(packetType);
        br.Write(reliable);
    }

    public void Deserialize(Stream stream)
    {
        BinaryReader br = new BinaryReader(stream);

        id = br.ReadUInt32();
        senderId = br.ReadUInt32();
        objectId = br.ReadUInt32();
        packetType = br.ReadUInt16();
        reliable = br.ReadBoolean();
    }
}
