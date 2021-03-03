using System;
using System.Collections.Generic;
using UnityEngine;

public class GridPathNode
{
    private enum Mask { Closed = 1, Blocked = 2 };

    private readonly int x;
    private readonly int z;
    private float gCost;
    private float hCost;
    private GridPathNode parent;
    private Mask mask;

    public int X { get => x; }
    public int Z { get => z; }
    public float FCost { get => gCost + hCost; }
    public float GCost { get => gCost; set => gCost = value; }
    public float HCost { get => hCost; set => hCost = value; }
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
    private GridPathNode[] heap;
    private int count;
    private int capacity;

    public GridPathPriorityQueue(int capacity = 256)
    {
        this.heap = new GridPathNode[capacity];
        this.count = 0;
        this.capacity = capacity;
    }
    public void Push(GridPathNode node)
    {
        if (count == capacity)
        {
            capacity <<= 1;
            var newHeap = new GridPathNode[capacity];
            heap.CopyTo(newHeap, 0);
            heap = newHeap;
        }
        heap[count] = node;
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
        var node = heap[0];
        heap[0] = heap[count];
        HeapifyDown(0, count);
        return node;
    }
    public void Clear()
    {
        for (int i = 0; i < count; i++)
        {
            heap[i] = null;
        }
        count = 0;
    }
    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int j = (i - 1) / 2; //parent
            if (heap[j].FCost <= heap[i].FCost)
            {
                break;
            }
            var tmp = heap[j];
            heap[j] = heap[i];
            heap[i] = tmp;
            i = j;
        }
    }
    private void HeapifyDown(int i, int length)
    {
        int lowest = i;
        int left = i * 2 + 1;
        int right = i * 2 + 2;
        if (left < length && heap[left].FCost < heap[lowest].FCost)
        {
            lowest = left;
        }
        if (right < length && heap[right].FCost < heap[lowest].FCost)
        {
            lowest = right;
        }
        if (lowest != i)
        {
            var tmp = heap[i];
            heap[i] = heap[lowest];
            heap[lowest] = tmp;
            HeapifyDown(lowest, length);
        }
    }
}

public class GridPathFinder
{
    private static readonly int[] neighbors = { 0, 1, 0, -1, 1, 0, -1, 0, -1, 1, 1, 1, -1, -1, 1, -1 };
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
    public GridPathNode GetNode(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return nodes[x + z * xsize];
    }
    public void SetNodeBlocked(int x, int z, bool isBlocked)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        nodes[x + z * xsize].IsBlocked = isBlocked;
    }
    public GridPathNode FindNearestNode(int unitSize, int x, int z, int extent, Func<int, int, bool> blockedFunc)
    {
        Debug.Assert(unitSize > 0 && extent >= 0);

        x = Mathf.Clamp(x, 0, xsize - 1);
        z = Mathf.Clamp(z, 0, zsize - 1);
        if (!IsNodeBlocked(unitSize, x, z, blockedFunc))
        {
            return nodes[x + z * xsize];
        }
        for (int k = 1; k <= extent; k++)
        {
            int xmin = x - k;
            int xmax = x + k;
            int zmin = z - k;
            int zmax = z + k;
            if (!IsNodeBlocked(unitSize, x, zmax, blockedFunc)) //up
            {
                return nodes[x + zmax * xsize];
            }
            if (!IsNodeBlocked(unitSize, x, zmin, blockedFunc)) //down
            {
                return nodes[x + zmin * xsize];
            }
            if (!IsNodeBlocked(unitSize, xmin, z, blockedFunc)) //left
            {
                return nodes[xmin + z * xsize];
            }
            if (!IsNodeBlocked(unitSize, xmax, z, blockedFunc)) //right
            {
                return nodes[xmax + z * xsize];
            }
            for (int t = 1; t < k; t++)
            {
                if (!IsNodeBlocked(unitSize, x - t, zmax, blockedFunc)) //up left
                {
                    return nodes[x - t + zmax * xsize];
                }
                if (!IsNodeBlocked(unitSize, x + t, zmax, blockedFunc)) //up right
                {
                    return nodes[x + t + zmax * xsize];
                }
                if (!IsNodeBlocked(unitSize, x - t, zmin, blockedFunc)) //down left
                {
                    return nodes[x - t + zmin * xsize];
                }
                if (!IsNodeBlocked(unitSize, x + t, zmin, blockedFunc)) //down right
                {
                    return nodes[x + t + zmin * xsize];
                }
                if (!IsNodeBlocked(unitSize, xmin, z - t, blockedFunc)) //left up
                {
                    return nodes[xmin + (z - t) * xsize];
                }
                if (!IsNodeBlocked(unitSize, xmin, z + t, blockedFunc)) //left down
                {
                    return nodes[xmin + (z + t) * xsize];
                }
                if (!IsNodeBlocked(unitSize, xmax, z - t, blockedFunc)) //right up
                {
                    return nodes[xmax + (z - t) * xsize];
                }
                if (!IsNodeBlocked(unitSize, xmax, z + t, blockedFunc)) //right down
                {
                    return nodes[xmax + (z + t) * xsize];
                }
            }
            if (!IsNodeBlocked(unitSize, xmin, zmax, blockedFunc)) //left up
            {
                return nodes[xmin + zmax * xsize];
            }
            if (!IsNodeBlocked(unitSize, xmax, zmax, blockedFunc)) //right up
            {
                return nodes[xmax + zmax * xsize];
            }
            if (!IsNodeBlocked(unitSize, xmin, zmin, blockedFunc)) //left down
            {
                return nodes[xmin + zmin * xsize];
            }
            if (!IsNodeBlocked(unitSize, xmax, zmin, blockedFunc)) //right down
            {
                return nodes[xmax + zmin * xsize];
            }
        }
        return null;
    }
    public bool IsCrossWalkable(int unitSize, GridPathNode snode, GridPathNode enode, Func<int, int, bool> blockedFunc)
    {
        Debug.Assert(unitSize > 0 && snode != null && enode != null);

        int dx = enode.X - snode.X;
        int dz = enode.Z - snode.Z;
        int nx = Mathf.Abs(dx);
        int nz = Mathf.Abs(dz);
        int signX = dx > 0 ? 1 : -1;
        int signZ = dz > 0 ? 1 : -1;

        int x = snode.X;
        int z = snode.Z;
        int ix = 0;
        int iz = 0;
        while (ix < nx || iz < nz)
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
    public bool FindStraightPath(int unitSize, GridPathNode startNode, GridPathNode goalNode, float goalRadius, Func<int, int, bool> blockedFunc, out List<GridPathNode> path)
    {
        Debug.Assert(unitSize > 0 && startNode != null && goalNode != null && goalRadius >= 0 && blockedFunc != null);

        path = new List<GridPathNode>();

        int dx = goalNode.X - startNode.X;
        int dz = goalNode.Z - startNode.Z;
        int nx = Mathf.Abs(dx);
        int nz = Mathf.Abs(dz);
        int signX = dx > 0 ? 1 : -1;
        int signZ = dz > 0 ? 1 : -1;

        int x = startNode.X;
        int z = startNode.Z;
        int ix = 0;
        int iz = 0;
        while (ix < nx || iz < nz)
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
    private bool IsNodeCenterBlocked(GridPathNode node, Func<int, int, bool> blockedFunc)
    {
        Debug.Assert(node != null);
        return node.IsBlocked || (blockedFunc != null && blockedFunc(node.X, node.Z));
    }
    private bool IsNodeBlocked(int unitSize, int x, int z, Func<int, int, bool> blockedFunc)
    {
        Debug.Assert(unitSize > 0);

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
    private bool IsNeighborWalkable(int unitSize, GridPathNode snode, GridPathNode enode, Func<int, int, bool> blockedFunc)
    {
        Debug.Assert(unitSize > 0 && snode != null && enode != null && (snode.X != enode.X || snode.Z != enode.Z));
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
    private static float HeuristicDistance(int sx, int sz, int ex, int ez)
    {
        int dx = Mathf.Abs(ex - sx);
        int dz = Mathf.Abs(ez - sz);
        return (dx + dz) * 1.0f + Mathf.Min(dx, dz) * (1.4142f - 2.0f);
    }
}