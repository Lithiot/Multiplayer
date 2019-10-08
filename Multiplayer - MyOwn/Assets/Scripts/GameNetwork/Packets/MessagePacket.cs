using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MessagePacket : GameNetworkPacket<string>
{
    public MessagePacket() : base(PacketType.Message)
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
        payload = br.ReadString();
    }
}
