using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private bool regenerateOnUpdate = true;
    
    [SerializeField] private Vector2 terrainSize;
    [SerializeField] private Vector2 segmentSize;
    [SerializeField] private Mesh mesh;
    
    [SerializeField] private Material grassmaterial;
    [SerializeField] private Material dirtmaterial;
    
    [Header("Golf Path Options")] 
    [SerializeField] private int pathLength = 10;
    [SerializeField] private float segmentStartingPointBorderMax;
    [SerializeField] private GameObject roadTrackPrefab;
    
    
    private GameObject _roadTrack;
    private List<Matrix4x4> matrixList = new List<Matrix4x4>();


    private List<Vector3> occupiedPositions = new List<Vector3>();
    private const float pathHeight = 1f;
    [SerializeField] private Transform roadsParent;

    private float XmaxBounds;
    private float XminBounds;
    
    private float ZmaxBounds;
    private float ZminBounds;

    private void Start()
    {
        GenerateTerrain();
        GenerateTrack();
    }

    private void Update()
    {
        if (regenerateOnUpdate)
        {
            GenerateTerrain();
            GenerateTrack();
        }
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
        XmaxBounds = terrainSize.x / 2 + segmentSize.x / 2;
        XminBounds = -terrainSize.x / 2 + segmentSize.x / 2;
        ZmaxBounds = terrainSize.y / 2 + segmentSize.y / 2;
        ZminBounds = -terrainSize.y / 2 + segmentSize.y / 2;
    }

    private void GenerateTrack()
    {
        occupiedPositions.Clear();
        foreach (Transform child in roadsParent) Destroy(child.gameObject);
        var debugTryNumber = 0;
        var point = Vector3.zero;
        float rotationAngle = 90f;
        while (true)
        {
            if (debugTryNumber > 500) break;
            int randomX = (int)Random.Range((terrainSize.x / 2) - 1, -terrainSize.x / 2);
            int randomY = (int)Random.Range((terrainSize.y / 2) - 1, -terrainSize.y / 2);
            point = new Vector3(randomX + segmentSize.x / 2, pathHeight, randomY + segmentSize.y / 2);
            if (point.x < XmaxBounds - segmentStartingPointBorderMax &&
                point.x > XminBounds + segmentStartingPointBorderMax)
            {
                if (point.z < ZmaxBounds - segmentStartingPointBorderMax &&
                    point.z > ZminBounds + segmentStartingPointBorderMax)
                {
                    debugTryNumber++;
                    continue;
                }
            }
            break;
        }

        if (_roadTrack != null)
            Destroy(_roadTrack);
        _roadTrack = Instantiate(roadTrackPrefab, point, Quaternion.Euler(0, rotationAngle, 0), roadsParent);
        occupiedPositions.Add(point);
        var nextObject = _roadTrack;
        for (int i = 0; i < pathLength; i++)
        {
            var positionsAvailable = ScanForAvailableSegment(nextObject.transform.position);
            int index = -1;
            if (positionsAvailable.Count > 0)
            {
                index = Random.Range(0, positionsAvailable.Count);
            }
            nextObject = Instantiate(roadTrackPrefab, index == -1 ? Vector3.zero : positionsAvailable[index], Quaternion.Euler(0, rotationAngle, 0), roadsParent);
            occupiedPositions.Add(nextObject.transform.position);
        }
    }

    private List<Vector3> ScanForAvailableSegment(Vector3 startSegment)
    {
        List<Vector3> availablePositions = new List<Vector3>();
        var forwardSegment = new Vector3(startSegment.x + segmentSize.x, pathHeight, startSegment.z);
        var backwardSegment = new Vector3(startSegment.x - segmentSize.x, pathHeight, startSegment.z);
        var leftSegment = new Vector3(startSegment.x, pathHeight, startSegment.z - segmentSize.y);
        var rightSegment = new Vector3(startSegment.x, pathHeight, startSegment.z + segmentSize.y);
        if (forwardSegment.x < XmaxBounds)
            if(!occupiedPositions.Contains(forwardSegment)) availablePositions.Add(forwardSegment);
        if (backwardSegment.x > XminBounds)
            if(!occupiedPositions.Contains(backwardSegment)) availablePositions.Add(backwardSegment);
        if (leftSegment.z < ZminBounds)
            if (!occupiedPositions.Contains(leftSegment)) availablePositions.Add(leftSegment);
        if (rightSegment.z > ZmaxBounds)
            if (!occupiedPositions.Contains(rightSegment)) availablePositions.Add(rightSegment);
        return availablePositions;
    }
    
}
