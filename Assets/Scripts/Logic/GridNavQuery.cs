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
    public bool FindPath(int unitSize, float searchRadiusScale, Func<int, bool> blockedFunc, int startIndex, int endIndex, out List<int> path)
    {
        Debug.Assert(unitSize > 0 && searchRadiusScale > 0);
        path = new List<int>();
        if (!navMesh.GetSquareXZ(startIndex, out int sx, out int sz) || !navMesh.GetSquareXZ(endIndex, out int ex, out int ez))
        {
            return false;
        }
        var snode = nodes[sx + sz * xsize];
        var enode = nodes[ex + ez * xsize];
        if (IsNodeBlocked(unitSize, snode, blockedFunc) || IsNodeBlocked(unitSize, enode, blockedFunc))
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

        var mnode = nodes[(sx + ex) / 2 + (sz + ez) / 2 * xsize];
        float searchRadius = DistanceApproximately(snode, mnode) * searchRadiusScale;

        snode.gCost = 0;
        snode.fCost = DistanceApproximately(snode, enode);
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
                var neighbourNode = nodes[nx + nz * xsize];
                if (DistanceApproximately(neighbourNode, mnode) > searchRadius)
                {
                    continue;
                }
                var gCost = bestNode.gCost + DistanceApproximately(bestNode, neighbourNode);
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
                var hCost = DistanceApproximately(neighbourNode, enode);
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
    public bool FindRawPath(int unitSize, Func<int, bool> blockedFunc, int startIndex, int endIndex, out List<int> path)
    {
        Debug.Assert(unitSize > 0);
        path = new List<int>();
        if (!navMesh.GetSquareXZ(startIndex, out int sx, out int sz) || !navMesh.GetSquareXZ(endIndex, out int ex, out int ez))
        {
            return false;
        }
        var snode = nodes[sx + sz * xsize];
        var enode = nodes[ex + ez * xsize];
        if (IsNodeBlocked(unitSize, snode, blockedFunc) || IsNodeBlocked(unitSize, enode, blockedFunc))
        {
            return false;
        }
        if (snode == enode)
        {
            path.Add(snode.squareIndex);
            return true;
        }
        path.Add(snode.squareIndex);
        int dx = ex - sx;
        int dz = ez - sz;
        int nx = Mathf.Abs(dx);
        int nz = Mathf.Abs(dz);
        int signX = dx > 0 ? 1 : -1;
        int signZ = dz > 0 ? 1 : -1;
        int x = sx;
        int z = sz;
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
            path.Add(nodes[x + z * xsize].squareIndex);
        }
        return true;
    }
    public bool InitSlicedFindPath()
    {
        return true;
    }
    public bool UpdateSlicedFindPath()
    {
        return true;
    }
    public bool FinalizeSlicedFindPath()
    {
        return true;
    }
    public bool FindNearestSquare(int unitSize, Func<int, bool> blockedFunc, Vector3 pos, float radius, out int nearestRef, out Vector3 nearestPos)
    {
        Debug.Assert(unitSize > 0 && radius > 0);
        navMesh.ClampInBounds(pos, out nearestRef, out nearestPos);
        navMesh.GetSquareXZ(nearestRef, out int x, out int z);
        var node = nodes[x + z * xsize];
        if (!IsNodeBlocked(unitSize, node, blockedFunc))
        {
            return true;
        }
        var extent = (int)(radius / navMesh.SquareSize);
        for (int k = 1; k <= extent; k++)
        {
            int xmin = x - k;
            int xmax = x + k;
            int zmin = z - k;
            int zmax = z + k;
            if (!IsNodeBlocked(unitSize, x, zmax, blockedFunc)) //up
            {
                nearestRef = nodes[x + zmax * xsize].squareIndex;
                nearestPos = navMesh.GetSquarePos(nearestRef);
                return true;
            }
            if (!IsNodeBlocked(unitSize, x, zmin, blockedFunc)) //down
            {
                nearestRef = nodes[x + zmin * xsize].squareIndex;
                nearestPos = navMesh.GetSquarePos(nearestRef);
                return true;
            }
            if (!IsNodeBlocked(unitSize, xmin, z, blockedFunc)) //left
            {
                nearestRef = nodes[xmin + z * xsize].squareIndex;
                return true;
            }
            if (!IsNodeBlocked(unitSize, xmax, z, blockedFunc)) //right
            {
                nearestRef = nodes[xmax + z * xsize].squareIndex;
                return true;
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
    }
    private bool IsNeighborWalkable(int unitSize, GridNavQueryNode snode, GridNavQueryNode enode, Func<int, bool> blockedFunc)
    {
        Debug.Assert(unitSize > 0 && snode != null && enode != null && snode != enode);
        Debug.Assert(Mathf.Abs(snode.x - enode.x) <= 1 && Mathf.Abs(snode.z - enode.z) <= 1);

        var offset = (unitSize >> 1);
        if (snode.z == enode.z) //Horizontal
        {
            int x = enode.x + offset * (enode.x - snode.x);
            if (x < 0 || x >= xsize)
            {
                return false;
            }
            for (int i = 0; i < unitSize; i++)
            {
                if (IsNodeBlocked(nodes[x + (enode.z - offset + i) * xsize], blockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
        if (snode.x == enode.x) //Vertical
        {
            int z = enode.z + offset * (enode.z - snode.z);
            if (z < 0 || z >= zsize)
            {
                return false;
            }
            for (int i = 0; i < unitSize; i++)
            {
                if (IsNodeBlocked(nodes[enode.x - offset + i + z * xsize], blockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
        { //Cross
            int x = enode.x + offset * (enode.x - snode.x);
            int z = enode.z + offset * (enode.z - snode.z);
            if (x < 0 || x >= xsize || z < 0 || z >= zsize)
            {
                return false;
            }
            for (int i = 0; i <= unitSize; i++)
            {
                if (IsNodeBlocked(nodes[x - i * (enode.x - snode.x) + z * xsize], blockedFunc))
                {
                    return false;
                }
            }
            for (int i = 1; i <= unitSize; i++)
            {
                if (IsNodeBlocked(nodes[x + (z - i * (enode.z - snode.z)) * xsize], blockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
    }
    private bool IsNodeCenterBlocked(GridNavQueryNode node, Func<int, bool> blockedFunc)
    {
        Debug.Assert(node != null);
        return navMesh.IsSquareBlocked(node.squareIndex) || (blockedFunc != null && blockedFunc(node.squareIndex));
    }
    private bool IsNodeBlocked(int unitSize, GridNavQueryNode node, Func<int, bool> blockedFunc)
    {
        Debug.Assert(unitSize > 0);
        return IsNodeBlocked(unitSize, node.x, node.z, blockedFunc);
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
                if (IsNodeCenterBlocked(nodes[tx + tz * xsize], blockedFunc))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private static float DistanceApproximately(GridNavQueryNode snode, GridNavQueryNode enode)
    {
        int dx = Mathf.Abs(enode.x - snode.x);
        int dz = Mathf.Abs(enode.z - snode.z);
        return (dx + dz) + Mathf.Min(dx, dz) * (1.4142f - 2.0f);
    }
}