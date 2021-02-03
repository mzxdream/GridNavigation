using System.Collections.Generic;
using UnityEngine;

public class GridPathNode
{
    private enum Mask { Blocked = 1, Closed = 2 };

    private int x;
    private int z;
    private int gCost;
    private int hCost;
    private Mask mask;
    private GridPathNode parent;

    public int X { get => x; set => x = value; }
    public int Z { get => z; set => z = value; }
    public int FCost { get => gCost + hCost; }
    public int GCost { get => gCost; set => gCost = value; }
    public int HCost { set => hCost = value; }
    public GridPathNode Parent { get => parent; set => parent = value; }
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

public class GridPathFinder
{
    private int[] neighbors = { 0, 1, 0, -1, 1, 0, -1, 0, -1, 1, 1, 1, -1, -1, 1, -1 };
    private int gridX;
    private int gridZ;
    private GridPathNode[] nodes;
    private GridPathPriorityQueue openQueue;
    private List<GridPathNode> closedQueue;

    public GridPathFinder(int gridX, int gridZ)
    {
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.nodes = new GridPathNode[this.gridX * this.gridZ];
        for (int z = 0; z < gridZ; z++)
        {
            for (int x = 0; x < gridX; x++)
            {
                var node = nodes[x + z * gridX];
                node.X = x;
                node.Z = z;
            }
        }
        this.openQueue = new GridPathPriorityQueue();
        this.closedQueue = new List<GridPathNode>();
    }
    public bool Init()
    {
        //TODO set static blocked;
        return true;
    }
    private static int CalcDistanceCost(int fromX, int fromZ, int toX, int toZ)
    {
        int x = Mathf.Abs(toX - fromX);
        int z = Mathf.Abs(toZ - fromZ);
        return x > z ? 14 * z + 10 * (x - z) : 14 * x + 10 * (z - x);
    }
    private bool IsAtGoal()
    {
        return false;
    }
    private bool IsNeighborWalkable(int fromX, int fromZ, int toX, int toZ)
    {
        return false;
    }
    public List<Vector2Int> Search(int unitSize, int startX, int startZ, int goalX, int goalZ, int goalRadius, int searchRadius, int searchMaxNodes)
    {
        Debug.Assert(unitSize >= 3 && (unitSize & 1) == 1);
        Debug.Assert(startX >= 0 && startX < gridX && startZ >= 0 && startZ < gridZ);
        Debug.Assert(goalX >= 0 && goalX < gridX && goalZ >= 0 && goalZ < gridZ);
        Debug.Assert(goalRadius >= 0 && searchRadius >= 0 && searchMaxNodes >= 0);

        openQueue.Clear();
        foreach (var n in closedQueue)
        {
            n.IsClosed = false;
        }
        closedQueue.Clear();

        bool isFound = false;
        var node = nodes[startX + startZ * gridX];
        node.IsClosed = true;
        closedQueue.Add(node);
        for (int i = 0; i < searchMaxNodes && node != null; i++)
        {
            if (IsAtGoal())
            {
                isFound = true;
                break;
            }
            for (int j = 0; j < neighbors.Length; j += 2)
            {
                var x = node.X + neighbors[j];
                var z = node.Z + neighbors[j + 1];
                if (x < 0 || x >= gridX || z < 0 || z >= gridZ)
                {
                    continue;
                }
                var n = nodes[x + z * gridX];
                if (n.IsBlocked || n.IsClosed)
                {
                    continue;
                }
                if (!IsNeighborWalkable(node.X, node.Z, x, z))
                {
                    n.IsClosed = true;
                    closedQueue.Add(n);
                    continue;
                }
                n.GCost = node.GCost + CalcDistanceCost(node.X, node.Z, x, z);
                n.HCost = CalcDistanceCost(x, z, goalX, goalZ);
                n.IsClosed = true;
                n.Parent = node;
                closedQueue.Add(n);
                openQueue.Push(n);
            }
            node = openQueue.Pop();
        }
        if (isFound)
        {
            var path = new List<GridPathNode>();
        }
        return null;
    }
}