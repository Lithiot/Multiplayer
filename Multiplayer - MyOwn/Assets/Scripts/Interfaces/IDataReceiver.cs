using System.Net;

public interface IDataReceiver
{
    void OnReceiveData(byte[] data, IPEndPoint ip);
}
