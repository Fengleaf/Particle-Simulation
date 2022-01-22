using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringSystem
{
    public int ConnectIndexStart;
    public int ConnectIndexEnd;
    public float OriginLength;

    public float RestLength;

    public Matrix4x4 Jx = new Matrix4x4();
    public Matrix4x4 Jv = new Matrix4x4();
        
    public const float Ks = 500.0f;
    public const float Kd = 1.5f;

    public SpringSystem(int startIndex, int endIndex, float sideLength)
    {
        // 連接資訊
        ConnectIndexStart = startIndex;
        ConnectIndexEnd = endIndex;

        OriginLength = sideLength;
    }

    public void UpdateLength(List<GameObject> particles)
    {
        RestLength = Vector3.Distance(particles[ConnectIndexStart].transform.position, particles[ConnectIndexEnd].transform.position);
    }

    public Vector3 CountForce(Vector3 startSpeed, Vector3 endSpeed, Vector3 startPos, Vector3 endPos)
    {
        // Damped spring
        float distance = Vector3.Distance(startPos, endPos);
        return -(Ks * (distance - OriginLength) + Kd * Vector3.Dot(startSpeed - endSpeed, startPos - endPos) / distance)
            * (startPos - endPos) / distance;
    }
}
