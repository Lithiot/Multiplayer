using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PacketType
{
    HandShake,
    HandShake_OK,
    ACK,
    Error,
    Ping,
    Message,
}

public abstract class GameNetworkPacket<T> : BasePacket<T>
{
    public GameNetworkPacket(PacketType type) : base((ushort)type)
    {
    }
}
