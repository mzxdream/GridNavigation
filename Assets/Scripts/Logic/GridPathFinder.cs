using System.Collections.Generic;
using UnityEngine;

public class GridPathNode
{
    public Vector2Int pos;
    public float fCost;
}

public class GridPathPriorityQueue
{
    private int capacity = 256;
    private int count = 0;
    private GridPathNode[] nodeHeap = new GridPathNode[256];

    private void Grow()
    {
        capacity <<= 1;
        var newNodeHeap = new GridPathNode[capacity];
        nodeHeap.CopyTo(newNodeHeap, 0);
        nodeHeap = newNodeHeap;
    }
    public void Push(GridPathNode node)
    {
        if (count == capacity)
        {
            Grow();
        }
        //TODO
        nodeHeap[count++] = node;
    }
    public GridPathNode Pop()
    {
        if (count == 0)
        {
            return null;
        }
        //TODO
        int index = 0;
        for (int i = 1; i < count; i++)
        {
            if (nodeHeap[index].fCost > nodeHeap[i].fCost)
            {
                index = i;
            }
        }
        var node = nodeHeap[index];
        nodeHeap[index] = nodeHeap[count - 1];
        nodeHeap[count - 1] = null;
        count--;
        return node;
    }
}

public class GridPath
{
    public Vector3 goalPos;
    public List<Vector2Int> nodes;
}

public class GridPathFinder
{


    public GridPathFinder()
    {
    }
}