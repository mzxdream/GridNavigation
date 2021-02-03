using System.Collections.Generic;
using UnityEngine;

public class GridPathNode
{
    private enum Mask { Blocked = 1, Closed = 2, ParentFwd = 4, ParentRgt = 8 };

    private int index;
    private Mask mask;
    private int gCost;
    private int hCost;

    public int Index { get => index; set => index = value; }
    public bool IsBlocked
    {
        get { return (mask & Mask.Blocked) != 0; }
        set { if (value) { mask |= Mask.Blocked; } else { mask &= ~Mask.Blocked; } }
    }
    public bool IsClosed
    {
        get { return (mask & Mask.Closed) != 0; }
        set { if (value) { mask |= Mask.Closed; } else { mask &= ~Mask.Closed; } }
    }
    public int FCost { get => gCost + hCost; }
}

public class GridPathPriorityQueue
{
    private int capacity;
    private int count;
    private GridPathNode[] nodeHeap;

    public GridPathPriorityQueue(int capacity = 256)
    {
        this.capacity = capacity;
        this.count = 0;
        this.nodeHeap = new GridPathNode[capacity];
    }

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
            if (nodeHeap[index].FCost > nodeHeap[i].FCost)
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
    public void Clear()
    {
        for (int i = 0; i < count; i++)
        {
            nodeHeap[i] = null;
        }
        count = 0;
    }
}

public class GridPath
{
    private Vector2Int startPos;
    private Vector2Int goalPos;
    private int goalRadius;
    private int searchSize;
    public List<int> path;

    public GridPath(Vector2Int startPos, Vector2Int goalPos, int goalRadius, int searchSize)
    {
        this.startPos = startPos;
        this.goalPos = goalPos;
        this.goalRadius = goalRadius;
        this.searchSize = searchSize;
    }
}

public class GridPathFinder
{
    private int gridX;
    private int gridZ;
    private int blockSize;
    private GridPathNode[] nodes;
    private GridPathPriorityQueue openQueue;
    private List<GridPathNode> closedQueue;

    public GridPathFinder(int gridX, int gridZ, int blockSize)
    {
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.blockSize = blockSize;
        this.nodes = new GridPathNode[this.gridX * this.gridZ];
        this.openQueue = new GridPathPriorityQueue();
        this.closedQueue = new List<GridPathNode>();
    }
    public bool Init()
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].Index = i;
            //TODO check block
        }
        return true;
    }
    public bool Search(GridMoveAgent agent, ref GridPath path)
    {


        foreach (var node in closedQueue)
        {
            node.IsClosed = false;
        }
        closedQueue.Clear();
        return true;
    }
}