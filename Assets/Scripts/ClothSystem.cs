using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ForceStatus
{
    Euler = 0,
    RungeKutta2,
    RungeKutta4,
}

public class ClothSystem : MonoBehaviour
{
    public GameObject ParticlePrefab;

    // 邊有幾顆粒子
    public int SideCount = 10;
    // 布料初始位置
    public Vector3 InitialPosition = Vector3.zero;
    // 每顆粒子距離
    public float UnitDistance = 1;

    public List<GameObject> Particles = new List<GameObject>();
    public List<ParticleCollider> Colliders = new List<ParticleCollider>();
    public List<Vector3> Vertexes = new List<Vector3>();
    public List<Vector2> UVs = new List<Vector2>();
    public List<int> TrianglesIndexes = new List<int>();

    public ForceStatus ForceStatus = ForceStatus.RungeKutta2;

    public float Mass = 1;
    public float Gravity = -9.81f;
    public float TimeStep = 0.02f;

    public GameObject lineRendererPrefab;
    public Material shearSpringMat;
    public Material structSpringMat;
    public Material bendSpringMat;

    public bool IsPlaying;

    private List<SpringSystem> springArray = new List<SpringSystem>();
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private List<LineRenderer> forceLineRenderers = new List<LineRenderer>();
    private List<Vector3> speedArray = new List<Vector3>();
    private List<Vector3> userAppendForce = new List<Vector3>();
    private List<Vector3> wallAppendForce = new List<Vector3>();

    public Texture Texture;
    public Material TwoSideMat;
    private MeshFilter meshFilter;
    private MeshRenderer meshRender;

    private List<int> lockIndexes = new List<int>();

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
                Vector3 position = new Vector3(i * UnitDistance + InitialPosition.x, InitialPosition.y, j * UnitDistance + InitialPosition.z);
                Vertexes.Add(position);
                GameObject particle = Instantiate(ParticlePrefab, position, Quaternion.identity, transform);
                forceLineRenderers.Add(particle.transform.GetComponent<LineRenderer>());
                particle.name = $"Particle {i} * {j}";
                Particles.Add(particle);
                Colliders.Add(particle.GetComponent<ParticleCollider>());
                userAppendForce.Add(Vector3.zero);
                wallAppendForce.Add(Vector3.zero);
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
                AddSpringWithIndex(i * SideCount + j, particle);
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
        if (!IsPlaying)
            return;

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
            tempspeedArray[startIndex] += tempForce / Mass * TimeStep;
            tempspeedArray[endIndex] -= tempForce / Mass * TimeStep;
        }

        // 存入 speedArray
        for (int i = 0; i < speedArray.Count; i++)
        {
            speedArray[i] += tempspeedArray[i];
            CheckUserAppendForce(i);
            // 重力
            speedArray[i] += Vector3.up * Gravity * TimeStep;
            // 碰撞檢測
            if (Colliders[i].RayCast(speedArray[i]))
            {
                speedArray[i] = Vector3.zero;
            }
        }
        // 固定點不動
        for (int i = 0; i < lockIndexes.Count; i++)
            speedArray[lockIndexes[i]] = Vector3.zero;

        //if (ForceStatus == ForceStatus.Implicit)
        //{
        //    ComputeJacobians();
        //    List<Vector3> results = new List<Vector3>();
        //    MultiplyDfDx(Vertexes, ref results);
        //    for (int i = 0; i < results.Count; i++)
        //    {
        //        Vertexes[i] = results[i];
        //        Particles[i].transform.position = Vertexes[i];
        //        springArray[i].UpdateLength(Particles);
        //    }
        //}
        //else
        //{
            // 更新粒子資訊
            for (int i = 0; i < SideCount; i++)
            {
                for (int j = 0; j < SideCount; j++)
                {
                    int index = i * SideCount + j;
                    Vector3 result = Vector3.zero;
                    List<Vector3> results = new List<Vector3>();
                    switch (ForceStatus)
                    {
                        case ForceStatus.Euler:
                            result = EulerMethod(index, TimeStep);
                            break;
                        case ForceStatus.RungeKutta2:
                            result = RungeKutta2(index, TimeStep);
                            break;
                        case ForceStatus.RungeKutta4:
                            result = RunguKutta4(index, TimeStep);
                            break;
                        default:
                            break;
                    }
                    // 碰撞
                    if (!Colliders[index].IsCollision)
                        Vertexes[index] += result + userAppendForce[index] + wallAppendForce[index];
                    Vector3[] linePnts = new Vector3[2];
                    linePnts[0] = Particles[index].transform.position;
                    linePnts[1] = (result + userAppendForce[index] + wallAppendForce[index]) * 50f + linePnts[0];
                    forceLineRenderers[index].SetPositions(linePnts);

                    // 給予位置
                    Particles[index].transform.position = Vertexes[index];
                    springArray[index].UpdateLength(Particles);
                }
            }
        //}
        //for (int w = 0; w < wallAppendForce.Count; w++)
        //    wallAppendForce[w] = Vector3.zero;
        meshFilter.mesh.vertices = Vertexes.ToArray();
        for (int i = 0; i < springArray.Count; i++)
        {
            int startIndex = springArray[i].ConnectIndexStart;
            int endIndex = springArray[i].ConnectIndexEnd;
            Vector3[] linePnts = new Vector3[2];
            linePnts[0] = Particles[startIndex].transform.position;
            linePnts[1] = Particles[endIndex].transform.position;
            lineRenderers[i].SetPositions(linePnts);
        }
    }

    private void NewLineRenderer(GameObject parent, Material mat)
    {
        GameObject lineRendererOb = Instantiate(lineRendererPrefab, parent.transform);
        LineRenderer aLineRenderer = lineRendererOb.GetComponent<LineRenderer>();
        aLineRenderer.material = mat;
        lineRenderers.Add(aLineRenderer);
    }

    private void CheckUserAppendForce(int index)
    {
        userAppendForce[index] = Particles[index].transform.position - Vertexes[index];
    }

    public void AppendWallCollisionForce(GameObject particle, Vector3 forward)
    {
        int index = Particles.IndexOf(particle);
        wallAppendForce[index] = forward;
    }

    private void AddSpringWithIndex(int index, GameObject parent)
    {
        int NextIndex;
        // Structural Springs
        // 向上
        NextIndex = index + 1;
        // 確保在同一行
        if (NextIndex / SideCount == index / SideCount)
        {
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance));
            NewLineRenderer(parent, structSpringMat);
        }
        // 向右
        NextIndex = index + SideCount;
        if (NextIndex / SideCount < SideCount)
        {
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance));
            NewLineRenderer(parent, structSpringMat);
        }
        // Shear Springs
        // 右上
        NextIndex = index + SideCount + 1;
        // 避免超出邊界且要在隔壁
        if (NextIndex / SideCount < SideCount && NextIndex / SideCount == index / SideCount + 1)
        {
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance * Mathf.Sqrt(2)));
            NewLineRenderer(parent, shearSpringMat);
        }
        // 左上
        NextIndex = index - SideCount + 1;
        if (NextIndex > 0 && NextIndex / SideCount < SideCount && NextIndex / SideCount == index / SideCount - 1)
        {
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance * Mathf.Sqrt(2)));
            NewLineRenderer(parent, shearSpringMat);
        }
        // Bending Springs
        // 向上
        NextIndex = index + 2;
        if (NextIndex < SideCount)
        {
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance * 2));
            NewLineRenderer(parent, bendSpringMat);
        }
        // 向右
        NextIndex = index + SideCount * 2;
        if (NextIndex / SideCount < SideCount)
        {
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance * 2));
            NewLineRenderer(parent, bendSpringMat);
        }
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
        if (lockIndexes.Contains(index))
            return Vector3.zero;
        // K1
        Vector3 k1 = EulerMethod(index, time);
        // K2
        // V = a t
        Vector3 appendSpeedK2 = Vector3.up * Gravity * TimeStep / 2;
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
                Vector3 StartPos = Vertexes[StartIndex] + EulerMethod(StartIndex, time / 2) / 2.0f;
                Vector3 EndPos = Vertexes[EndIndex] + EulerMethod(EndIndex, time / 2) / 2.0f;

                Vector3 tempForce = springArray[i].CountForce(StartSpeed, EndSpeed, StartPos, EndPos);

                if (index == springArray[i].ConnectIndexStart)
                    appendSpeedK2 += tempForce / Mass * TimeStep / 2.0f;
                else
                    appendSpeedK2 -= tempForce / Mass * TimeStep / 2.0f;
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
        if (lockIndexes.Contains(index))
            return Vector3.zero;
        // K1
        Vector3 k1 = EulerMethod(index, time);
        // K2
        Vector3 appendSpeedK2 = Vector3.up * Gravity * TimeStep / 2;
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
                Vector3 StartPos = Vertexes[StartIndex] + EulerMethod(StartIndex, time / 2) / 2.0f;
                Vector3 EndPos = Vertexes[EndIndex] + EulerMethod(EndIndex, time / 2) / 2.0f;

                Vector3 tempForce = springArray[i].CountForce(StartSpeed, EndSpeed, StartPos, EndPos);

                if (index == springArray[i].ConnectIndexStart)
                    appendSpeedK2 += tempForce / Mass * TimeStep / 2.0f;
                else
                    appendSpeedK2 -= tempForce / Mass * TimeStep / 2.0f ;
            }
        }
        Vector3 k2 = EulerMethodWithAppendForce(index, time / 2, appendSpeedK2);
        // K3
        Vector3 appendSpeedK3 = Vector3.up * Gravity * TimeStep / 2;
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
                Vector3 StartPos = Vertexes[StartIndex] + EulerMethodWithAppendForce(index, time / 2, appendSpeedK2) / 2.0f;
                Vector3 EndPos = Vertexes[EndIndex] + EulerMethodWithAppendForce(index, time / 2, appendSpeedK2) / 2.0f;

                Vector3 tempForce = springArray[i].CountForce(StartSpeed, EndSpeed, StartPos, EndPos);

                if (index == springArray[i].ConnectIndexStart)
                    appendSpeedK3 += tempForce / Mass * TimeStep / 2.0f;
                else
                    appendSpeedK3 -= tempForce / Mass * TimeStep / 2.0f;
            }
        }
        Vector3 k3 = EulerMethodWithAppendForce(index, time / 2, appendSpeedK3);
        // K4
        Vector3 appendSpeedK4 = Vector3.up * Gravity * TimeStep;                                  // V = a t
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
                    appendSpeedK4 += tempForce / Mass * TimeStep;
                else
                    appendSpeedK4 -= tempForce / Mass * TimeStep;
            }

        Vector3 k4 = EulerMethodWithAppendForce(index, time, appendSpeedK4);
        return k1 / 6.0f + k2 / 3.0f + k3 / 3.0f + k4 / 6.0f;
    }

    public void SetParticleVisibility(bool visible)
    {
        foreach (GameObject particle in Particles)
        {
            particle.transform.GetChild(0).gameObject.SetActive(visible);
        }
    }

    public void SetSpringVisibility(bool visible)
    {
        for (int i = 0; i < lineRenderers.Count; i++)
        {
            lineRenderers[i].enabled = visible;
        }
        for (int i = 0; i < forceLineRenderers.Count; i++)
        {
            forceLineRenderers[i].enabled = visible;
        }
    }

    public void SetTextureVisibility(bool visible)
    {
        meshRender.enabled = visible;
    }

    public void SetLockParticle(GameObject particle)
    {
        int index = Particles.IndexOf(particle);
        if (index == -1)
            return;
        if (!lockIndexes.Contains(index))
            lockIndexes.Add(index);
        else
            lockIndexes.Remove(index);
    }

    private void ComputeJacobians()
    {
        for (int i = 0; i < springArray.Count; i++)
        {
            Vector3 dx = Particles[springArray[i].ConnectIndexStart].transform.position - Particles[springArray[i].ConnectIndexEnd].transform.position;
            Matrix4x4 dxtdx = new Matrix4x4();
            Matrix4x4 i3x3 = Matrix4x4.identity;
            dxtdx = OuterProduct(dx, dx);
            // TODO: 檢查是不是dot
            float l = Mathf.Sqrt(Vector3.Dot(dx, dx));
            if (l != 0)
                l = 1.0f / l;
            dxtdx.SetRow(0, dxtdx.GetRow(0) * l * l);
            dxtdx.SetRow(1, dxtdx.GetRow(1) * l * l);
            dxtdx.SetRow(2, dxtdx.GetRow(2) * l * l);
            dxtdx.SetRow(3, dxtdx.GetRow(3) * l * l);

            springArray[i].Jx = AddMatrix(dxtdx, MultipltyScalerMaxtrix(AddMatrix(i3x3, MultipltyScalerMaxtrix(dxtdx, -1)), 1 - springArray[i].RestLength * l));
            springArray[i].Jx = MultipltyScalerMaxtrix(springArray[i].Jx, SpringSystem.Ks);
            springArray[i].Jv = MultipltyScalerMaxtrix(Matrix4x4.identity, SpringSystem.Kd);
        }
    }

    private void MultiplyDfDx(List<Vector3> v1, ref List<Vector3> v2)
    {
        v2.Clear();
        for (int i = 0; i < Particles.Count; i++)
            v2.Add(Vector3.zero);
        for (int i = 0; i < springArray.Count; i++)
        {
            Vector3 temp = springArray[i].Jx.MultiplyVector(v1[springArray[i].ConnectIndexStart] - v1[springArray[i].ConnectIndexEnd]);
            v2[springArray[i].ConnectIndexStart] -= temp;
            v2[springArray[i].ConnectIndexEnd] += temp;
        }
    }

    private Matrix4x4 OuterProduct(Vector3 v1, Vector3 v2)
    {
        Matrix4x4 matrix = new Matrix4x4();
        matrix.SetRow(0, new Vector4(v1.x * v2.x, v1.x * v2.y, v1.x * v2.z, 0));
        matrix.SetRow(1, new Vector4(v1.y * v2.x, v1.y * v2.y, v1.y * v2.z, 0));
        matrix.SetRow(2, new Vector4(v1.z * v2.x, v1.z * v2.y, v1.z * v2.z, 0));
        matrix.SetRow(3, Vector3.zero);
        return matrix;
    }

    private Matrix4x4 MultipltyScalerMaxtrix(Matrix4x4 m, float scaler)
    {
        Matrix4x4 result = new Matrix4x4();
        result.SetRow(0, m.GetRow(0) * scaler);
        result.SetRow(1, m.GetRow(1) * scaler);
        result.SetRow(2, m.GetRow(2) * scaler);
        result.SetRow(3, m.GetRow(3) * scaler);
        return result;
    }

    private Matrix4x4 AddMatrix(Matrix4x4 m1, Matrix4x4 m2)
    {
        Matrix4x4 m = new Matrix4x4();
        m.SetRow(0, m1.GetRow(0) + m2.GetRow(0));
        m.SetRow(1, m1.GetRow(1) + m2.GetRow(1));
        m.SetRow(2, m1.GetRow(2) + m2.GetRow(2));
        m.SetRow(3, m1.GetRow(3) + m2.GetRow(3));
        return m;
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
