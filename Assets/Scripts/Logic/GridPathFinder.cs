using System;
using System.Collections.Generic;
using UnityEngine;

class GridPathNode
{
    private enum Mask { TestBlocked = 1, Blocked = 2, Closed = 4 };

    private readonly int x;
    private readonly int z;
    private int gCost;
    private int hCost;
    private GridPathNode parent;
    private Mask mask;

    public int X { get => x; }
    public int Z { get => z; }
    public int FCost { get => gCost + hCost; }
    public int GCost { get => gCost; set => gCost = value; }
    public int HCost { set => hCost = value; }
    public GridPathNode Parent { get => parent; set => parent = value; }
    public bool HasTestBlocked
    {
        get { return (mask & Mask.TestBlocked) != 0; }
        set { if (value) { mask |= Mask.TestBlocked; } else { mask &= ~Mask.TestBlocked; } }
    }
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

    public GridPathNode(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}

class GridPathPriorityQueue
{
    private GridPathNode[] nodeHeap;
    private int count;
    private int capacity;

    public GridPathPriorityQueue(int capacity = 256)
    {
        this.nodeHeap = new GridPathNode[capacity];
        this.count = 0;
        this.capacity = capacity;
    }
    private void Grow()
    {
        capacity <<= 1;
        var newNodeHeap = new GridPathNode[capacity];
        nodeHeap.CopyTo(newNodeHeap, 0);
        nodeHeap = newNodeHeap;
    }
    private void HeapifyDown(int i, int length)
    {
        int lowest = i;
        int left = i * 2 + 1;
        int right = i * 2 + 2;
        if (left < length && nodeHeap[left].FCost < nodeHeap[lowest].FCost)
        {
            lowest = left;
        }
        if (right < length && nodeHeap[right].FCost < nodeHeap[lowest].FCost)
        {
            lowest = right;
        }
        if (lowest != i)
        {
            var tmp = nodeHeap[i];
            nodeHeap[i] = nodeHeap[lowest];
            nodeHeap[lowest] = tmp;
            HeapifyDown(lowest, length);
        }
    }
    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int j = (i - 1) / 2; //parent
            if (nodeHeap[j].FCost <= nodeHeap[i].FCost)
            {
                break;
            }
            var tmp = nodeHeap[j];
            nodeHeap[j] = nodeHeap[i];
            nodeHeap[i] = tmp;
            i = j;
        }
    }
    public void Push(GridPathNode node)
    {
        if (count == capacity)
        {
            Grow();
        }
        nodeHeap[count] = node;
        HeapifyUp(count);
        count++;
    }
    public GridPathNode Pop()
    {
        if (count == 0)
        {
            return null;
        }
        count--;
        var node = nodeHeap[0];
        nodeHeap[0] = nodeHeap[count];
        HeapifyDown(0, count);
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
    public Vector3 startPos;
    public Vector3 goalPos;
    public float goalRadius;
    public List<Vector3> positions = new List<Vector3>();
}

public class GridPathFinder
{
    private int[] neighbors = { 0, 1, 0, -1, 1, 0, -1, 0, -1, 1, 1, 1, -1, -1, 1, -1 };
    private GridMoveManager moveManager;
    private GridPathNode[] nodes;
    private GridPathPriorityQueue openQueue;
    private List<GridPathNode> closedQueue;

    public GridPathFinder(GridMoveManager moveManager)
    {
        this.moveManager = moveManager;
        nodes = new GridPathNode[moveManager.XSize * moveManager.ZSize];
        for (int z = 0; z < moveManager.ZSize; z++)
        {
            for (int x = 0; x < moveManager.XSize; x++)
            {
                var index = x + z * moveManager.XSize;
                nodes[index] = new GridPathNode(x, z);
            }
        }
        openQueue = new GridPathPriorityQueue();
        closedQueue = new List<GridPathNode>();
    }
    private static int CalcDistanceApproximately(int fromX, int fromZ, int toX, int toZ)
    {
        int x = Mathf.Abs(toX - fromX);
        int z = Mathf.Abs(toZ - fromZ);
        return x > z ? 14 * z + 10 * (x - z) : 14 * x + 10 * (z - x);
    }
    private bool IsNeighborWalkable(GridMoveAgent agent, GridPathNode snode, GridPathNode enode)
    {
        Debug.Assert(Mathf.Abs(snode.X - enode.X) <= 1 && Mathf.Abs(snode.Z - enode.Z) <= 1);
        var offset = agent.UnitSize / 2;
        if (snode.Z == enode.Z) //Horizontal
        {
            int x = enode.X + offset * (enode.X - snode.X);
            if (x < 0 || x >= moveManager.XSize)
            {
                return false;
            }
            for (int i = 0; i < agent.UnitSize; i++)
            {
                if (IsNodeBlocked(nodes[x + (enode.Z - offset + i) * gridX], checkBlockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
        if (snode.X == enode.X) //Vertical
        {
            int z = enode.Z + offset * (enode.Z - snode.Z);
            if (z < 0 || z >= gridZ)
            {
                return false;
            }
            for (int i = 0; i < unitSize; i++)
            {
                if (IsNodeBlocked(nodes[enode.X - offset + i + z * gridX], checkBlockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
        { //Cross
            int x = enode.X + offset * (enode.X - snode.X);
            int z = enode.Z + offset * (enode.Z - snode.Z);
            if (x < 0 || x >= gridX || z < 0 || z >= gridZ)
            {
                return false;
            }
            for (int i = 0; i <= unitSize; i++)
            {
                if (IsNodeBlocked(nodes[x - i * (enode.X - snode.X) + z * gridX], checkBlockedFunc))
                {
                    return false;
                }
            }
            for (int i = 1; i <= unitSize; i++)
            {
                if (IsNodeBlocked(nodes[x + (z - i * (enode.Z - snode.Z)) * gridX], checkBlockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
    }
    private bool IsCrossWalkable(int unitSize, GridPathNode snode, GridPathNode enode, Func<int, int, bool> checkBlockedFunc)
    {
        int dx = enode.X - snode.X, dz = enode.Z - snode.Z;
        int nx = Mathf.Abs(dx), nz = Mathf.Abs(dz);
        int signX = dx > 0 ? 1 : -1, signZ = dz > 0 ? 1 : -1;

        int x = snode.X, z = snode.Z;
        for (int ix = 0, iz = 0; ix < nx || iz < nz;)
        {
            var t1 = (2 * ix + 1) * nz;
            var t2 = (2 * iz + 1) * nx;
            if (t1 < t2) //Horizontal
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * gridX], nodes[x + signX + z * gridX], checkBlockedFunc))
                {
                    return false;
                }
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * gridX], nodes[x + (z + signZ) * gridX], checkBlockedFunc))
                {
                    return false;
                }
                z += signZ;
                iz++;
            }
            else //Cross
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * gridX], nodes[x + signX + (z + signZ) * gridX], checkBlockedFunc))
                {
                    return false;
                }
                x += signX;
                z += signZ;
                ix++;
                iz++;
            }
        }
        return true;
    }
    public bool Search(GridMoveAgent agent, float searchSize, float searchExtraSize, int searchMaxNodes, ref GridPath path)
    {
        Debug.Assert(searchSize > 0 && searchMaxNodes > 0);
        openQueue.Clear();
        foreach (var n in closedQueue)
        {
            n.HasTestBlocked = false;
            n.IsClosed = false;
        }
        closedQueue.Clear();

        moveManager.GetTileXZ(path.startPos, out var startX, out var startZ);
        moveManager.GetTileXZ(path.goalPos, out var goalX, out var goalZ);
        int goalDistance = (int)(path.goalRadius / moveManager.TileSize) * 10;
        int searchDistance = (int)(searchSize * CalcDistanceApproximately(startX, startZ, goalX, goalZ)) + (int)(searchExtraSize / moveManager.TileSize) * 10;

        var startIndex = startX + startZ * moveManager.XSize;
        var startNode = nodes[startIndex];
        startNode.IsClosed = true;
        closedQueue.Add(startNode);
        openQueue.Push(startNode);
        for (int i = 0; i < searchMaxNodes; i++)
        {
            var node = openQueue.Pop();
            if (node == null)
            {
                return false;
            }
            if (CalcDistanceApproximately(node.X, node.Z, goalX, goalZ) <= goalDistance)
            {
                var nodes = new List<GridPathNode>();
                while (node != startNode)
                {
                    nodes.Add(node);
                    node = node.Parent;
                }
                nodes.Add(startNode);
                path.positions.Clear();
                for (int t = nodes.Count - 1; t >= 0; t--)
                {
                    var n = nodes[t];
                    path.positions.Add(moveManager.GetTilePos(n.X, n.Z));
                }
                return true;
            }
            for (int j = 0; j < neighbors.Length; j += 2)
            {
                var x = node.X + neighbors[j];
                var z = node.Z + neighbors[j + 1];
                if (x < 0 || x >= moveManager.XSize || z < 0 || z >= moveManager.ZSize)
                {
                    continue;
                }
                var n = nodes[x + z * moveManager.XSize];
                if (n.IsClosed)
                {
                    continue;
                }
                n.IsClosed = true;
                closedQueue.Add(n);
                if (CalcDistanceApproximately(startX, startZ, x, z) > searchDistance)
                {
                    continue;
                }
                if (!IsNeighborWalkable(agent, node, n))
                {
                    continue;
                }
                n.GCost = node.GCost + CalcDistanceApproximately(node.X, node.Z, x, z);
                n.HCost = CalcDistanceApproximately(x, z, goalX, goalZ);
                n.Parent = node;
                openQueue.Push(n);
            }
        }
        return false;
    }
}