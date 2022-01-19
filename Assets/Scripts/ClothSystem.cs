using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                GameObject particle = Instantiate(ParticlePrefab, position, Quaternion.identity, transform);
                particle.name = $"Particle {i} * {j}";
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
        List<Vector3> tempSpeedArray = new List<Vector3>(speedArray.Count);
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
            // 不知道在幹嘛
            tempSpeedArray[startIndex] += tempForce / Mass * Time.fixedDeltaTime;
            tempSpeedArray[endIndex] -= tempForce / Mass * Time.fixedDeltaTime;
        }
    }

    private void AddSpringWithIndex(int index)
    {
        int NextIndex;
        // Structural Springs
        // 向上
        NextIndex = index + 1;
        if (NextIndex < SideCount)
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance));
        // 向右
        NextIndex = index + SideCount;
        if (NextIndex / SideCount < SideCount)
            springArray.Add(new SpringSystem(index, NextIndex, UnitDistance));
        // Shear Springs
        // 右上
        NextIndex = index + SideCount + 1;
        if (NextIndex / SideCount < SideCount)
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
}
