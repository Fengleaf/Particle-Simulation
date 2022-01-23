using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollider : MonoBehaviour
{
    public bool IsCollision = false;
    public Vector3 RelativeVector;
    public Vector3 HitPoint;
    private LineRenderer lineRenderer;
    private Vector3[] linePnts = new Vector3[2];

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public bool RayCast(Vector3 forward)
    {
        Ray ray = new Ray(transform.position, forward);
        bool collision = Physics.Raycast(ray, out RaycastHit hit, 0.5f);
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
        //linePnts[0] = transform.parent.position;
        //linePnts[1] = (forward - linePnts[0]) * 0.01f;
        //lineRenderer.SetPositions(linePnts);
        return IsCollision;
    }
}
