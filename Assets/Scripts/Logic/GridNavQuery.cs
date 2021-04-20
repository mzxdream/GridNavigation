using System;
using System.Collections.Generic;
using UnityEngine;

public enum GridNavQueryStatus { Success, Failed, InProgress, }

class GridNavQueryData
{
    public GridNavQueryStatus status;
    public int startIndex;
    public GridNavQueryNode lastBestNode;
    public float lastBestNodeCost;
}

public class GridNavQuery
{
    private GridNavMesh navMesh;
    private GridNavQueryNodePool nodePool;
    private GridNavQueryPriorityQueue openQueue;
    private GridNavQueryData queryData;

    public bool Init(GridNavMesh navMesh, int maxNodes = 8192)
    {
        Debug.Assert(maxNodes > 0);
        this.navMesh = navMesh;
        this.nodePool = new GridNavQueryNodePool(maxNodes);
        this.openQueue = new GridNavQueryPriorityQueue(maxNodes);
        this.queryData = new GridNavQueryData();
        return true;
    }
    public void Clear()
    {
    }
    public bool FindPath(IGridNavQueryFilter filter, int startIndex, IGridNavQueryConstraint constraint, out List<int> path)
    {
        Debug.Assert(filter != null);
        path = new List<int>();
        if (filter.IsBlocked(navMesh, startIndex))
        {
            return false;
        }
        if (constraint.IsGoal(navMesh, startIndex))
        {
            path.Add(startIndex);
            return true;
        }

        nodePool.Clear();
        openQueue.Clear();

        var snode = nodePool.GetNode(startIndex);
        if (snode == null)
        {
            return false;
        }
        snode.gCost = 0;
        snode.fCost = constraint.GetHeuristicCost(navMesh, startIndex);
        snode.parent = null;
        snode.flags |= (int)GridNavNodeFlags.Open;
        openQueue.Push(snode);

        var lastBestNode = snode;
        var lastBestNodeCost = snode.fCost;
        GridNavQueryNode bestNode = null;
        while ((bestNode = openQueue.Pop()) != null)
        {
            bestNode.flags &= ~(int)GridNavNodeFlags.Open;
            bestNode.flags |= (int)GridNavNodeFlags.Closed;
            if (constraint.IsGoal(navMesh, bestNode.index))
            {
                lastBestNode = bestNode;
                break;
            }
            var leftBlocked = TestNeighbourBlocked(filter, constraint, bestNode, GridNavDirection.Left, ref lastBestNodeCost, ref lastBestNode);
            var rightBlocked = TestNeighbourBlocked(filter, constraint, bestNode, GridNavDirection.Right, ref lastBestNodeCost, ref lastBestNode);
            var upBlocked = TestNeighbourBlocked(filter, constraint, bestNode, GridNavDirection.Up, ref lastBestNodeCost, ref lastBestNode);
            var downBlocked = TestNeighbourBlocked(filter, constraint, bestNode, GridNavDirection.Down, ref lastBestNodeCost, ref lastBestNode);
            if (!leftBlocked)
            {
                if (!upBlocked)
                {
                    TestNeighbourBlocked(filter, constraint, bestNode, GridNavDirection.LeftUp, ref lastBestNodeCost, ref lastBestNode);
                }
                if (!downBlocked)
                {
                    TestNeighbourBlocked(filter, constraint, bestNode, GridNavDirection.LeftDown, ref lastBestNodeCost, ref lastBestNode);
                }
            }
            if (!rightBlocked)
            {
                if (!upBlocked)
                {
                    TestNeighbourBlocked(filter, constraint, bestNode, GridNavDirection.RightUp, ref lastBestNodeCost, ref lastBestNode);
                }
                if (!downBlocked)
                {
                    TestNeighbourBlocked(filter, constraint, bestNode, GridNavDirection.RightDown, ref lastBestNodeCost, ref lastBestNode);
                }
            }
        }
        var curNode = lastBestNode;
        do
        {
            path.Add(curNode.index);
            curNode = curNode.parent;
        } while (curNode != null);
        path.Reverse();
        return true;
    }
    public bool Raycast(IGridNavQueryFilter filter, int startIndex, int endIndex, out List<int> path, out float totalCost)
    {
        Debug.Assert(filter != null);
        path = new List<int>();
        totalCost = 0.0f;
        if (filter.IsBlocked(navMesh, startIndex))
        {
            return false;
        }
        if (startIndex == endIndex)
        {
            path.Add(startIndex);
            return true;
        }
        path.Add(startIndex);
        navMesh.GetSquareXZ(startIndex, out var sx, out var sz);
        navMesh.GetSquareXZ(endIndex, out var ex, out var ez);
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
        var parentIndex = startIndex;
        while (ix < nx || iz < nz)
        {
            var t1 = (2 * ix + 1) * nz;
            var t2 = (2 * iz + 1) * nx;
            if (t1 < t2) //Horizontal
            {
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                z += signZ;
                iz++;
            }
            else //Cross
            {
                var xIndex = navMesh.GetSquareIndex(x + signX, z);
                var zIndex = navMesh.GetSquareIndex(x, z + signZ);
                if (filter.IsBlocked(navMesh, xIndex) || filter.IsBlocked(navMesh, zIndex))
                {
                    return false;
                }
                x += signX;
                z += signZ;
                ix++;
                iz++;
            }
            var index = navMesh.GetSquareIndex(x, z);
            var cost = filter.GetCost(navMesh, index, parentIndex);
            if (cost < 0)
            {
                return false;
            }
            if (filter.IsBlocked(navMesh, index))
            {
                return false;
            }
            totalCost += cost;
            path.Add(index);
            parentIndex = index;
        }
        return true;
    }
    public bool FindStraightPath(int unitSize, List<int> path, out List<int> straightPath, Func<int, bool> blockedFunc = null)
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
    public GridNavQueryStatus InitSlicedFindPath(int unitSize, float searchRadiusScale, int startIndex, int endIndex, Func<int, bool> blockedFunc)
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
    public bool FindNearestSquare(int unitSize, Vector3 pos, float radius, out int nearestIndex, out Vector3 nearestPos, Func<int, bool> blockedFunc)
    {
        Debug.Assert(unitSize > 0 && radius > 0);
        navMesh.ClampInBounds(pos, out nearestIndex, out nearestPos);
        navMesh.GetSquareXZ(nearestIndex, out int x, out int z);
        var node = nodes[x + z * xsize];
        var ext = Mathf.Max(1, (int)(radius / navMesh.SquareSize + 0.9f));
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
        int signX = enode.x - snode.x;
        int signZ = enode.z - enode.z;
        Debug.Assert(signX >= -1 && signX <= 1 && signZ >= -1 && signZ <= 1);

        if (signZ == 0) //Horizontal
        {
            int x = enode.x + (unitSize - 1) * signX;
            if (x < 0 || x >= xsize)
            {
                return false;
            }
            for (int i = -(unitSize - 1); i < unitSize; i++)
            {
                if (IsNodeCenterBlocked(x, enode.z + i, blockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
        if (signX == 0) //Vertical
        {
            int z = enode.z + (unitSize - 1) * signZ;
            if (z < 0 || z >= zsize)
            {
                return false;
            }
            for (int i = -(unitSize - 1); i < unitSize; i++)
            {
                if (IsNodeCenterBlocked(enode.x + i, z, blockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
        { //Cross
            int x = enode.x + (unitSize - 1) * signX;
            int z = enode.z + (unitSize - 1) * signZ;
            if (x < 0 || x >= xsize || z < 0 || z >= zsize)
            {
                return false;
            }
            for (int i = 0; i < unitSize * 2; i++)
            {
                if (IsNodeCenterBlocked(x - i * signX, z, blockedFunc))
                {
                    return false;
                }
            }
            for (int i = 1; i < unitSize * 2; i++)
            {
                if (IsNodeCenterBlocked(x, z - i * signZ, blockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
    }
    private bool IsNodeCenterBlocked(int x, int z, Func<int, bool> blockedFunc)
    {
        Debug.Assert(x < 0 || x >= xsize || z < 0 || z >= zsize);
        var node = nodes[x + z * xsize];
        return navMesh.IsSquareBlocked(node.squareIndex) || (blockedFunc != null && blockedFunc(node.squareIndex));
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
        int xmin = x - (unitSize - 1);
        int xmax = x + (unitSize - 1);
        int zmin = z - (unitSize - 1);
        int zmax = z + (unitSize - 1);
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
    private bool TestNeighbourBlocked(IGridNavQueryFilter filter, IGridNavQueryConstraint constraint, GridNavQueryNode node, GridNavDirection dir, ref float lastBestNodeCost, ref GridNavQueryNode lastBestNode)
    {
        var neighbourIndex = navMesh.GetSuqareNeighbourIndex(node.index, dir);
        if (neighbourIndex == -1)
        {
            return true;
        }
        var neighbourNode = nodePool.GetNode(neighbourIndex);
        if (neighbourNode == null)
        {
            return true;
        }
        if ((neighbourNode.flags & (int)(GridNavNodeFlags.Closed | GridNavNodeFlags.Blocked)) != 0)
        {
            return (neighbourNode.flags & (int)GridNavNodeFlags.Blocked) != 0;
        }
        if ((neighbourNode.flags & (int)GridNavNodeFlags.Open) != 0)
        {
            var gCost = filter.GetCost(navMesh, neighbourIndex, dir);
            if (gCost < 0)
            {
                return true;
            }
            gCost += node.gCost;
            if (gCost < neighbourNode.gCost)
            {
                neighbourNode.gCost = gCost;
                neighbourNode.fCost = gCost + constraint.GetHeuristicCost(navMesh, neighbourIndex);
                neighbourNode.parent = node;
                openQueue.Modify(neighbourNode);
            }
        }
        else
        {
            if (!constraint.WithinConstraints(navMesh, neighbourIndex) || filter.IsBlocked(navMesh, neighbourIndex))
            {
                neighbourNode.flags |= (int)(GridNavNodeFlags.Closed | GridNavNodeFlags.Blocked);
                return true;
            }
            var gCost = filter.GetCost(navMesh, neighbourIndex, dir);
            if (gCost < 0)
            {
                return true;
            }
            gCost += node.gCost;
            var hCost = constraint.GetHeuristicCost(navMesh, neighbourIndex);
            neighbourNode.gCost = gCost;
            neighbourNode.fCost = gCost + hCost;
            neighbourNode.parent = node;
            neighbourNode.flags |= (int)GridNavNodeFlags.Open;
            openQueue.Push(neighbourNode);
            if (hCost < lastBestNodeCost)
            {
                lastBestNodeCost = hCost;
                lastBestNode = neighbourNode;
            }
        }
        return false;
    }
}