using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;

public enum NetworkPacketType
{
    ConnectionRequest,
    ConnectionChallenge,
    ChallengeResponse,
    Acknowledge,
    Ping
}

public struct Client
{
    public uint id;
    public IPEndPoint ipEndPoint;
    public ulong clientSalt;
    public ulong serverSalt;

    public Client(IPEndPoint ipEndPoint, uint id, ulong clientSalt, ulong serverSalt)
    {
        this.id = id;
        this.ipEndPoint = ipEndPoint;
        this.clientSalt = clientSalt;
        this.serverSalt = serverSalt;
    }
}

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager instance;

    private void Awake()
    {
        if (instance && instance != this)
            Destroy(this);
        else
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
    }

    private uint clientId = 0;
    public uint ClientId { get => clientId; }

    private Dictionary<uint, Client> unconfirmedClients = new Dictionary<uint, Client>();
    private readonly Dictionary<IPEndPoint, uint> ipToId = new Dictionary<IPEndPoint, uint>();

    private void Start()
    {
        PacketManager.instance.AddListener(1, OnRecieveData);
    }

    public void SendConnectionRequest()
    {
        RequestConnectionPacket packet = new RequestConnectionPacket();
        packet.payload = 0;

        PacketManager.instance.SendPacket(packet, 1);
    }

    private void SendChallengeRequest(IPEndPoint ip)
    {
        if (!ipToId.ContainsKey(ip))
        {
            uint id = clientId;
            ipToId[ip] = id;

            ulong clientSalt = GenerateRandomLong();
            ulong serverSalt = GenerateRandomLong();

            unconfirmedClients.Add(clientId, new Client(ip, id, clientSalt, serverSalt));
        }

        ChallengePacket packet = new ChallengePacket();
        packet.clientSalt = unconfirmedClients[ipToId[ip]].clientSalt;
        packet.serverSalt = unconfirmedClients[ipToId[ip]].serverSalt;

        PacketManager.instance.SendToClient(packet, 1, ip);
    }

    private ulong GenerateRandomLong()
    {
        System.Random random = new System.Random();
        return (ulong)random.NextLong(long.MinValue, long.MaxValue);
    }

    private void RespondChallenge(Stream stream, IPEndPoint ip)
    {
        ChallengePacket challenge = new ChallengePacket();
        challenge.Deserialize(stream);

        ChallengeResponse packet = new ChallengeResponse();
        packet.payload = GenerateChallengeResult(challenge.clientSalt, challenge.serverSalt);

        PacketManager.instance.SendPacket(packet, 1);
    }

    private ulong GenerateChallengeResult(ulong clientSalt, ulong serverSalt)
    {
        ulong result = clientSalt ^ serverSalt;
        return result;
    }

    private void CheckResults(Stream stream, IPEndPoint ip)
    {
        ChallengeResponse response = new ChallengeResponse();
        response.Deserialize(stream);

        Client client = unconfirmedClients[ipToId[ip]];
        ulong serverResult = GenerateChallengeResult(client.clientSalt, client.serverSalt);

        if (response.payload == serverResult)
        {
            NetworkManager.instance.AddClient(client);
        }
    }

    private void OnRecieveData(ushort type, Stream stream, IPEndPoint ip)
    {
        switch ((NetworkPacketType)type)
        {
            case NetworkPacketType.ConnectionRequest:
                if (NetworkManager.instance.IsServer)
                {
                    SendChallengeRequest(ip);
                }
                break;
            case NetworkPacketType.ConnectionChallenge:
                if (!NetworkManager.instance.IsServer)
                    RespondChallenge(stream, ip);
                break;
            case NetworkPacketType.ChallengeResponse:
                if (NetworkManager.instance.IsServer)
                    CheckResults(stream, ip);
                break;
            case NetworkPacketType.Acknowledge:
                break;
            case NetworkPacketType.Ping:
                break;
            default:
                break;
        }
    }
}
