using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public abstract class BasePacket<T> : ISerializable
{
    public ushort packetType { get; set; }
    public T payload;

    public BasePacket(ushort type)
    {
        this.packetType = type;
    }

    public void Serialize(Stream stream)
    {
        OnSerialize(stream);
    }

    public void Deserialize(Stream stream)
    {
        OnDeserialize(stream);
    }

    protected abstract void OnSerialize(Stream stream);
    protected abstract void OnDeserialize(Stream stream);
}