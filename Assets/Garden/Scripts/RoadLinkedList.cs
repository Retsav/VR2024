using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadLinkedList
{
    private RoadNode _head;
    private RoadNode _tail;

    public RoadLinkedList()
    {
        _head = null;
        _tail = null;
    }

    public RoadNode FindNode(Vector3 position)
    {
        var currentNode = _head;
        while (currentNode != null)
        {
            if (currentNode.Position == position)
                return currentNode;
            currentNode = currentNode.Next;
        }
        return null;
    }
    
    public bool ContainsPosition(Vector3 position)
    {
        var currentNode = _head;
        while (currentNode != null)
        {
            if (currentNode.Position == position)
                return true;
            currentNode = currentNode.Next;
        }
        return false;
    }

    public void AddNode(Vector3 pos, Quaternion rotation, RoadType roadType)
    {
        RoadNode newNode = new RoadNode(pos, rotation, roadType);
        if (_tail != null)
        {
            _tail.Next = newNode;
            newNode.Previous = _tail;
        }
        _tail = newNode;
        if (_head == null) _head = newNode;
    }

    public RoadNode GetHead() => _head;
    public RoadNode GetTail() => _tail;

    public void PrintList()
    {
        var currentNode = _head;
        while (currentNode != null)
        {
            Debug.Log($"{currentNode.Position.ToString()} || {currentNode.RoadType.ToString()}");
            currentNode = currentNode.Next;
        }
    }

    public void Clear()
    {
        var currentNode = _head;
        while (currentNode != null)
        {
            var nextNode = currentNode.Next;
            currentNode.Previous = null;
            currentNode.Next = null;
            currentNode = nextNode;
        }
        _head = null;
        _tail = null;
    }
}
