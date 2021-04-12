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
    public bool IsEmpty()
    {
        return count == 0;
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

public enum GridNavQueryStatus { Success, Failed, InProgress, }

class GridNavQueryData
{
    public GridNavQueryStatus status;
    public int unitSize;
    public Func<int, bool> blockedFunc;
    public GridNavQueryNode snode;
    public GridNavQueryNode enode;
    public GridNavQueryNode mnode;
    public float searchRadius;
    public GridNavQueryNode lastBestNode;
    public float lastBestNodeCost;
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
    private GridNavQueryData queryData;

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
        this.queryData = new GridNavQueryData { status = GridNavQueryStatus.Failed };
        return true;
    }
    public void Clear()
    {
    }
    public bool FindPath(int unitSize, Func<int, bool> blockedFunc, float searchRadiusScale, int startIndex, int endIndex, out List<int> path)
    {
        Debug.Assert(unitSize > 0 && searchRadiusScale > 0);
        path = new List<int>();
        if (!navMesh.GetSquareXZ(startIndex, out int sx, out int sz) || !navMesh.GetSquareXZ(endIndex, out int ex, out int ez))
        {
            return false;
        }
        var snode = nodes[sx + sz * xsize];
        var enode = nodes[ex + ez * xsize];
        if (IsNodeBlocked(unitSize, blockedFunc, snode))
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
        if (IsNodeBlocked(unitSize, blockedFunc, snode))
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
                if (!IsNeighborWalkable(unitSize, blockedFunc, nodes[x + z * xsize], nodes[x + signX + z * xsize]))
                {
                    return false;
                }
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                if (!IsNeighborWalkable(unitSize, blockedFunc, nodes[x + z * xsize], nodes[x + (z + signZ) * xsize]))
                {
                    return false;
                }
                z += signZ;
                iz++;
            }
            else //Cross
            {
                if (!IsNeighborWalkable(unitSize, blockedFunc, nodes[x + z * xsize], nodes[x + signX + (z + signZ) * xsize]))
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
    public bool FindStraightPath(int unitSize, Func<int, bool> blockedFunc, List<int> path, out List<int> straightPath)
    {
        Debug.Assert(unitSize > 0);
        straightPath = new List<int>();
        if (path.Count <= 1)
        {
            return false;
        }
        //去除直线上的点
        straightPath.Add(path[0]);
        int oldDir = path[1] - path[0];
        for (int i = 2; i < path.Count; i++)
        {
            int newDir = path[i] - path[i - 1];
            if (oldDir != newDir)
            {
                oldDir = newDir;
                straightPath.Add(path[i - 1]);
            }
        }
        straightPath.Add(path[path.Count - 1]);
        //去除可以直达的拐点
        for (int i = straightPath.Count - 1; i > 1; i--)
        {
            for (int j = 0; j < i - 1; j++)
            {
                if (IsCrossWalkable(unitSize, blockedFunc, straightPath[i], straightPath[j]))
                {
                    for (int k = i - 1; k > j; k--)
                    {
                        straightPath.RemoveAt(k);
                    }
                    i = j + 1;
                    break;
                }
            }
        }
        return true;
    }
    public GridNavQueryStatus InitSlicedFindPath(int unitSize, Func<int, bool> blockedFunc, float searchRadiusScale, int startIndex, int endIndex)
    {
        Debug.Assert(unitSize > 0 && searchRadiusScale > 0);
        queryData.status = GridNavQueryStatus.Failed;
        if (!navMesh.GetSquareXZ(startIndex, out int sx, out int sz) || !navMesh.GetSquareXZ(endIndex, out int ex, out int ez))
        {
            return queryData.status;
        }
        queryData.unitSize = unitSize;
        queryData.blockedFunc = blockedFunc;
        queryData.snode = nodes[sx + sz * xsize];
        queryData.enode = nodes[ex + ez * xsize];
        if (IsNodeBlocked(unitSize, blockedFunc, queryData.snode))
        {
            return queryData.status;
        }
        if (queryData.snode == queryData.enode)
        {
            queryData.status = GridNavQueryStatus.Success;
            return queryData.status;
        }

        foreach (var n in dirtyQueue)
        {
            n.flags &= ~(int)(GridNavNodeFlags.Open | GridNavNodeFlags.Closed);
        }
        dirtyQueue.Clear();
        openQueue.Clear();

        queryData.mnode = nodes[(sx + ex) / 2 + (sz + ez) / 2 * xsize];
        queryData.searchRadius = DistanceApproximately(queryData.snode, queryData.mnode) * searchRadiusScale;

        queryData.snode.gCost = 0;
        queryData.snode.fCost = DistanceApproximately(queryData.snode, queryData.enode);
        queryData.snode.parent = null;
        queryData.snode.flags |= (int)GridNavNodeFlags.Open;
        dirtyQueue.Add(queryData.snode);
        openQueue.Push(queryData.snode);

        queryData.lastBestNode = queryData.snode;
        queryData.lastBestNodeCost = queryData.snode.fCost;
        queryData.status = GridNavQueryStatus.InProgress;
        return queryData.status;
    }
    public GridNavQueryStatus UpdateSlicedFindPath(int maxNodes, out int doneNodes)
    {
        doneNodes = 0;
        if (queryData.status != GridNavQueryStatus.InProgress)
        {
            return queryData.status;
        }
        if (IsNodeBlocked(queryData.unitSize, queryData.blockedFunc, queryData.snode))
        {
            queryData.status = GridNavQueryStatus.Failed;
            return queryData.status;
        }
        GridNavQueryNode bestNode = null;
        while (doneNodes < maxNodes && (bestNode = openQueue.Pop()) != null)
        {
            doneNodes++;
            bestNode.flags &= ~(int)GridNavNodeFlags.Open;
            bestNode.flags |= (int)GridNavNodeFlags.Closed;
            if (bestNode == queryData.enode)
            {
                queryData.lastBestNode = bestNode;
                queryData.status = GridNavQueryStatus.Success;
                return queryData.status;
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
                if (DistanceApproximately(neighbourNode, queryData.mnode) > queryData.searchRadius)
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
                var hCost = DistanceApproximately(neighbourNode, queryData.enode);
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
                if (hCost < queryData.lastBestNodeCost)
                {
                    queryData.lastBestNodeCost = hCost;
                    queryData.lastBestNode = neighbourNode;
                }
            }
        }
        if (openQueue.IsEmpty())
        {
            queryData.status = GridNavQueryStatus.Success;
        }
        return queryData.status;
    }
    public GridNavQueryStatus FinalizeSlicedFindPath(out List<int> path)
    {
        path = new List<int>();
        if (queryData.status == GridNavQueryStatus.Failed)
        {
            return queryData.status;
        }
        if (queryData.lastBestNode == null)
        {
            queryData.status = GridNavQueryStatus.Failed;
            return queryData.status;
        }
        var curNode = queryData.lastBestNode;
        do
        {
            path.Add(curNode.squareIndex);
            curNode = curNode.parent;
        } while (curNode != null);
        path.Reverse();

        return queryData.status;
    }
    public bool FindNearestSquare(int unitSize, Func<int, bool> blockedFunc, Vector3 pos, float radius, out int nearestIndex, out Vector3 nearestPos)
    {
        Debug.Assert(unitSize > 0 && radius > 0);
        navMesh.ClampInBounds(pos, out nearestIndex, out nearestPos);
        navMesh.GetSquareXZ(nearestIndex, out int x, out int z);
        var node = nodes[x + z * xsize];
        var ext = (int)(radius / navMesh.SquareSize);
        var nearestNode = FindNearestNode(unitSize, blockedFunc, node, ext);
        if (nearestNode == null)
        {
            return false;
        }
        if (node != nearestNode)
        {
            nearestIndex = nearestNode.squareIndex;
            nearestPos = navMesh.GetSquarePos(nearestIndex);
        }
        return true;
    }
    private GridNavQueryNode FindNearestNode(int unitSize, Func<int, bool> blockedFunc, GridNavQueryNode node, int ext)
    {
        Debug.Assert(unitSize > 0 && node != null);
        if (!IsNodeBlocked(unitSize, blockedFunc, node))
        {
            return node;
        }
        int x = node.x;
        int z = node.z;
        for (int k = 1; k <= ext; k++)
        {
            int xmin = x - k;
            int xmax = x + k;
            int zmin = z - k;
            int zmax = z + k;
            if (!IsNodeBlocked(unitSize, blockedFunc, x, zmax)) //up
            {
                return nodes[x + zmax * xsize];
            }
            if (!IsNodeBlocked(unitSize, blockedFunc, x, zmin)) //down
            {
                return nodes[x + zmin * xsize];
            }
            if (!IsNodeBlocked(unitSize, blockedFunc, xmin, z)) //left
            {
                return nodes[xmin + z * xsize];
            }
            if (!IsNodeBlocked(unitSize, blockedFunc, xmax, z)) //right
            {
                return nodes[xmax + z * xsize];
            }
            for (int t = 1; t < k; t++)
            {
                if (!IsNodeBlocked(unitSize, blockedFunc, x - t, zmax)) //up left
                {
                    return nodes[x - t + zmax * xsize];
                }
                if (!IsNodeBlocked(unitSize, blockedFunc, x + t, zmax)) //up right
                {
                    return nodes[x + t + zmax * xsize];
                }
                if (!IsNodeBlocked(unitSize, blockedFunc, x - t, zmin)) //down left
                {
                    return nodes[x - t + zmin * xsize];
                }
                if (!IsNodeBlocked(unitSize, blockedFunc, x + t, zmin)) //down right
                {
                    return nodes[x + t + zmin * xsize];
                }
                if (!IsNodeBlocked(unitSize, blockedFunc, xmin, z - t)) //left up
                {
                    return nodes[xmin + (z - t) * xsize];
                }
                if (!IsNodeBlocked(unitSize, blockedFunc, xmin, z + t)) //left down
                {
                    return nodes[xmin + (z + t) * xsize];
                }
                if (!IsNodeBlocked(unitSize, blockedFunc, xmax, z - t)) //right up
                {
                    return nodes[xmax + (z - t) * xsize];
                }
                if (!IsNodeBlocked(unitSize, blockedFunc, xmax, z + t)) //right down
                {
                    return nodes[xmax + (z + t) * xsize];
                }
            }
            if (!IsNodeBlocked(unitSize, blockedFunc, xmin, zmax)) //left up
            {
                return nodes[xmin + zmax * xsize];
            }
            if (!IsNodeBlocked(unitSize, blockedFunc, xmax, zmax)) //right up
            {
                return nodes[xmax + zmax * xsize];
            }
            if (!IsNodeBlocked(unitSize, blockedFunc, xmin, zmin)) //left down
            {
                return nodes[xmin + zmin * xsize];
            }
            if (!IsNodeBlocked(unitSize, blockedFunc, xmax, zmin)) //right down
            {
                return nodes[xmax + zmin * xsize];
            }
        }
        return null;
    }
    private bool IsCrossWalkable(int unitSize, Func<int, bool> blockedFunc, int startIndex, int endIndex)
    {
        Debug.Assert(unitSize > 0);
        if (!navMesh.GetSquareXZ(startIndex, out int sx, out int sz) || !navMesh.GetSquareXZ(endIndex, out int ex, out int ez))
        {
            return false;
        }
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
                if (!IsNeighborWalkable(unitSize, blockedFunc, nodes[x + z * xsize], nodes[x + signX + z * xsize]))
                {
                    return false;
                }
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                if (!IsNeighborWalkable(unitSize, blockedFunc, nodes[x + z * xsize], nodes[x + (z + signZ) * xsize]))
                {
                    return false;
                }
                z += signZ;
                iz++;
            }
            else //Cross
            {
                if (!IsNeighborWalkable(unitSize, blockedFunc, nodes[x + z * xsize], nodes[x + signX + (z + signZ) * xsize]))
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
    private bool IsNeighborWalkable(int unitSize, Func<int, bool> blockedFunc, GridNavQueryNode snode, GridNavQueryNode enode)
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
                if (IsNodeCenterBlocked(nodes[x + (enode.z - offset + i) * xsize], blockedFunc))
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
                if (IsNodeCenterBlocked(nodes[enode.x - offset + i + z * xsize], blockedFunc))
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
                if (IsNodeCenterBlocked(nodes[x - i * (enode.x - snode.x) + z * xsize], blockedFunc))
                {
                    return false;
                }
            }
            for (int i = 1; i <= unitSize; i++)
            {
                if (IsNodeCenterBlocked(nodes[x + (z - i * (enode.z - snode.z)) * xsize], blockedFunc))
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
    private bool IsNodeBlocked(int unitSize, Func<int, bool> blockedFunc, GridNavQueryNode node)
    {
        Debug.Assert(unitSize > 0);
        return IsNodeBlocked(unitSize, blockedFunc, node.x, node.z);
    }
    private bool IsNodeBlocked(int unitSize, Func<int, bool> blockedFunc, int x, int z)
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