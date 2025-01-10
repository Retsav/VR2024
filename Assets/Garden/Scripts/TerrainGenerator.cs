using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;


public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private uint seedValue;
    
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


    private RoadLinkedList _roadLinkedList = new RoadLinkedList();
    private const float pathHeight = 1f;
    [SerializeField] private Transform roadsParent;
    
    private Random random;

    private float XmaxBounds;
    private float XminBounds;
    
    private float ZmaxBounds;
    private float ZminBounds;

    private void Start()
    {
        random = new Random();
        random.InitState(seedValue);
        GenerateTerrain();
        GenerateTrack();
    }

    private void Update()
    {
        if (regenerateOnUpdate)
        {
            random.InitState(seedValue);
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
        _roadLinkedList.Clear();
        foreach (Transform child in roadsParent) Destroy(child.gameObject);
        var debugTryNumber = 0;
        var point = Vector3.zero;
        float rotationAngle = 90f;
        while (true)
        {
            if (debugTryNumber > 500) break;
            int randomX = (int)random.NextFloat((terrainSize.x / 2) - 1, -terrainSize.x / 2);
            int randomY = (int)random.NextFloat((terrainSize.y / 2) - 1, -terrainSize.y / 2);
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
        _roadLinkedList.AddNode(point, RoadType.Straight);
        var nextObject = _roadTrack;
        for (int i = 0; i < pathLength; i++)
        {
            var positionsWithoutObjects = ScanForAvailableSegment(nextObject.transform.position);
            int index = -1;
            if (positionsWithoutObjects.Count > 0)
            {
                var roadRecentTrack = _roadLinkedList.GetTail();
                var bannedPositions = new List<(Vector3, TurnType)>();
                foreach (var position in positionsWithoutObjects)
                {
                    //If last added track was a turn, it cannot make another turn so we ban these available positions even if they are free.
                    if(roadRecentTrack.RoadType == RoadType.Straight) continue;
                    if (position.Item2 == TurnType.Left || position.Item2 == TurnType.Right) bannedPositions.Add(position);
                }

                for (int j = 0; j < bannedPositions.Count; j++) positionsWithoutObjects.Remove(bannedPositions[j]);
                
                index = random.NextInt(0, positionsWithoutObjects.Count);
            }

            var pos = index == -1 ? Vector3.zero : positionsWithoutObjects[index].Item1;
            nextObject = Instantiate(roadTrackPrefab, pos, Quaternion.Euler(0, rotationAngle, 0), roadsParent);
            _roadLinkedList.AddNode(nextObject.transform.position, RoadType.Straight);
        }
    }

    private List<(Vector3, TurnType)> ScanForAvailableSegment(Vector3 startSegment)
    {
        List<(Vector3, TurnType)> availablePositions = new List<(Vector3, TurnType)>();
        var forwardSegment = new Vector3(startSegment.x + segmentSize.x, pathHeight, startSegment.z);
        var backwardSegment = new Vector3(startSegment.x - segmentSize.x, pathHeight, startSegment.z);
        var rightSegment = new Vector3(startSegment.x, pathHeight, startSegment.z - segmentSize.y);
        var leftSegment = new Vector3(startSegment.x, pathHeight, startSegment.z + segmentSize.y);
        if (forwardSegment.x < XmaxBounds)
            if(!_roadLinkedList.ContainsPosition(forwardSegment)) availablePositions.Add((forwardSegment, TurnType.Up));
        if (backwardSegment.x > XminBounds)
            if(!_roadLinkedList.ContainsPosition(backwardSegment)) availablePositions.Add((backwardSegment, TurnType.Down));
        if (rightSegment.z > ZminBounds)
            if (!_roadLinkedList.ContainsPosition(rightSegment)) availablePositions.Add((rightSegment, TurnType.Right));
        if (leftSegment.z < ZmaxBounds)
            if (!_roadLinkedList.ContainsPosition(leftSegment)) availablePositions.Add((leftSegment, TurnType.Left));
        return availablePositions;
    }
    
    
}

public enum RoadType
{
    Straight,
    TurnRight,
    TurnLeft
}

public enum TurnType
{
    Up,
    Down,
    Left,
    Right
}
