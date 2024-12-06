using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private bool regenerateOnUpdate = true;
    
    [SerializeField] private Vector2 terrainSize;
    [SerializeField] private Vector2 segmentSize;
    [SerializeField] private Mesh mesh;
    
    [SerializeField] private Material grassmaterial;
    [SerializeField] private Material dirtmaterial;
    
    
    private List<Matrix4x4> matrixList = new List<Matrix4x4>();


    private void Start()
    {
        GenerateTerrain();
    }

    private void Update()
    {
        if(regenerateOnUpdate) GenerateTerrain();
    }

    private void GenerateTerrain()
    {
        matrixList.Clear();
        for (int y = 0; y < terrainSize.y; y++)
        {
            for (int x = 0; x < (int)terrainSize.x; x++)
            {
                Vector3 t = new Vector3(transform.position.x / 2 + -terrainSize.x / 2 + x + (segmentSize.x /2), 0,transform.position.z / 2 + -terrainSize.y / 2 + y + (segmentSize.y /2));
                Quaternion r = Quaternion.identity;
                Vector3 s = new Vector3(segmentSize.x, 1, segmentSize.y);
                matrixList.Add(Matrix4x4.TRS(t, r, s));
            }
        }
        Graphics.DrawMeshInstanced(mesh, 0, grassmaterial, matrixList.ToArray(), matrixList.Count);
        Graphics.DrawMeshInstanced(mesh, 1, dirtmaterial, matrixList.ToArray(), matrixList.Count);
    }

    private void OnDrawGizmos()
    {
        /*for (int y = 0; y < terrainSize.y; y++)
        {
            for (int x = 0; x < (int)terrainSize.x; x++)
            {
                    Vector3 t = new Vector3(-terrainSize.x / 2 + x + (segmentSize.x /2), 0, -terrainSize.y / 2 + y + (segmentSize.y /2));
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(t, 0.2f);
            }
        }*/
    }
}
