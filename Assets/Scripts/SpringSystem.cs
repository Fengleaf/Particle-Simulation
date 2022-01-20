using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringSystem : MonoBehaviour
{
    public int ConnectIndexStart;
    public int ConnectIndexEnd;
    public float OriginLength;

    private const float Ks = 500.0f;
    private const float Kd = 1.5f;

    public SpringSystem(int startIndex, int endIndex, float sideLength)
    {
        // 連接資訊
        ConnectIndexStart = startIndex;
        ConnectIndexEnd = endIndex;

        OriginLength = sideLength;
    }

    public Vector3 CountForce(Vector3 startSpeed, Vector3 endSpeed, Vector3 startPos, Vector3 endPos)
    {
        // Damped spring
        float distance = Vector3.Distance(startPos, endPos);
        return -(Ks * (distance - OriginLength) + Kd * Vector3.Dot(startSpeed - endSpeed, startPos - endPos) / distance)
            * (startPos - endPos) / distance;
    }
}
