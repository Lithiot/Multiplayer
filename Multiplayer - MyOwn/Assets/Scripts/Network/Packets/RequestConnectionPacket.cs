using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RequestConnectionPacket : BasePacket<ulong>
{

    public RequestConnectionPacket() : base((ushort)NetworkPacketType.ConnectionRequest)
    {
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter bw = new BinaryWriter(stream);
        bw.Write(payload);
    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader br = new BinaryReader(stream);
        payload = br.ReadUInt64();
    }
}
