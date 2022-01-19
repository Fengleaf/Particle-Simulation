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
}
