using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waves : MonoBehaviour
{
    public const int waterChunkSize = 17;
    private int verticiesPerLine;
    [Range(0, 6)]
    public int levelOfDetail;
    //Public Properties
    public float UVScale = 2f;
    public Octave[] Octaves;

    //Mesh
    protected MeshFilter MeshFilter;
    protected Mesh Mesh;

    // Start is called before the first frame update
    void Start()
    {

        //Mesh Setup
        Mesh = new Mesh();
        Mesh.name = gameObject.name;

        GenerateMesh();
    }

    void GenerateMesh()
    {
        Debug.Log("!!!!");
        Mesh.Clear();
        Mesh.vertices = GenerateVerts();
        Mesh.triangles = GenerateTries();
        Mesh.uv = GenerateUVs();
        Mesh.RecalculateNormals();
        Mesh.RecalculateBounds();

        if(MeshFilter == null)
            MeshFilter = gameObject.AddComponent<MeshFilter>();
        MeshFilter.mesh = Mesh;
    }

    public float GetHeight(Vector3 position)
    {
        //scale factor and position in local space
        var scale = new Vector3(1 / transform.lossyScale.x, 0, 1 / transform.lossyScale.z);
        var localPos = Vector3.Scale((position - transform.position), scale);

        //get edge points
        var p1 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Floor(localPos.z));
        var p2 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Ceil(localPos.z));
        var p3 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Floor(localPos.z));
        var p4 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Ceil(localPos.z));

        //clamp if the position is outside the plane
        p1.x = Mathf.Clamp(p1.x, 0, waterChunkSize);
        p1.z = Mathf.Clamp(p1.z, 0, waterChunkSize);
        p2.x = Mathf.Clamp(p2.x, 0, waterChunkSize);
        p2.z = Mathf.Clamp(p2.z, 0, waterChunkSize);
        p3.x = Mathf.Clamp(p3.x, 0, waterChunkSize);
        p3.z = Mathf.Clamp(p3.z, 0, waterChunkSize);
        p4.x = Mathf.Clamp(p4.x, 0, waterChunkSize);
        p4.z = Mathf.Clamp(p4.z, 0, waterChunkSize);

        //get the max distance to one of the edges and take that to compute max - dist
        var max = Mathf.Max(Vector3.Distance(p1, localPos), Vector3.Distance(p2, localPos), Vector3.Distance(p3, localPos), Vector3.Distance(p4, localPos) + Mathf.Epsilon);
        var dist = (max - Vector3.Distance(p1, localPos))
                 + (max - Vector3.Distance(p2, localPos))
                 + (max - Vector3.Distance(p3, localPos))
                 + (max - Vector3.Distance(p4, localPos) + Mathf.Epsilon);
        //weighted sum
        var height = Mesh.vertices[index(p1.x, p1.z)].y * (max - Vector3.Distance(p1, localPos))
                   + Mesh.vertices[index(p2.x, p2.z)].y * (max - Vector3.Distance(p2, localPos))
                   + Mesh.vertices[index(p3.x, p3.z)].y * (max - Vector3.Distance(p3, localPos))
                   + Mesh.vertices[index(p4.x, p4.z)].y * (max - Vector3.Distance(p4, localPos));

        //scale
        return height * transform.lossyScale.y / dist;

    }

    private Vector3[] GenerateVerts()
    {
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        verticiesPerLine = (waterChunkSize - 1) / meshSimplificationIncrement + 1;
        var verts = new Vector3[verticiesPerLine * verticiesPerLine];

        int X = 0;
        //equaly distributed verts
        for (int x = 0; x < verticiesPerLine; x++)
        {
            int Y = 0;

            for (int z = 0; z < verticiesPerLine; z++)
            {
                verts[index(x, z)] = new Vector3(X, 0, Y);
                Y += meshSimplificationIncrement;
            }
            X += meshSimplificationIncrement;
        }

        return verts;
    }

    private int[] GenerateTries()
    {
        var tries = new int[(verticiesPerLine-1) * (verticiesPerLine - 1) * 6];

        int vert = 0;
        int tris = 0;

        //two triangles are one tile
        for (int x = 0; x < verticiesPerLine-1; x++)
        {
            for (int z = 0; z < verticiesPerLine-1; z++)
            {
                
                tries[tris + 0] = vert + 0;
                tries[tris + 1] = vert + (verticiesPerLine - 1) + 2;
                tries[tris + 2] = vert + (verticiesPerLine - 1) + 1;
                tries[tris + 3] = vert + 0;
                tries[tris + 4] = vert + 1;
                tries[tris + 5] = vert + (verticiesPerLine - 1) + 2;
                
                vert++;
                tris += 6;
            }
            vert++;
        }

        return tries;
    }

    private Vector2[] GenerateUVs()
    {
        var uvs = new Vector2[Mesh.vertices.Length];

        //always set one uv over n tiles than flip the uv and set it again
        for (int x = 0; x < verticiesPerLine; x++)
        {
            for (int z = 0; z < verticiesPerLine; z++)
            {
                var vec = new Vector2((x / UVScale) % 2, (z / UVScale) % 2);
                uvs[index(x, z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }

        return uvs;
    }
    private int index(int x, int z)
    {
        return x * verticiesPerLine + z;
    }

    private int index(float x, float z)
    {
        return index((int)x, (int)z);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
          GenerateMesh();
        var verts = Mesh.vertices;
        for (int x = 0; x < verticiesPerLine; x++)
        {
            for (int z = 0; z < verticiesPerLine; z++)
            {
                var y = 0f;
                for (int o = 0; o < Octaves.Length; o++)
                {
                    if (Octaves[o].alternate)
                    {
                        var perl = Mathf.PerlinNoise((x * Octaves[o].scale.x) / waterChunkSize, (z * Octaves[o].scale.y) / waterChunkSize) * Mathf.PI * 2f;
                        y += Mathf.Cos(perl + Octaves[o].speed.magnitude * Time.time) * Octaves[o].height;
                    }
                    else
                    {
                        var perl = Mathf.PerlinNoise((x * Octaves[o].scale.x + Time.time * Octaves[o].speed.x) / waterChunkSize, (z * Octaves[o].scale.y + Time.time * Octaves[o].speed.y) / waterChunkSize) - 0.5f;
                        y += perl * Octaves[o].height;
                    }
                }
                int offsetX = (int)verts[index(x, z)].x - x;
                int offsetZ = (int)verts[index(x, z)].z - z;
                verts[index(x, z)] = new Vector3(x + offsetX, y, z + offsetZ);
            }
        }
        Mesh.vertices = verts;
        Mesh.RecalculateNormals();
    }

    [Serializable]
    public struct Octave
    {
        public Vector2 speed;
        public Vector2 scale;
        public float height;
        public bool alternate;
    }
}
