using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;

public class Ship : MonoBehaviour
{
    [SerializeField] protected int lives;
    [SerializeField] private float rotSpeed = 120.0f;
    [SerializeField] private float acceleration = 2.5f;
    private Vector3 speed = Vector3.zero;

    [SerializeField] private float fireRate;
    float timer = 0.0f;

    [SerializeField] private GameObject bulletPrefab;

    private float networkTimer = 0.10f;
    private float auxNetworkTimer;

    private bool isOwner;

    public bool GetIsOwner()
    {
        return isOwner;
    }

    public void SetIsOwner(bool isOwner)
    {
        this.isOwner = isOwner;
    }

    private void Start()
    {
        if(!isOwner)
            PacketManager.instance.AddListener(4, OnReceivePacket);
    }

    public void Reset()
    {
        lives = 3;
        this.transform.position = Vector3.zero;
        timer = fireRate;
    }

    private void Update()
    {
        if (isOwner)
        {
            float horizontal = -Input.GetAxis("Horizontal");
            float vertical = Mathf.Clamp01(Input.GetAxis("Vertical"));

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Shoot(-this.transform.up);
            }

            this.transform.rotation *= Quaternion.AngleAxis(horizontal * rotSpeed * Time.deltaTime, Vector3.forward);
            speed += vertical * -this.transform.up * acceleration * Time.deltaTime;

            Vector3 pos = this.transform.position;
            pos += speed * Time.deltaTime;
            this.transform.position = pos;

            CheckBounds(pos);

            if (auxNetworkTimer > networkTimer)
                SendTransform();
            else
                auxNetworkTimer += Time.deltaTime;
        }
    }

    private void CheckBounds(Vector3 pos)
    {
        Bounds b = CameraUtils.OrthographicBounds();

        if (pos.x > b.extents.x)
        {
            pos.x = -b.extents.x;
        }
        else if (pos.x < -b.extents.x)
        {
            pos.x = b.extents.x;
        }

        if (pos.y > b.extents.y)
        {
            pos.y = -b.extents.y;
        }
        else if (pos.y < -b.extents.y)
        {
            pos.y = b.extents.y;
        }

        // Set position
        this.transform.position = pos;
    }

    void Shoot(Vector3 dir)
    {
        GameObject go = Instantiate(bulletPrefab, transform.position, transform.rotation);
        go.GetComponent<Bullet>().Shoot(dir);

        if (isOwner)
        {
            ShootPacket packet = new ShootPacket();
            packet.payload = new bulletProperties(dir);

            PacketManager.instance.SendPacket(packet, 4);
        }
    }

    void Die()
    {
        lives--;
        this.transform.position = Vector3.zero;

        if (isOwner)
        {
            DeathPacket packet = new DeathPacket();
            packet.payload = true;

            PacketManager.instance.SendReliablePacket(packet, 4);
        }

    }

    public void OnReceivePacket(ushort packetType, Stream stream, IPEndPoint ip)
    {
        if (packetType == (uint)PacketType.Transform)
        {
            TransformPacket packet = new TransformPacket();

            packet.Deserialize(stream);

            transform.position = packet.payload.pos;
            transform.rotation = packet.payload.rot;
        }

        if (packetType == (uint)PacketType.Died)
        {
            Die();
        }

        if (packetType == (uint)PacketType.Shoot)
        {
            ShootPacket packet = new ShootPacket();
            packet.Deserialize(stream);

            Shoot(packet.payload.dir);
        }
    }

    private void SendTransform()
    {
        TransformPacket packet = new TransformPacket();

        packet.payload = new Transform(transform.position, transform.rotation);

        PacketManager.instance.SendPacket(packet, 4);
    }
}
