using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeSomeNoise : MonoBehaviour
{
    public float power = 3;
    public float scale = 1;
    public float timeScale = 1;

    private float xOffset;
    private float yOffset;
    private MeshFilter mf;

    void Start()
    {
        mf = GetComponent<MeshFilter>();
        MakeNoise();
    }

    // Update is called once per frame
    void Update()
    {
        MakeNoise();
        xOffset += Time.deltaTime * timeScale;
        yOffset += Time.deltaTime * timeScale;
    }

    void MakeNoise()
    {
        Vector3[] verticies = mf.mesh.vertices;

        for (int i = 0; i < verticies.Length; i++)
        {
            verticies[i].y = CalculateHeight(verticies[i].x, verticies[i].z*power);
        }

        mf.mesh.vertices = verticies;
    }
    float CalculateHeight(float x, float y)
    {
        float xCord = x * scale + xOffset;
        float yCord = y * scale + yOffset;

        return Mathf.PerlinNoise(xCord, yCord);
    }

}
