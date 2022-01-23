using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    private Vector3 position;

    private Collider colli;

    private void Start()
    {
        position = transform.position;
        colli = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        Vector3 forward = transform.position - position;
        forward.Normalize();
        Ray ray = new Ray(transform.position, forward);
        Debug.DrawRay(transform.position, ray.direction * 100, Color.red);
        RaycastHit[] hits = new RaycastHit[0];
        if (colli is SphereCollider sc)
        {
            hits = Physics.SphereCastAll(ray, sc.radius, 1.5f);
        }
        else if (colli is BoxCollider bc)
        {
            hits = Physics.BoxCastAll(transform.position, bc.size / 2.0f, ray.direction, Quaternion.identity, bc.size.x + 1.5f);
        }
        if (hits.Length > 1)
        {
            for(int i = 1;i < hits.Length;i++)
            {
                if (hits[i].collider.CompareTag("Particle"))
                {
                    hits[i].collider.transform.parent.GetComponent<ClothSystem>().AppendWallCollisionForce(hits[i].collider.gameObject, forward);
                }
            }
        }
        position = transform.position;
    }
}
