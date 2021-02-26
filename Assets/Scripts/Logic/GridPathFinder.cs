using System;
using System.Collections.Generic;
using UnityEngine;

public class GridPathNode
{
    private enum Mask { Closed = 1, Blocked = 2 };

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
    public int HCost { get => hCost; set => hCost = value; }
    public GridPathNode Parent { get => parent; set => parent = value; }
    public bool IsClosed
    {
        get { return (mask & Mask.Closed) != 0; }
        set { if (value) { mask |= Mask.Closed; } else { mask &= ~Mask.Closed; } }
    }
    public bool IsBlocked
    {
        get { return (mask & Mask.Blocked) != 0; }
        set { if (value) { mask |= Mask.Blocked; } else { mask &= ~Mask.Blocked; } }
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

public class GridPathFinder
{
    private int[] neighbors = { 0, 1, 0, -1, 1, 0, -1, 0, -1, 1, 1, 1, -1, -1, 1, -1 };
    private int xsize;
    private int zsize;
    private GridPathNode[] nodes;
    private GridPathPriorityQueue openQueue;
    private List<GridPathNode> closedQueue;

    public GridPathFinder(int xsize, int zsize)
    {
        this.xsize = xsize;
        this.zsize = zsize;
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
    }
    public void SetNodeBlocked(int x, int z, bool isBlocked)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        nodes[x + z * xsize].IsBlocked = isBlocked;
    }
    private bool IsNodeCenterBlocked(GridPathNode node, Func<int, int, bool> blockedFunc)
    {
        return node.IsBlocked || blockedFunc(node.X, node.Z);
    }
    private bool IsNodeBlocked(int unitSize, int x, int z, Func<int, int, bool> blockedFunc)
    {
        int offset = (unitSize >> 1);
        int xmin = x - offset;
        int xmax = x + offset;
        int zmin = z - offset;
        int zmax = z + offset;
        if (xmin < 0 || xmax >= xsize || zmin < 0 || zmax >= zsize)
        {
            return true;
        }
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                if (IsNodeCenterBlocked(nodes[tx + tz * xsize], blockedFunc))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool IsNodeBlocked(int unitSize, GridPathNode node, Func<int, int, bool> blockedFunc)
    {
        return IsNodeBlocked(unitSize, node.X, node.Z, blockedFunc);
    }
    private bool IsNeighborWalkable(int unitSize, GridPathNode snode, GridPathNode enode, Func<int, int, bool> blockedFunc)
    {
        Debug.Assert(Mathf.Abs(snode.X - enode.X) <= 1 && Mathf.Abs(snode.Z - enode.Z) <= 1);

        var offset = (unitSize >> 1);
        if (snode.Z == enode.Z) //Horizontal
        {
            int x = enode.X + offset * (enode.X - snode.X);
            if (x < 0 || x >= xsize)
            {
                return false;
            }
            for (int i = 0; i < unitSize; i++)
            {
                if (IsNodeCenterBlocked(nodes[x + (enode.Z - offset + i) * xsize], blockedFunc))
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
            for (int i = 0; i < unitSize; i++)
            {
                if (IsNodeCenterBlocked(nodes[enode.X - offset + i + z * xsize], blockedFunc))
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
            for (int i = 0; i <= unitSize; i++)
            {
                if (IsNodeCenterBlocked(nodes[x - i * (enode.X - snode.X) + z * xsize], blockedFunc))
                {
                    return false;
                }
            }
            for (int i = 1; i <= unitSize; i++)
            {
                if (IsNodeCenterBlocked(nodes[x + (z - i * (enode.Z - snode.Z)) * xsize], blockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
    }
    public bool IsCrossWalkable(int unitSize, GridPathNode snode, GridPathNode enode, Func<int, int, bool> blockedFunc)
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
                if (!IsNeighborWalkable(unitSize, nodes[x + z * xsize], nodes[x + signX + z * xsize], blockedFunc))
                {
                    return false;
                }
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * xsize], nodes[x + (z + signZ) * xsize], blockedFunc))
                {
                    return false;
                }
                z += signZ;
                iz++;
            }
            else //Cross
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * xsize], nodes[x + signX + (z + signZ) * xsize], blockedFunc))
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
    public GridPathNode GetNode(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return nodes[x + z * xsize];
    }
    public GridPathNode FindNearestNode(int unitSize, int x, int z, int searchRadius, Func<int, int, bool> blockedFunc)
    {
        Debug.Assert(unitSize > 0 && blockedFunc != null);

        x = Mathf.Clamp(x, 0, xsize - 1);
        z = Mathf.Clamp(z, 0, zsize - 1);
        if (!IsNodeBlocked(unitSize, x, z, blockedFunc))
        {
            return nodes[x + z * xsize];
        }

        foreach (var n in closedQueue)
        {
            n.IsClosed = false;
        }
        closedQueue.Clear();
        openQueue.Clear();

        int searchDistance = searchRadius * 10;
        var startNode = nodes[x + z * xsize];
        startNode.GCost = 0;
        startNode.HCost = 0;
        startNode.IsClosed = true;
        closedQueue.Add(startNode);

        var node = startNode;
        while (node != null)
        {
            if (!IsNodeBlocked(unitSize, node, blockedFunc))
            {
                return node;
            }
            for (int i = 0; i < neighbors.Length; i += 2)
            {
                var neighborX = node.X + neighbors[i];
                var neighborZ = node.Z + neighbors[i + 1];
                var n = nodes[neighborX + neighborZ * xsize];
                if (n.IsClosed)
                {
                    continue;
                }
                n.IsClosed = true;
                closedQueue.Add(n);

                n.GCost = CalcDistanceApproximately(startNode, n);
                n.HCost = 0;
                if (searchDistance >= 0 && n.GCost > searchDistance)
                {
                    continue;
                }
                openQueue.Push(n);
            }
            node = openQueue.Pop();
        }
        return null;
    }
    public bool FindStraightPath(int unitSize, GridPathNode startNode, GridPathNode goalNode, int goalRadius, Func<int, int, bool> blockedFunc, out List<GridPathNode> path)
    {
        Debug.Assert(unitSize > 0 && startNode != null && goalNode != null && goalRadius >= 0 && blockedFunc != null);

        path = new List<GridPathNode>();
        path.Add(startNode);
        int goalDistance = goalRadius * 10;
        int dx = goalNode.X - startNode.X;
        int dz = goalNode.Z - startNode.Z;
        int nx = Mathf.Abs(dx);
        int nz = Mathf.Abs(dz);
        int signX = dx > 0 ? 1 : -1;
        int signZ = dz > 0 ? 1 : -1;
        int x = startNode.X;
        int z = startNode.Z;
        for (int ix = 0, iz = 0; ix < nx || iz < nz;)
        {
            var t1 = (2 * ix + 1) * nz;
            var t2 = (2 * iz + 1) * nx;
            if (t1 < t2) //Horizontal
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * xsize], nodes[x + signX + z * xsize], blockedFunc))
                {
                    return false;
                }
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * xsize], nodes[x + (z + signZ) * xsize], blockedFunc))
                {
                    return false;
                }
                z += signZ;
                iz++;
            }
            else //Cross
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * xsize], nodes[x + signX + (z + signZ) * xsize], blockedFunc))
                {
                    return false;
                }
                x += signX;
                z += signZ;
                ix++;
                iz++;
            }
            path.Add(nodes[x + z * xsize]);
        }
        return true;
    }
    public bool FindPath(int unitSize, GridPathNode startNode, GridPathNode goalNode, int goalRadius, int searchRadius, int searchMaxNodes, Func<int, int, bool> blockedFunc, out List<GridPathNode> path)
    {
        Debug.Assert(unitSize > 0 && startNode != null && goalNode != null && goalRadius >= 0 && blockedFunc != null);

        foreach (var n in closedQueue)
        {
            n.IsClosed = false;
        }
        closedQueue.Clear();
        openQueue.Clear();

        int goalDistance = goalRadius * 10;
        int searchDistance = searchRadius * 10;
        startNode.GCost = 0;
        startNode.HCost = CalcDistanceApproximately(startNode, goalNode);
        startNode.IsClosed = true;
        closedQueue.Add(startNode);

        var nearestNode = startNode;
        int searchNodeCount = 0;
        bool isFound = false;
        var node = startNode;
        while (node != null && (searchMaxNodes < 0 || searchNodeCount++ < searchMaxNodes))
        {
            if (node.HCost <= goalDistance)
            {
                nearestNode = node;
                isFound = true;
                break;
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
                if (searchDistance >= 0 && CalcDistanceApproximately(startNode, n) > searchDistance)
                {
                    continue;
                }
                if (!IsNeighborWalkable(unitSize, node, n, blockedFunc))
                {
                    continue;
                }
                n.GCost = node.GCost + CalcDistanceApproximately(node, n);
                n.HCost = CalcDistanceApproximately(n, goalNode);
                n.Parent = node;
                openQueue.Push(n);
                if (node.HCost < nearestNode.HCost)
                {
                    nearestNode = node;
                }
            }
            node = openQueue.Pop();
        }
        path = new List<GridPathNode>();
        while (nearestNode != startNode)
        {
            path.Add(nearestNode);
            nearestNode = nearestNode.Parent;
        }
        path.Add(startNode);
        path.Reverse();
        return isFound;
    }
    private static int CalcDistanceApproximately(int fromX, int fromZ, int toX, int toZ)
    {
        int x = Mathf.Abs(toX - fromX);
        int z = Mathf.Abs(toZ - fromZ);
        return x > z ? 14 * z + 10 * (x - z) : 14 * x + 10 * (z - x);
    }
    private static int CalcDistanceApproximately(GridPathNode fromNode, GridPathNode toNode)
    {
        return CalcDistanceApproximately(fromNode.X, fromNode.Z, toNode.X, toNode.Z);
    }
}