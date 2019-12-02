using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public struct Transform
{
    public Vector3 pos;
    public Quaternion rot;

    public Transform(Vector3 pos, Quaternion rot)
    {
        this.pos = pos;
        this.rot = rot;
    }
}

public class TransformPacket : GameNetworkPacket<Transform>
{
    public TransformPacket() : base(PacketType.Transform)
    {
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter bw = new BinaryWriter(stream);
        bw.Write(payload.pos.x);
        bw.Write(payload.pos.y);
        bw.Write(payload.pos.z);
        bw.Write(payload.rot.x);
        bw.Write(payload.rot.y);
        bw.Write(payload.rot.z);
        bw.Write(payload.rot.w);
    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader br = new BinaryReader(stream);
        payload.pos.x = br.ReadSingle();
        payload.pos.y = br.ReadSingle();
        payload.pos.z = br.ReadSingle();
        payload.rot.x = br.ReadSingle();
        payload.rot.y = br.ReadSingle();
        payload.rot.z = br.ReadSingle();
        payload.rot.w = br.ReadSingle();
    }
}
