using System;
using System.Collections.Generic;
using UnityEngine;

enum GridNavNodeFlags { Open = 0x01, Closed = 0x02 };

class GridNavQueryNode
{
    public int x;
    public int z;
    public int squareIndex;
    public float gCost;
    public float fCost;
    public GridNavQueryNode parent;
    public int flags;
}

class GridNavQueryPriorityQueue
{
    private GridNavQueryNode[] heap;
    private int count;
    private int capacity;

    public GridNavQueryPriorityQueue(int capacity = 256)
    {
        this.heap = new GridNavQueryNode[capacity];
        this.count = 0;
        this.capacity = capacity;
    }
    public void Push(GridNavQueryNode node)
    {
        if (count == capacity)
        {
            capacity <<= 1;
            var newHeap = new GridNavQueryNode[capacity];
            heap.CopyTo(newHeap, 0);
            heap = newHeap;
        }
        heap[count] = node;
        HeapifyUp(count);
        count++;
    }
    public void Modify(GridNavQueryNode node)
    {
        for (int i = 0; i < count; ++i)
        {
            if (heap[i] == node)
            {
                HeapifyUp(i);
                return;
            }
        }
    }
    public GridNavQueryNode Pop()
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
            if (heap[j].fCost <= heap[i].fCost)
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
        if (left < length && heap[left].fCost < heap[lowest].fCost)
        {
            lowest = left;
        }
        if (right < length && heap[right].fCost < heap[lowest].fCost)
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

public class GridNavQueryFilter
{
    public int unitSize;
    public float searchRadiusScale;
    public Func<int, bool> blockedFunc;
}

public class GridNavQuery
{
    private static readonly int[] neighbours = { 1, 0, -1, 0, 0, 1, 0, -1, -1, -1, 1, 1, -1, 1, 1, -1 };
    private GridNavMesh navMesh;
    private int xsize;
    private int zsize;
    private GridNavQueryNode[] nodes;
    private GridNavQueryPriorityQueue openQueue;
    private List<GridNavQueryNode> dirtyQueue;

    public bool Init(GridNavMesh navMesh)
    {
        this.navMesh = navMesh;
        this.xsize = navMesh.XSize;
        this.zsize = navMesh.ZSize;
        this.nodes = new GridNavQueryNode[this.xsize * this.zsize];
        for (int z = 0; z < this.zsize; z++)
        {
            for (int x = 0; x < this.xsize; x++)
            {
                this.nodes[x + z * xsize] = new GridNavQueryNode
                {
                    x = x,
                    z = z,
                    squareIndex = navMesh.GetSquareIndex(x, z),
                };
            }
        }
        this.openQueue = new GridNavQueryPriorityQueue();
        this.dirtyQueue = new List<GridNavQueryNode>();
        return true;
    }
    public void Clear()
    {
    }
    public bool FindPath(int startIndex, int endIndex, GridNavQueryFilter filter, out List<int> path)
    {
        Debug.Assert(filter != null && filter.unitSize > 0);
        path = new List<int>();
        if (!navMesh.GetSquareXZ(startIndex, out int sx, out int sz) || !navMesh.GetSquareXZ(endIndex, out int ex, out int ez))
        {
            return false;
        }
        var snode = nodes[sx + sz * xsize];
        var enode = nodes[ex + ez * xsize];
        if (IsNodeBlocked(filter.unitSize, sx, sz, filter.blockedFunc) || IsNodeBlocked(filter.unitSize, ex, ez, filter.blockedFunc))
        {
            return false;
        }
        if (snode == enode)
        {
            path.Add(snode.squareIndex);
            return true;
        }

        foreach (var n in dirtyQueue)
        {
            n.flags &= ~(int)(GridNavNodeFlags.Open | GridNavNodeFlags.Closed);
        }
        dirtyQueue.Clear();
        openQueue.Clear();

        int mx = (sx + ex) / 2;
        int mz = (sz + ez) / 2;
        float searchRadius = GridMathUtils.GridDistanceApproximately(sx, sz, mx, mz) * filter.searchRadiusScale;

        snode.gCost = 0;
        snode.fCost = GridMathUtils.GridDistanceApproximately(sx, sz, ex, ez);
        snode.parent = null;
        snode.flags |= (int)GridNavNodeFlags.Open;
        dirtyQueue.Add(snode);
        openQueue.Push(snode);

        var lastBestNode = snode;
        var lastBestNodeCost = snode.fCost;
        GridNavQueryNode bestNode = null;
        while ((bestNode = openQueue.Pop()) != null)
        {
            bestNode.flags &= ~(int)GridNavNodeFlags.Open;
            bestNode.flags |= (int)GridNavNodeFlags.Closed;
            if (bestNode == enode)
            {
                lastBestNode = bestNode;
                break;
            }
            for (int i = 0; i < neighbours.Length - 1; i += 2)
            {
                var nx = bestNode.x + neighbours[i];
                var nz = bestNode.z + neighbours[i + 1];
                if (nx < 0 || nx >= xsize || nz < 0 || nz >= zsize)
                {
                    continue;
                }
                if (GridMathUtils.GridDistanceApproximately(nx, nz, mx, mz) > searchRadius)
                {
                    continue;
                }
                var neighbourNode = nodes[nx + nz * xsize];
                var gCost = bestNode.gCost + GridMathUtils.GridDistanceApproximately(bestNode.x, bestNode.z, neighbourNode.x, neighbourNode.z);
                if ((neighbourNode.flags & (int)(GridNavNodeFlags.Open | GridNavNodeFlags.Closed)) != 0)
                {
                    if (gCost >= neighbourNode.gCost)
                    {
                        continue;
                    }
                    neighbourNode.flags &= ~(int)GridNavNodeFlags.Closed;
                }
                else
                {
                    dirtyQueue.Add(neighbourNode);
                }
                var hCost = GridMathUtils.GridDistanceApproximately(nx, nz, ex, ez);
                neighbourNode.gCost = gCost;
                neighbourNode.fCost = gCost + hCost;
                neighbourNode.parent = bestNode;
                if ((neighbourNode.flags & (int)GridNavNodeFlags.Open) != 0)
                {
                    openQueue.Modify(neighbourNode);
                }
                else
                {
                    neighbourNode.flags |= (int)GridNavNodeFlags.Open;
                    openQueue.Push(neighbourNode);
                }
                if (hCost < lastBestNodeCost)
                {
                    lastBestNodeCost = hCost;
                    lastBestNode = neighbourNode;
                }
            }
        }

        var curNode = lastBestNode;
        do
        {
            path.Add(curNode.squareIndex);
            curNode = curNode.parent;
        } while (curNode != null);
        path.Reverse();

        return true;
    }
    public GridPathStatus FindRawPath(int unitSize, GridPathNode snode, GridPathNode enode, Func<int, int, bool> blockedFunc, out List<GridPathNode> path)
    {
        Debug.Assert(unitSize > 0 && snode != null && enode != null);

        path = new List<GridPathNode>();
        if (IsNodeBlocked(unitSize, snode.X, snode.Z, blockedFunc) || IsNodeBlocked(unitSize, enode.X, enode.Z, blockedFunc))
        {
            return GridPathStatus.Failure;
        }
        path.Add(snode);
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
                    return GridPathStatus.Failure;
                }
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * xsize], nodes[x + (z + signZ) * xsize], blockedFunc))
                {
                    return GridPathStatus.Failure;
                }
                z += signZ;
                iz++;
            }
            else //Cross
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * xsize], nodes[x + signX + (z + signZ) * xsize], blockedFunc))
                {
                    return GridPathStatus.Failure;
                }
                x += signX;
                z += signZ;
                ix++;
                iz++;
            }
            path.Add(nodes[x + z * xsize]);
        }
        return GridPathStatus.Success;
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
    private bool IsNodeBlocked(int x, int z, Func<int, bool> blockedFunc)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        var node = nodes[x + z * xsize];
        return navMesh.IsSquareBlocked(node.squareIndex) || (blockedFunc != null && blockedFunc(node.squareIndex));
    }
    private bool IsNodeBlocked(int unitSize, int x, int z, Func<int, bool> blockedFunc)
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
                if (IsNodeBlocked(tx, tz, blockedFunc))
                {
                    return true;
                }
            }
        }
        return false;
    }
}