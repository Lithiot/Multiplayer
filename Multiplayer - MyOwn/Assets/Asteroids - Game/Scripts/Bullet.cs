using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    float speed = 10.0f;

    Vector3 direction;
    public void Shoot(Vector3 dir)
    {
        direction = dir;
        this.transform.up = dir;
    }

    void Update()
    {
        Vector3 pos = this.transform.position;

        pos += direction * speed * Time.deltaTime;

        // Check boundaries
        Bounds b = CameraUtils.OrthographicBounds();

        if (Mathf.Abs(pos.x) > b.extents.x)
        {
            Destroy();
        }

        if (Mathf.Abs(pos.y) > b.extents.y)
        {
            Destroy();
        }

        this.transform.position = pos;
    }

    void Destroy()
    {
        Destroy(this.gameObject);
    }
}
