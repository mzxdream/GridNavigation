using System.Collections.Generic;
using UnityEngine;

public class GridPathNode
{
    private enum Mask { Blocked = 1, Closed = 2, ParentFwd = 4, ParentRgt = 8 };

    private int x;
    private int z;
    private int gCost;
    private int hCost;
    private Mask mask;

    public int X { get => x; set => x = value; }
    public int Z { get => z; set => z = value; }
    public int FCost { get => gCost + hCost; }
    public int GCost { get => gCost; set => gCost = value; }
    public int HCost { set => hCost = value; }
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

public class GridPath
{
    private int startX;
    private int startZ;
    private int goalX;
    private int goalZ;
    private int goalRadius;
    private int searchSize;
    private List<int> path;

    public int StartX { get => startX; }
    public int StartZ { get => startZ; }
    public int GoalX { get => goalX; }
    public int GoalZ { get => goalZ; }

    public GridPath(int startX, int startZ, int goalX, int goalZ, int goalRadius, int searchSize)
    {
        this.startX = startX;
        this.startZ = startZ;
        this.goalX = goalX;
        this.goalZ = goalZ;
        this.goalRadius = goalRadius;
        this.searchSize = searchSize;
    }
    public bool IsGoal(int x, int z)
    {
        return false;
    }
    public bool IsWalkable(int fromX, int fromZ, int toX, int toZ)
    {
        return true;
    }
    public void SetPath(List<int> path)
    {
        this.path = path;
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
    public bool Search(int maxNodes, ref GridPath path)
    {
        Debug.Assert(path.StartX >= 0 && path.StartZ < gridX && path.StartZ >= 0 && path.StartZ < gridZ);
        Debug.Assert(path.GoalX >= 0 && path.GoalX < gridX && path.GoalZ >= 0 && path.GoalZ < gridZ);

        openQueue.Clear();
        foreach (var n in closedQueue)
        {
            n.IsClosed = false;
        }
        closedQueue.Clear();


        bool isFound = false;
        int nodeCount = 0;
        var node = nodes[path.StartX + path.StartZ * gridX];
        node.IsClosed = true;
        closedQueue.Add(node);
        openQueue.Push(node);
        while ((node = openQueue.Pop()) != null && nodeCount++ < maxNodes)
        {
            if (path.IsGoal(node.X, node.Z))
            {
                isFound = true;
                break;
            }
            for (int i = 0; i < neighbors.Length; i += 2)
            {
                var x = node.X + neighbors[i];
                var z = node.Z + neighbors[i + 1];
                if (x < 0 || x >= gridX || z < 0 || z >= gridZ)
                {
                    continue;
                }
                var n = nodes[x + z * gridX];
                if (n.IsBlocked || n.IsClosed)
                {
                    continue;
                }
                if (!path.IsWalkable(node.X, node.Z, x, z))
                {
                    n.IsClosed = true;
                    closedQueue.Add(n);
                    continue;
                }
                n.GCost = node.GCost + CalcDistanceCost(node.X, node.Z, x, z);
                n.HCost = CalcDistanceCost(x, z, );
                n.IsClosed = true;
                closedQueue.Add(n);
                openQueue.Push(n);
            }
        }
        return isFound;
    }
}