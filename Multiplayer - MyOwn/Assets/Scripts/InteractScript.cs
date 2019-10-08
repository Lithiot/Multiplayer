using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using UnityEngine.UI;
using System.Text;
using System.IO;


public class InteractScript : MonoBehaviour
{
    public GameObject initialMenu;
    public GameObject chatMenu;

    public InputField messageField;
    public Text screenText;

    private void Start()
    {
        PacketManager.instance.AddListener(0, OnReceivePacket);
    }

    public void StartServer()
    {
        NetworkManager.instance.StartServer(1234);
        ChangeMenu();
    }

    public void StartClient()
    {
        IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
        NetworkManager.instance.StartClient(ipAdress, 1234);
        ChangeMenu();
    }

    public void ChangeMenu()
    {
        initialMenu.SetActive(false);
        chatMenu.SetActive(true);
    }

    public void SendMessage()
    {
        screenText.text += messageField.text + System.Environment.NewLine;

        MessagePacket message = new MessagePacket();
        message.payload = messageField.text;

        PacketManager.instance.SendPacket(message, 0);
    }

    public void OnReceivePacket(ushort packetType, Stream stream, IPEndPoint ip)
    {
        if (packetType == (ushort)PacketType.Message)
        {
            MessagePacket message = new MessagePacket();

            message.Deserialize(stream);

            screenText.text += message.payload + System.Environment.NewLine;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (messageField && messageField.text != "")
            {
                SendMessage();

                messageField.ActivateInputField();
                messageField.Select();
                messageField.text = "";
            }
        }
    }
}
