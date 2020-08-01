using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GameFinishedPacket : GameNetworkPacket<bool>
{
    public GameFinishedPacket() : base(PacketType.GameOver) 
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
        payload = br.ReadBoolean();
    }
}
