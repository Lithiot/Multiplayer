using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PacketSecurity : ISerializable
{
    public ushort packetType { get; set; }

    public int dataLength;
    public int hashLenght;
    public byte[] data;
    public byte[] hash;
 
    public void Serialize(Stream stream)
    {
        BinaryWriter bw = new BinaryWriter(stream);

        bw.Write(data.Length);
        bw.Write(hash.Length);
        bw.Write(data);
        bw.Write(hash);
    }
  
    public void Deserialize(Stream stream)
    {
        BinaryReader br = new BinaryReader(stream);

        dataLength = br.ReadInt32();
        hashLenght = br.ReadInt32();
        data = br.ReadBytes(dataLength);
        hash = br.ReadBytes(hashLenght);
    }
}
