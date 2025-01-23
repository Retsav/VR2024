using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadNode
{
    public Vector3 Position {get; set;}
    public Quaternion Rotation { get; set; }
    public RoadType RoadType {get; set;}
    public RoadNode Next {get; set;}
    public RoadNode Previous {get; set;}

    public RoadNode(Vector3 position, Quaternion rotation, RoadType roadType)
    {
        Position = position;
        Rotation = rotation;
        RoadType = roadType;
        Next = null;
        Previous = null;
    }
}
