using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public struct bulletProperties
{
    public Vector3 dir;

    public bulletProperties(Vector3 dir)
    {
        this.dir = dir;
    }
}

public class ShootPacket : GameNetworkPacket<bulletProperties>
{
    public ShootPacket() : base(PacketType.Shoot)
    {
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter bw = new BinaryWriter(stream);
        bw.Write(payload.dir.x);
        bw.Write(payload.dir.y);
        bw.Write(payload.dir.z);
    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader br = new BinaryReader(stream);
        payload.dir.x = br.ReadSingle();
        payload.dir.y = br.ReadSingle();
        payload.dir.z = br.ReadSingle();
    }
}
