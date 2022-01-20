using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collider : MonoBehaviour
{
    public bool IsCollision = false;
    public bool RayCast(Vector3 forward)
    {
        Ray ray = new Ray(transform.position, forward);
        bool collision = Physics.Raycast(ray, out RaycastHit hit, 0.3f);
        if (collision)
        {
            if (hit.collider.CompareTag("Particle") || hit.collider.CompareTag("Obstacle"))
            {
                IsCollision = true;
            }
            else
            {
                IsCollision = false;
            }
        }
        else
            IsCollision = false;
        return IsCollision;
    }
}
