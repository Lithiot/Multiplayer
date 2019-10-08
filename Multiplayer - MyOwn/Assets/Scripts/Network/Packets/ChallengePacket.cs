using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ChallengePacket : BasePacket<ulong[]>
{
    public ulong clientSalt;
    public ulong serverSalt;

    public ChallengePacket() : base((ushort)NetworkPacketType.ConnectionChallenge)
    {
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter bw = new BinaryWriter(stream);
        bw.Write(clientSalt);
        bw.Write(serverSalt);
    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader br = new BinaryReader(stream);
        clientSalt = br.ReadUInt64();
        serverSalt = br.ReadUInt64();
    }
}
