using System;
using System.Collections.Generic;
using UnityEngine;

enum GridNavNodeFlags { Open = 0x01, Closed = 0x02 };

class GridNavQueryNode
{
    public int squareIndex;
    public float gCost;
    public float fCost;
    public GridNavQueryNode parent;
    public int flags;
}

class GridNavQueryPriorityQueue
{
    private GridPathNode[] heap;
    private int count;
    private int capacity;

    public GridNavQueryPriorityQueue(int capacity = 256)
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
    public void Modify(GridPathNode node)
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

public class GridNavQueryFilter
{
    public int unitSize;
    public float searchRadiusScale;
    public float searchRadiusExtra;
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
            n.flags = 0;
        }
        dirtyQueue.Clear();
        openQueue.Clear();

        int mx = (sx + ex) / 2;
        int mz = (sz + ez) / 2;
        float searchRadius = GridMathUtils.GridDistanceApproximately(snode.X, snode.Z, middleX, middleZ) * searchRadiusScale + searchRadiusExtra;

        snode.GCost = 0;
        snode.HCost = GridMathUtils.GridDistanceApproximately(snode.X, snode.Z, enode.X, enode.Z);
        snode.Parent = null;
        snode.IsOpen = true;
        dirtyQueue.Add(snode);
        openQueue.Push(snode);

        var lastBestNode = snode;
        GridPathNode bestNode = null;
        while ((bestNode = openQueue.Pop()) != null)
        {
            bestNode.IsOpen = false;
            bestNode.IsClosed = true;
            if (bestNode == enode)
            {
                lastBestNode = bestNode;
                break;
            }
            for (int i = 0; i < neighbours.Length - 1; i += 2)
            {
                var neighbourX = bestNode.X + neighbours[i];
                var neighbourZ = bestNode.Z + neighbours[i + 1];
                if (neighbourX < 0 || neighbourX >= xsize || neighbourZ < 0 || neighbourZ >= zsize)
                {
                    continue;
                }
                if (GridMathUtils.GridDistanceApproximately(neighbourX, neighbourZ, middleX, middleZ) > searchRadius)
                {
                    continue;
                }
                var neighbourNode = nodes[neighbourX + neighbourZ * xsize];
                float gCost = bestNode.GCost + GridMathUtils.GridDistanceApproximately(bestNode.X, bestNode.Z, neighbourNode.X, neighbourNode.Z);
                if (neighbourNode.IsOpen || neighbourNode.IsClosed)
                {
                    if (gCost >= neighbourNode.GCost)
                    {
                        continue;
                    }
                    neighbourNode.IsClosed = false;
                }
                else
                {
                    dirtyQueue.Add(neighbourNode);
                }
                neighbourNode.GCost = gCost;
                neighbourNode.HCost = GridMathUtils.GridDistanceApproximately(neighbourX, neighbourZ, enode.X, enode.Z);
                neighbourNode.Parent = bestNode;
                if (neighbourNode.IsOpen)
                {
                    openQueue.Modify(neighbourNode);
                }
                else
                {
                    neighbourNode.IsOpen = true;
                    openQueue.Push(neighbourNode);
                }
                if (neighbourNode.HCost < lastBestNode.HCost)
                {
                    lastBestNode = neighbourNode;
                }
            }
        }

        var curNode = lastBestNode;
        do
        {
            path.Add(curNode);
            curNode = curNode.Parent;
        } while (curNode != null);
        path.Reverse();

        var status = GridPathStatus.Success;
        if (lastBestNode != enode)
        {
            status |= GridPathStatus.PartialResult;
        }
        return status;
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