using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReliableAcknowledgePacket : BasePacket<uint>
{
    public uint lastAck;
    public uint ackBits;

    public ReliableAcknowledgePacket() : base((ushort)ReliableType.Acknowledge)
    {
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter bw = new BinaryWriter(stream);
        bw.Write(lastAck);
        bw.Write(ackBits);
    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader br = new BinaryReader(stream);
        lastAck = br.ReadUInt32();
        ackBits = br.ReadUInt32();
    }
}
