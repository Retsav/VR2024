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
    [SerializeField] private int maxTurnsInARow = 2;
    private int _turnsCount;
    
    
    
    [Header("Prefabs")]
    [SerializeField] private GameObject roadTrackPrefab;
    [SerializeField] private GameObject turnLeftPrefab;
    [SerializeField] private GameObject turnRightPrefab;
    
    
    
    
    
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
    private RoadType _randomRoadType;
    private GameObject _nextObject;

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
        XmaxBounds = terrainSize.x / 2 + segmentSize.x / 2 - 1;
        XminBounds = -terrainSize.x / 2 + segmentSize.x / 2;
        ZmaxBounds = terrainSize.y / 2 + segmentSize.y / 2 - 1;
        ZminBounds = -terrainSize.y / 2 + segmentSize.y / 2;
    }
    
    private void GenerateTrack()
    {
        _roadLinkedList.Clear();
        foreach (Transform child in roadsParent) Destroy(child.gameObject);
        
        var debugTryNumber = 0;
        var point = Vector3.zero;
        float rotationAngle = 0f;
        

        while (true)
        {
            if (debugTryNumber > 500) break;
            int randomX = (int)random.NextFloat((terrainSize.x / 2) - 1, -terrainSize.x / 2);
            int randomY = (int)random.NextFloat((terrainSize.y / 2) - 1, -terrainSize.y / 2);
            point = new Vector3(randomX + segmentSize.x / 2, pathHeight, randomY + segmentSize.y / 2);
            
            if (point.x < XmaxBounds - segmentStartingPointBorderMax &&
                point.x > XminBounds + segmentStartingPointBorderMax &&
                point.z < ZmaxBounds - segmentStartingPointBorderMax &&
                point.z > ZminBounds + segmentStartingPointBorderMax)
            {
                debugTryNumber++;
                continue;
            }
            break;
        }

        if (_roadTrack != null)
            Destroy(_roadTrack);
        
        var rotation = Quaternion.Euler(0, rotationAngle, 0);
        _roadTrack = Instantiate(roadTrackPrefab, point, rotation, roadsParent);
        _roadLinkedList.AddNode(point, rotation, RoadType.Straight);
        
        _nextObject = _roadTrack;
        _randomRoadType = RoadType.Straight;

        
        for (int i = 0; i < pathLength; i++)
        {
            var roadRecentTrack = _roadLinkedList.GetTail();
            
            _randomRoadType = PickNewRoadType(_nextObject);
            
            var positionsWithoutObjects = ScanForAvailableSegment(_nextObject.transform, roadRecentTrack.RoadType);

            
            int index = -1;
            
            if (positionsWithoutObjects.Count > 0)
            {

                var bannedPositions = new List<(Vector3, TurnType)>();

                foreach (var position in positionsWithoutObjects)
                {
                    switch (roadRecentTrack.RoadType)
                    {
                        
                        case RoadType.TurnLeft:
                            if (_randomRoadType == RoadType.Straight) rotationAngle = roadRecentTrack.Rotation.eulerAngles.y - 90f;
                            if (_randomRoadType == RoadType.TurnLeft) rotationAngle = roadRecentTrack.Rotation.eulerAngles.y - 90f; 
                            break;
                        case RoadType.TurnRight:
                            if (_randomRoadType == RoadType.Straight) rotationAngle = roadRecentTrack.Rotation.eulerAngles.y + 90f;
                            if (_randomRoadType == RoadType.TurnRight) rotationAngle = roadRecentTrack.Rotation.eulerAngles.y + 90f;
                            break;
                    }
                }


                foreach (var bannedPos in bannedPositions) positionsWithoutObjects.Remove(bannedPos);
                if (positionsWithoutObjects.Count > 0) index = random.NextInt(0, positionsWithoutObjects.Count);
            }


            Vector3 pos = Vector3.zero;
            if (positionsWithoutObjects.Count > 0 && index != -1)
                pos = positionsWithoutObjects[index].Item1;
            else
                break;
            Quaternion newRotation = Quaternion.Euler(0, rotationAngle, 0);
            _nextObject = Instantiate(GetRoadPrefabFromRoadType(_randomRoadType), pos, newRotation, roadsParent);
            _roadLinkedList.AddNode(_nextObject.transform.position, newRotation, _randomRoadType);
        }
    }

    private RoadType PickNewRoadType(GameObject lastGameObject)
    {
        List<RoadType> validRoadTypes = new List<RoadType>();
        Vector3 forwardVector = Vector3.zero;
        Vector3 leftVector = Vector3.zero;
        Vector3 rightVector = Vector3.zero;
        Vector3 position = lastGameObject.transform.position;

        var forward = lastGameObject.transform.forward;
        var right = lastGameObject.transform.right;

        var tail = _roadLinkedList.GetTail();
        switch (tail.RoadType)
        {
            case RoadType.Straight:
                forwardVector = position + forward * (2 * segmentSize.x);
                leftVector = position + forward * segmentSize.x - right * segmentSize.y;
                rightVector = position + forward * segmentSize.x + right * segmentSize.y;
                break;
            case RoadType.TurnLeft:
                forwardVector = position - right * (2 * segmentSize.x);
                leftVector = position - right * segmentSize.x - forward * segmentSize.y;
                rightVector = position - right * segmentSize.x + forward * segmentSize.y;
                break;
            case RoadType.TurnRight:
                forwardVector = position + right * (2 * segmentSize.x);
                leftVector = position + right * segmentSize.x + forward * segmentSize.y;
                rightVector = position + right * segmentSize.x - forward * segmentSize.y;
                break;
        }
        

        
        if(IsPositionValid(forwardVector))
            validRoadTypes.Add(RoadType.Straight);
        if(IsPositionValid(leftVector) && (tail.RoadType == RoadType.Straight || !validRoadTypes.Contains(RoadType.Straight)))
            validRoadTypes.Add(RoadType.TurnLeft);
        if(IsPositionValid(rightVector) && (tail.RoadType == RoadType.Straight || !validRoadTypes.Contains(RoadType.Straight)))
            validRoadTypes.Add(RoadType.TurnRight);

        if (validRoadTypes.Count <= 0)
        {
            Debug.LogWarning("Road death.");
            return RoadType.Straight;
        }
        return validRoadTypes[random.NextInt(validRoadTypes.Count)];
    }

    private void OnDrawGizmos()
    {
        if (_roadLinkedList != null)
        {
            var currentNode = _roadLinkedList.GetHead();
            while (currentNode != null)
            {
                currentNode = currentNode.Next;
                if (currentNode == null) return;
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(currentNode.Position, 0.2f);
                Gizmos.color = Color.blue;
            }
        }
    }

  
    private List<(Vector3, TurnType)> ScanForAvailableSegment(Transform segmentTransform, RoadType currentRoadType)
    {
        List<(Vector3, TurnType)> availablePositions = new List<(Vector3, TurnType)>();
        Vector3 currentPos = segmentTransform.position;
        Vector3 newSegmentPos;
        TurnType turnType;
        switch (currentRoadType)
        {
            case RoadType.Straight:
                // Forward: +Z
                newSegmentPos = currentPos + segmentTransform.forward * segmentSize.x;
                newSegmentPos.y = pathHeight;
                turnType = TurnType.PlusZ;
                if (IsPositionValid(newSegmentPos))
                {
                    availablePositions.Add((newSegmentPos, turnType));
                }
                break;
        
            case RoadType.TurnLeft:
                // Forward: -X
                newSegmentPos = currentPos - segmentTransform.right * segmentSize.y;
                newSegmentPos.y = pathHeight;
                turnType = TurnType.MinusX;
                if (IsPositionValid(newSegmentPos))
                {
                    availablePositions.Add((newSegmentPos, turnType));
                }
                break;
        
            case RoadType.TurnRight:
                // Forward: -Z
                newSegmentPos = currentPos + segmentTransform.right * segmentSize.y;
                newSegmentPos.y = pathHeight;
                turnType = TurnType.PlusX;
                if (IsPositionValid(newSegmentPos))
                {
                    availablePositions.Add((newSegmentPos, turnType));
                }
                break;
            default:
                Debug.LogWarning($"Unhandled RoadType: {currentRoadType}");
                break;
        }
        return availablePositions;
    }

    private const float Tolerance = 0.001f;
    private bool IsPositionValid(Vector3 position)
    {
        return position.x >= XminBounds - Tolerance &&
               position.x <= XmaxBounds + Tolerance &&
               position.z >= ZminBounds - Tolerance &&
               position.z <= ZmaxBounds + Tolerance &&
               !_roadLinkedList.ContainsPosition(position);
    }
    

    private GameObject GetRoadPrefabFromRoadType(RoadType roadType)
    {
        switch (roadType)
        {
            case RoadType.TurnLeft:
                return turnLeftPrefab;
            case RoadType.TurnRight:
                return turnRightPrefab;
            case RoadType.Straight:
                return roadTrackPrefab;
        }
        return null;
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
    PlusX,
    MinusX,
    PlusZ
}
