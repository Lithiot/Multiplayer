using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Text;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

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

    private void Start()
    {
        PacketManager.instance.AddListener(3, OnReceivePacket);
    }

    public GameObject initialMenu;
    public GameObject shipPrefab;
    public GameObject spawnPoint1;
    public GameObject spawnPoint2;

    private GameObject thisShip;

    public void StartServer()
    {
        NetworkManager.instance.StartServer(1234);
        NetworkManager.instance.onClientConnected += OnClientConnected;
        InstantiatePlayer();
        ChangeMenu();
    }

    public void StartClient()
    {
        IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
        NetworkManager.instance.StartClient(ipAdress, 1234);
        InstantiatePlayer();
        ChangeMenu();
    }

    public void ChangeMenu()
    {
        initialMenu.SetActive(false);
    }

    public void OnClientConnected(uint clientId)
    {
        GameObject go = Instantiate(shipPrefab, spawnPoint2.transform.position, Quaternion.identity);
        go.GetComponent<Ship>().SetIsOwner(false);
    }

    public void InstantiatePlayer()
    {
        if (NetworkManager.instance.IsServer)
            thisShip = Instantiate(shipPrefab, spawnPoint1.transform.position, Quaternion.identity);
        else
        {
            thisShip = Instantiate(shipPrefab, spawnPoint2.transform.position, Quaternion.identity);
            GameObject go = Instantiate(shipPrefab, spawnPoint1.transform.position, Quaternion.identity);
            go.GetComponent<Ship>().SetIsOwner(false);
        }

        thisShip.GetComponent<Ship>().SetIsOwner(true);
    }

    public void OnReceivePacket(ushort packetType, Stream stream, IPEndPoint ip)
    {
    }
}
