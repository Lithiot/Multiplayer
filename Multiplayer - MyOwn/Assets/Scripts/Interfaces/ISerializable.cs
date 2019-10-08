using System.IO;
public interface ISerializable
{
    ushort packetType { get; set; }

    void Serialize(Stream stream);
    void Deserialize(Stream stream);
}
