using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ForceStatus
{
    Euler,
    RungeKutta2,
    RungeKutta4
}

public class ClothSystem : MonoBehaviour
{
    public GameObject ParticlePrefab;

    // 邊有幾顆粒子
    public int SideCount = 10;
    // 布料初始高度
    public float InitialHeight = 5;
    // 每顆粒子距離
    public float UnitDistance = 1;

    public List<GameObject> Particles = new List<GameObject>();
    public List<Vector3> Vertexes = new List<Vector3>();
    public List<Vector2> UVs = new List<Vector2>();
    public List<int> TrianglesIndexes = new List<int>();

    public ForceStatus ForceStatus = ForceStatus.RungeKutta2;

    public float Mass = 1;
    public float Gravity = -9.81f;

    private List<SpringSystem> springArray = new List<SpringSystem>();
    private List<Vector3> speedArray = new List<Vector3>();

    public Texture Texture;
    public Material TwoSideMat;
    private MeshFilter meshFilter;
    private MeshRenderer meshRender;

    private void Start()
    {
        // 加上 Component
        meshFilter = this.gameObject.AddComponent<MeshFilter>();
        meshRender = this.gameObject.AddComponent<MeshRenderer>();

        // 創建 Mesh
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;
        meshRender.material = TwoSideMat;
        meshRender.material.mainTexture = Texture;

        for (int i = 0; i < SideCount; i++)
        {
            for (int j = 0; j < SideCount; j++)
            {
                #region 產生粒子
                Vector3 position = new Vector3(i * UnitDistance, InitialHeight, j * UnitDistance);
                Vertexes.Add(position);
                //GameObject particle = Instantiate(ParticlePrefab, position, Quaternion.identity, transform);
                GameObject particle = new GameObject();
                particle.name = $"Particle {i} * {j}";
                particle.transform.position = position;
                particle.transform.parent = transform;
                particle.AddComponent<Collider>();
                Particles.Add(particle);
                #endregion
                #region UV
                float u = (float)i / (SideCount - 1);
                float v = (float)j / (SideCount - 1);
                UVs.Add(new Vector2(u, v));
                #endregion
                #region 速度，初始化為0
                speedArray.Add(Vector3.zero);
                #endregion
                #region 每個點連接彈簧
                AddSpringWithIndex(i * SideCount + j);
                #endregion
            }
        }

        #region 三角形
        // 相鄰的三個點形成三角形
        // 。
        // 。。
        for (int i = 0; i < SideCount - 1; i++)
        {
            for (int j = 0; j < SideCount - 1; j++)
            {
                // Index 資訊
                int index = i * SideCount + j;
                TrianglesIndexes.Add(index);
                TrianglesIndexes.Add(index + 1);
                TrianglesIndexes.Add(index + SideCount);

                TrianglesIndexes.Add(index + 1);
                TrianglesIndexes.Add(index + 1 + SideCount);
                TrianglesIndexes.Add(index + SideCount);
            }
        }
        #endregion

        mesh.vertices = Vertexes.ToArray();
        mesh.uv = UVs.ToArray();
        mesh.triangles = TrianglesIndexes.ToArray();
    }

    private void FixedUpdate()
    {
        // 彈簧
        Vector3[] tempspeedArray = new Vector3[speedArray.Count];
        for (int i = 0; i < springArray.Count; i++)
        {
            // 拿彈簧的起始粒子與結束粒子
            int startIndex = springArray[i].ConnectIndexStart;
            int endIndex = springArray[i].ConnectIndexEnd;

            // 拿現在速度
            Vector3 startSpeed = speedArray[startIndex];
            Vector3 endSpeed = speedArray[endIndex];
            // 拿現在位置
            Vector3 startPos = Vertexes[startIndex];
            Vector3 endPos = Vertexes[endIndex];

            Vector3 tempForce = springArray[i].CountForce(startSpeed, endSpeed, startPos, endPos);
            // 彈簧拉扯，對起始粒子來說是正向，對終點粒子來說是負向
            tempspeedArray[startIndex] += tempForce / Mass * Time.fixedDeltaTime;
            tempspeedArray[endIndex] -= tempForce / Mass * Time.fixedDeltaTime;
        }
        // 存入 speedArray
        for (int i = 0; i < speedArray.Count; i++)
        {
            speedArray[i] += tempspeedArray[i];
            // 重力
            speedArray[i] += Vector3.up * Gravity * Time.fixedDeltaTime;
            // 碰撞檢測
            if (Particles[i].GetComponent<Collider>().RayCast(speedArray[i]))
                speedArray[i] = Vector3.zero;
        }
        // 衣服上兩個點固定住
        speedArray[SideCount * SideCount - 1] = Vector3.zero;
        speedArray[SideCount - 1] = Vector3.zero;
        // 更新粒子資訊
        for (int i = 0; i < SideCount; i++)
        {
            for (int j = 0; j < SideCount; j++)
            {
                int index = i * SideCount + j;
                Vector3 result = Vector3.zero;
                switch (ForceStatus)
                {
                    case ForceStatus.Euler:
                        result = EulerMethod(index, Time.fixedDeltaTime);
                        break;
                    case ForceStatus.RungeKutta2:
                        result = RungeKutta2(index, Time.fixedDeltaTime);
                        break;
                    case ForceStatus.RungeKutta4:
                        result = RunguKutta4(index, Time.fixedDeltaTime);
                        break;
                    default:
                        break;
                }
                // 碰撞
                if (!Particles[index].GetComponent<Collider>().IsCollision)
                    Vertexes[index] += result;
                Particles[index].transform.position = Vertexes[index];
            }
        }
        meshFilter.mesh.vertices = Vertexes.ToArray();
    }

    private void AddSpringWithIndex(int index)
    {
        int NextIndex;
        // Structural Springs
        // 向上
        NextIndex = index + 1;
        // 確保在同一行
        if (NextIndex / SideCount == index / SideCount)
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance));
        // 向右
        NextIndex = index + SideCount;
        if (NextIndex / SideCount < SideCount)
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance));
        // Shear Springs
        // 右上
        NextIndex = index + SideCount + 1;
        // 避免超出邊界且要在隔壁
        if (NextIndex / SideCount < SideCount && NextIndex / SideCount == index / SideCount + 1)
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance * Mathf.Sqrt(2)));
        // 左上
        NextIndex = index - SideCount + 1;
        if (NextIndex > 0 && NextIndex / SideCount < SideCount && NextIndex / SideCount == index / SideCount - 1)
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance * Mathf.Sqrt(2)));
        // Bending Springs
        // 向上
        NextIndex = index + 2;
        if (NextIndex < SideCount)
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance * 2));
        // 向右
        NextIndex = index + SideCount * 2;
        if (NextIndex / SideCount < SideCount)
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance * 2));
    }

    // 一般的 Euler 方法
    private Vector3 EulerMethod(int index, float time)
    {
        // x = vt
        return speedArray[index] * time;
    }

    private Vector3 RungeKutta2(int index, float time)
    {
        // 固定的點不計算
        if (index == SideCount - 1 || index == SideCount * SideCount - 1)
            return Vector3.zero;
        // K1
        Vector3 k1 = EulerMethod(index, time);
        // K2
        // V = a t
        Vector3 appendSpeedK2 = Vector3.up * Gravity * Time.deltaTime / 2;
        for (int i = 0; i < springArray.Count; i++)
        {
            if (springArray[i].ConnectIndexStart == index || springArray[i].ConnectIndexEnd == index)
            {
                // 拿 Index
                int StartIndex = springArray[i].ConnectIndexStart;
                int EndIndex = springArray[i].ConnectIndexEnd;

                // 拿資料
                Vector3 StartSpeed = speedArray[StartIndex];
                Vector3 EndSpeed = speedArray[EndIndex];
                Vector3 StartPos = Vertexes[StartIndex] + EulerMethod(StartIndex, time / 2);
                Vector3 EndPos = Vertexes[EndIndex] + EulerMethod(EndIndex, time / 2);

                Vector3 tempForce = springArray[i].CountForce(StartSpeed, EndSpeed, StartPos, EndPos);

                if (index == springArray[i].ConnectIndexStart)
                    appendSpeedK2 += tempForce / Mass * Time.fixedDeltaTime;
                else
                    appendSpeedK2 -= tempForce / Mass * Time.fixedDeltaTime;
            }
        }
        Vector3 k2 = EulerMethodWithAppendForce(index, time / 2, appendSpeedK2);
        return k2;
    }

    // 算下一段時間，會加上下一秒的力
    private Vector3 EulerMethodWithAppendForce(int index, float time, Vector3 appendForce)
    {
        return (speedArray[index] + appendForce) * time;
    }

    private Vector3 RunguKutta4(int index, float time)
    {
        // 固定的點不計算
        if (index == SideCount - 1 || index == SideCount * SideCount - 1)
            return Vector3.zero;
        // K1
        Vector3 k1 = EulerMethod(index, time);
        // K2
        Vector3 appendSpeedK2 = Vector3.up * Gravity * Time.deltaTime / 2;
        for (int i = 0; i < springArray.Count; i++)
        {
            if (springArray[i].ConnectIndexStart == index || springArray[i].ConnectIndexEnd == index)
            {
                // 拿 Index
                int StartIndex = springArray[i].ConnectIndexStart;
                int EndIndex = springArray[i].ConnectIndexEnd;

                // 拿資料
                Vector3 StartSpeed = speedArray[StartIndex];
                Vector3 EndSpeed = speedArray[EndIndex];
                Vector3 StartPos = Vertexes[StartIndex] + EulerMethod(StartIndex, time / 2);
                Vector3 EndPos = Vertexes[EndIndex] + EulerMethod(EndIndex, time / 2);

                Vector3 tempForce = springArray[i].CountForce(StartSpeed, EndSpeed, StartPos, EndPos);

                if (index == springArray[i].ConnectIndexStart)
                    appendSpeedK2 += tempForce / Mass * Time.fixedDeltaTime;
                else
                    appendSpeedK2 -= tempForce / Mass * Time.fixedDeltaTime;
            }
        }
        Vector3 k2 = EulerMethodWithAppendForce(index, time / 2, appendSpeedK2);
        // K3
        Vector3 appendSpeedK3 = Vector3.up * Gravity * Time.deltaTime / 2;
        for (int i = 0; i < springArray.Count; i++)
        {
            if (springArray[i].ConnectIndexStart == index || springArray[i].ConnectIndexEnd == index)
            {
                // 拿 Index
                int StartIndex = springArray[i].ConnectIndexStart;
                int EndIndex = springArray[i].ConnectIndexEnd;

                // 拿資料
                Vector3 StartSpeed = speedArray[StartIndex];
                Vector3 EndSpeed = speedArray[EndIndex];
                Vector3 StartPos = Vertexes[StartIndex] + EulerMethodWithAppendForce(index, time / 2, appendSpeedK2);
                Vector3 EndPos = Vertexes[EndIndex] + EulerMethodWithAppendForce(index, time / 2, appendSpeedK2);

                Vector3 tempForce = springArray[i].CountForce(StartSpeed, EndSpeed, StartPos, EndPos);

                if (index == springArray[i].ConnectIndexStart)
                    appendSpeedK3 += tempForce / Mass * Time.fixedDeltaTime;
                else
                    appendSpeedK3 -= tempForce / Mass * Time.fixedDeltaTime;
            }
        }
        Vector3 k3 = EulerMethodWithAppendForce(index, time / 2, appendSpeedK3);
        // K4
        Vector3 appendSpeedK4 = Vector3.up * Gravity * Time.deltaTime / 2;                                  // V = a t
        for (int i = 0; i < springArray.Count; i++)
            if (springArray[i].ConnectIndexStart == index || springArray[i].ConnectIndexEnd == index)
            {
                // 拿 Index
                int StartIndex = springArray[i].ConnectIndexStart;
                int EndIndex = springArray[i].ConnectIndexEnd;

                // 拿資料
                Vector3 StartSpeed = speedArray[StartIndex];
                Vector3 EndSpeed = speedArray[EndIndex];
                Vector3 StartPos = Vertexes[StartIndex] + EulerMethodWithAppendForce(index, time, appendSpeedK3);
                Vector3 EndPos = Vertexes[EndIndex] + EulerMethodWithAppendForce(index, time, appendSpeedK3);

                Vector3 tempForce = springArray[i].CountForce(StartSpeed, EndSpeed, StartPos, EndPos);

                if (index == springArray[i].ConnectIndexStart)
                    appendSpeedK4 += tempForce / Mass * Time.fixedDeltaTime;
                else
                    appendSpeedK4 -= tempForce / Mass * Time.fixedDeltaTime;
            }

        Vector3 k4 = EulerMethodWithAppendForce(index, time, appendSpeedK4);
        return k1 / 6.0f + k2 / 3.0f + k3 / 3.0f + k4 / 6.0f;
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < springArray.Count; i++)
        {
            int startIndex = springArray[i].ConnectIndexStart;
            int endIndex = springArray[i].ConnectIndexEnd;
            Gizmos.DrawLine(Particles[startIndex].transform.position, Particles[endIndex].transform.position);
        }
    }
}
