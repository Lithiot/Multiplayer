using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConnectedAcknowledgePacket : BasePacket<bool>
{
    public uint clientID;

    public ConnectedAcknowledgePacket() : base((ushort)NetworkPacketType.Acknowledge)
    {
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter bw = new BinaryWriter(stream);
        bw.Write(payload);
        bw.Write(clientID);
    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader br = new BinaryReader(stream);
        payload = br.ReadBoolean();
        clientID = br.ReadUInt32();
    }
}
