using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PacketType
{
    Message,
    Transform,
    Died,
    Shoot,
    GameOver
}

public abstract class GameNetworkPacket<T> : BasePacket<T>
{
    public GameNetworkPacket(PacketType type) : base((ushort)type)
    {
    }
}
