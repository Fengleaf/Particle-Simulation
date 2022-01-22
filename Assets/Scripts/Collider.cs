using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collider : MonoBehaviour
{
    public bool IsCollision = false;
    public Vector3 RelativeVector;
    public Vector3 HitPoint;
    public bool RayCast(Vector3 forward)
    {
        Ray ray = new Ray(transform.position, forward);
        bool collision = Physics.Raycast(ray, out RaycastHit hit, 0.3f);
        if (collision)
        {
            if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Particle"))
            {
                RelativeVector = transform.position - hit.collider.transform.position;
                HitPoint = hit.collider.ClosestPoint(transform.position);
                IsCollision = true;
            }
            else
                IsCollision = false;
        }
        else
        {
            IsCollision = false;
        }
        return IsCollision;
    }
}
