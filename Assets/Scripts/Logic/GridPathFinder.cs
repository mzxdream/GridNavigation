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
    public List<Vector3> positions;
}

public class GridPathFinder
{
    private int[] neighbors = { 0, 1, 0, -1, 1, 0, -1, 0, -1, 1, 1, 1, -1, -1, 1, -1 };
    private GridMoveManager moveManager;
    private int xsize;
    private int zsize;
    private float tileSize;
    private GridPathNode[] nodes;
    private GridPathPriorityQueue openQueue;
    private List<GridPathNode> closedQueue;
    private List<GridPathNode> testBlockQueue;

    public GridPathFinder(GridMoveManager moveManager)
    {
        this.moveManager = moveManager;
        xsize = moveManager.XSize;
        zsize = moveManager.ZSize;
        tileSize = moveManager.TileSize;
        nodes = new GridPathNode[xsize * zsize];
        for (int z = 0; z < zsize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                nodes[x + z * xsize] = new GridPathNode(x, z);
            }
        }
        openQueue = new GridPathPriorityQueue();
        closedQueue = new List<GridPathNode>();
        testBlockQueue = new List<GridPathNode>();
    }
    private static int CalcDistanceApproximately(int fromX, int fromZ, int toX, int toZ)
    {
        int x = Mathf.Abs(toX - fromX);
        int z = Mathf.Abs(toZ - fromZ);
        return x > z ? 14 * z + 10 * (x - z) : 14 * x + 10 * (z - x);
    }
    private bool IsNodeCenterBlocked(GridMoveAgent agent, GridPathNode node)
    {
        if (!node.HasTestBlocked)
        {
            node.HasTestBlocked = true;
            node.IsBlocked = moveManager.IsTileBlocked(agent, node.X, node.Z, true);
            testBlockQueue.Add(node);
        }
        return node.IsBlocked;
    }
    private bool IsNodeCenterBlocked(GridMoveAgent agent, int x, int z)
    {
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            return true;
        }
        return IsNodeCenterBlocked(agent, nodes[x + z * xsize]);
    }
    private bool IsNodeBlocked(GridMoveAgent agent, int x, int z)
    {
        int offset = agent.UnitSize / 2;
        int xmin = x - offset, xmax = x + offset;
        int zmin = z - offset, zmax = z + offset;
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                if (IsNodeCenterBlocked(agent, tx, tz))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool IsNeighborWalkable(GridMoveAgent agent, GridPathNode snode, GridPathNode enode)
    {
        Debug.Assert(Mathf.Abs(snode.X - enode.X) <= 1 && Mathf.Abs(snode.Z - enode.Z) <= 1);

        var offset = agent.UnitSize / 2;
        if (snode.Z == enode.Z) //Horizontal
        {
            int x = enode.X + offset * (enode.X - snode.X);
            if (x < 0 || x >= xsize)
            {
                return false;
            }
            for (int i = 0; i < agent.UnitSize; i++)
            {
                if (IsNodeCenterBlocked(agent, nodes[x + (enode.Z - offset + i) * xsize]))
                {
                    return false;
                }
            }
            return true;
        }
        if (snode.X == enode.X) //Vertical
        {
            int z = enode.Z + offset * (enode.Z - snode.Z);
            if (z < 0 || z >= zsize)
            {
                return false;
            }
            for (int i = 0; i < agent.UnitSize; i++)
            {
                if (IsNodeCenterBlocked(agent, nodes[enode.X - offset + i + z * xsize]))
                {
                    return false;
                }
            }
            return true;
        }
        { //Cross
            int x = enode.X + offset * (enode.X - snode.X);
            int z = enode.Z + offset * (enode.Z - snode.Z);
            if (x < 0 || x >= xsize || z < 0 || z >= zsize)
            {
                return false;
            }
            for (int i = 0; i <= agent.UnitSize; i++)
            {
                if (IsNodeCenterBlocked(agent, nodes[x - i * (enode.X - snode.X) + z * xsize]))
                {
                    return false;
                }
            }
            for (int i = 1; i <= agent.UnitSize; i++)
            {
                if (IsNodeCenterBlocked(agent, nodes[x + (z - i * (enode.Z - snode.Z)) * xsize]))
                {
                    return false;
                }
            }
            return true;
        }
    }
    private bool IsCrossWalkable(GridMoveAgent agent, GridPathNode snode, GridPathNode enode)
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
                if (!IsNeighborWalkable(agent, nodes[x + z * xsize], nodes[x + signX + z * xsize]))
                {
                    return false;
                }
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                if (!IsNeighborWalkable(agent, nodes[x + z * xsize], nodes[x + (z + signZ) * xsize]))
                {
                    return false;
                }
                z += signZ;
                iz++;
            }
            else //Cross
            {
                if (!IsNeighborWalkable(agent, nodes[x + z * xsize], nodes[x + signX + (z + signZ) * xsize]))
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
    private GridPathNode FindNearestNode(GridMoveAgent agent, Vector3 pos, float searchRadius)
    {
        moveManager.GetTileXZ(pos, out int x, out int z);
        if (!IsNodeBlocked(agent, x, z))
        {
            return nodes[x + z * xsize];
        }
        var searchSize = (int)(searchRadius / tileSize);
        for (int i = 0; i < searchSize; i++)
        {
            if (!IsNodeBlocked(agent, x + i, z))
            {
                return nodes[x + i + z * xsize];
            }
            if (!IsNodeBlocked(agent, x - i, z))
            {
                return nodes[x - i + z * xsize];
            }
            if (!IsNodeBlocked(agent, x, z + i))
            {
                return nodes[x + (z + i) * xsize];
            }
            if (!IsNodeBlocked(agent, x, z - i))
            {
                return nodes[x + (z - i) * xsize];
            }
            if (!IsNodeBlocked(agent, x + i, z + i))
            {
                return nodes[x + i + (z + i) * xsize];
            }
            if (!IsNodeBlocked(agent, x + i, z - i))
            {
                return nodes[x + i + (z - i) * xsize];
            }
            if (!IsNodeBlocked(agent, x - i, z + i))
            {
                return nodes[x - i + (z + i) * xsize];
            }
            if (!IsNodeBlocked(agent, x - i, z - i))
            {
                return nodes[x - i + (z - i) * xsize];
            }
        }
        return null;
    }
    public bool Search(GridMoveAgent agent, float searchRadius, int searchMaxNodes, ref GridPath path)
    {
        Debug.Assert(searchRadius > 0 && searchMaxNodes > 0);

        openQueue.Clear();
        foreach (var n in closedQueue)
        {
            n.IsClosed = false;
        }
        closedQueue.Clear();
        foreach (var n in testBlockQueue)
        {
            n.HasTestBlocked = false;
        }

        moveManager.GetTileXZUnclamped(path.startPos, out var startX, out var startZ);
        moveManager.GetTileXZUnclamped(path.goalPos, out var goalX, out var goalZ);

        int goalDistance = (int)(path.goalRadius / tileSize) * 10;
        int searchDistance = (int)(searchRadius / tileSize) * 10;

        var startIndex = startX + startZ * xsize;
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
                path.positions = new List<Vector3>();
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
                if (x < 0 || x >= xsize || z < 0 || z >= zsize)
                {
                    continue;
                }
                var n = nodes[x + z * xsize];
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