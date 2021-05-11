using System;
using System.Collections.Generic;
using UnityEngine;

public enum GridNavQueryStatus { Success, Failed, InProgress, }

class GridNavQueryData
{
    public GridNavQueryStatus status;
    public IGridNavQueryFilter filter;
    public int startIndex;
    public IGridNavQueryConstraint constraint;
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
    public GridNavMesh GetNavMesh()
    {
        return navMesh;
    }
    public bool FindPath(IGridNavQueryFilter filter, int startIndex, IGridNavQueryConstraint constraint, out List<int> path)
    {
        Debug.Assert(filter != null && constraint != null);
        path = new List<int>();
        if (filter.IsBlocked(navMesh, startIndex))
        {
            return false;
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
        var dirX = dx > 0 ? GridNavDirection.Right : GridNavDirection.Left;
        var dirZ = dz > 0 ? GridNavDirection.Up : GridNavDirection.Down;
        var dirXZ = GridNavMath.CombineDirection(dirX, dirZ);
        var prevDir = GridNavDirection.None;
        while (ix < nx || iz < nz)
        {
            var t1 = (2 * ix + 1) * nz;
            var t2 = (2 * iz + 1) * nx;
            if (t1 < t2) //Horizontal
            {
                x += signX;
                ix++;
                if (prevDir == dirZ) //防止对角线过不去，但是可以先上下再左右
                {
                    var tIndex = navMesh.GetSquareIndex(x, z);
                    if (filter.IsBlocked(navMesh, tIndex))
                    {
                        return false;
                    }
                    var tCost = filter.GetCost(navMesh, tIndex, dirXZ);
                    if (tCost < 0)
                    {
                        return false;
                    }
                }
                var index = navMesh.GetSquareIndex(x, z);
                if (filter.IsBlocked(navMesh, index))
                {
                    return false;
                }
                var cost = filter.GetCost(navMesh, index, dirX);
                if (cost < 0)
                {
                    return false;
                }
                totalCost += cost;
                path.Add(index);
                prevDir = dirX;
            }
            else if (t1 > t2) //Vertical
            {
                z += signZ;
                iz++;
                if (prevDir == dirX) //防止对角线过不去，但是可以先上下再左右
                {
                    var tIndex = navMesh.GetSquareIndex(x, z);
                    if (filter.IsBlocked(navMesh, tIndex))
                    {
                        return false;
                    }
                    var tCost = filter.GetCost(navMesh, tIndex, dirXZ);
                    if (tCost < 0)
                    {
                        return false;
                    }
                }
                var index = navMesh.GetSquareIndex(x, z);
                if (filter.IsBlocked(navMesh, index))
                {
                    return false;
                }
                var cost = filter.GetCost(navMesh, index, dirZ);
                if (cost < 0)
                {
                    return false;
                }
                totalCost += cost;
                path.Add(index);
                prevDir = dirZ;
            }
            else //Cross
            {
                var xIndex = navMesh.GetSquareIndex(x + signX, z);
                var zIndex = navMesh.GetSquareIndex(x, z + signZ);
                if (filter.IsBlocked(navMesh, xIndex) || filter.GetCost(navMesh, xIndex, dirX) < 0
                    || filter.IsBlocked(navMesh, zIndex) || filter.GetCost(navMesh, zIndex, dirZ) < 0)
                {
                    return false;
                }
                x += signX;
                z += signZ;
                ix++;
                iz++;
                var index = navMesh.GetSquareIndex(x, z);
                if (filter.IsBlocked(navMesh, index))
                {
                    return false;
                }
                var cost = filter.GetCost(navMesh, index, dirXZ);
                if (cost < 0)
                {
                    return false;
                }
                totalCost += cost;
                path.Add(index);
                prevDir = dirXZ;
            }
        }
        return true;
    }
    public bool FindStraightPath(IGridNavQueryFilter filter, List<int> path, out List<int> straightPath)
    {
        Debug.Assert(filter != null);
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
                if (Raycast(filter, straightPath[i], straightPath[j], out _, out _))
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
    public GridNavQueryStatus InitSlicedFindPath(IGridNavQueryFilter filter, int startIndex, IGridNavQueryConstraint constraint)
    {
        Debug.Assert(filter != null && constraint != null);
        queryData.status = GridNavQueryStatus.Failed;
        queryData.filter = filter;
        queryData.startIndex = startIndex;
        queryData.constraint = constraint;
        queryData.lastBestNode = null;
        queryData.lastBestNodeCost = 0.0f;
        if (filter.IsBlocked(navMesh, startIndex))
        {
            return queryData.status;
        }

        nodePool.Clear();
        openQueue.Clear();

        var snode = nodePool.GetNode(startIndex);
        if (snode == null)
        {
            return queryData.status;
        }
        snode.gCost = 0;
        snode.fCost = constraint.GetHeuristicCost(navMesh, startIndex);
        snode.parent = null;
        snode.flags |= (int)GridNavNodeFlags.Open;
        openQueue.Push(snode);

        queryData.lastBestNode = snode;
        queryData.lastBestNodeCost = snode.fCost;
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
        GridNavQueryNode bestNode = null;
        while (doneNodes < maxNodes && (bestNode = openQueue.Pop()) != null)
        {
            doneNodes++;
            bestNode.flags &= ~(int)GridNavNodeFlags.Open;
            bestNode.flags |= (int)GridNavNodeFlags.Closed;
            if (queryData.constraint.IsGoal(navMesh, bestNode.index))
            {
                queryData.lastBestNode = bestNode;
                queryData.status = GridNavQueryStatus.Success;
                return queryData.status;
            }
            var leftBlocked = TestNeighbourBlocked(queryData.filter, queryData.constraint, bestNode, GridNavDirection.Left, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
            var rightBlocked = TestNeighbourBlocked(queryData.filter, queryData.constraint, bestNode, GridNavDirection.Right, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
            var upBlocked = TestNeighbourBlocked(queryData.filter, queryData.constraint, bestNode, GridNavDirection.Up, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
            var downBlocked = TestNeighbourBlocked(queryData.filter, queryData.constraint, bestNode, GridNavDirection.Down, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
            if (!leftBlocked)
            {
                if (!upBlocked)
                {
                    TestNeighbourBlocked(queryData.filter, queryData.constraint, bestNode, GridNavDirection.LeftUp, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                }
                if (!downBlocked)
                {
                    TestNeighbourBlocked(queryData.filter, queryData.constraint, bestNode, GridNavDirection.LeftDown, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                }
            }
            if (!rightBlocked)
            {
                if (!upBlocked)
                {
                    TestNeighbourBlocked(queryData.filter, queryData.constraint, bestNode, GridNavDirection.RightUp, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                }
                if (!downBlocked)
                {
                    TestNeighbourBlocked(queryData.filter, queryData.constraint, bestNode, GridNavDirection.RightDown, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
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
        var curNode = queryData.lastBestNode;
        if (curNode == null)
        {
            queryData.status = GridNavQueryStatus.Failed;
            return queryData.status;
        }
        do
        {
            path.Add(curNode.index);
            curNode = curNode.parent;
        } while (curNode != null);
        path.Reverse();
        return queryData.status;
    }
    public bool FindNearestSquare(IGridNavQueryFilter filter, Vector3 pos, float radius, out int nearestIndex, out Vector3 nearestPos)
    {
        Debug.Assert(filter != null && radius > 0);
        navMesh.ClampInBounds(pos, out nearestIndex, out nearestPos);
        if (!filter.IsBlocked(navMesh, nearestIndex))
        {
            return true;
        }
        navMesh.GetSquareXZ(nearestIndex, out int x, out int z);
        var ext = (int)(radius / navMesh.SquareSize);
        for (int k = 1; k <= ext; k++)
        {
            int xmin = x - k;
            int xmax = x + k;
            int zmin = z - k;
            int zmax = z + k;
            if (!TestBlocked(filter, xmin, z, ref nearestIndex) //left
                || !TestBlocked(filter, xmax, z, ref nearestIndex) //right
                || !TestBlocked(filter, x, zmax, ref nearestIndex) //up
                || !TestBlocked(filter, x, zmin, ref nearestIndex)) //down
            {
                nearestPos = navMesh.GetSquarePos(nearestIndex);
                return true;
            }
            for (int t = 1; t < k; t++)
            {
                if (!TestBlocked(filter, xmin, z + t, ref nearestIndex) //left up
                    || !TestBlocked(filter, xmin, z - t, ref nearestIndex) //left down
                    || !TestBlocked(filter, xmax, z + t, ref nearestIndex) //right up
                    || !TestBlocked(filter, xmax, z - t, ref nearestIndex) //right down
                    || !TestBlocked(filter, x - t, zmax, ref nearestIndex) //up left
                    || !TestBlocked(filter, x + t, zmax, ref nearestIndex) //up right
                    || !TestBlocked(filter, x - t, zmin, ref nearestIndex) //down left
                    || !TestBlocked(filter, x + t, zmin, ref nearestIndex)) //down right
                {
                    nearestPos = navMesh.GetSquarePos(nearestIndex);
                    return true;
                }
            }
            if (!TestBlocked(filter, xmin, zmax, ref nearestIndex) //left up
                || !TestBlocked(filter, xmin, zmin, ref nearestIndex) //left down
                || !TestBlocked(filter, xmax, zmax, ref nearestIndex) //right up
                || !TestBlocked(filter, xmax, zmin, ref nearestIndex)) //right down
            {
                nearestPos = navMesh.GetSquarePos(nearestIndex);
                return true;
            }
        }
        return false;
    }
    private bool TestBlocked(IGridNavQueryFilter filter, int x, int z, ref int index)
    {
        if (x >= 0 && x < navMesh.XSize && z >= 0 && z < navMesh.ZSize)
        {
            var t = navMesh.GetSquareIndex(x, z);
            if (!filter.IsBlocked(navMesh, t))
            {
                index = t;
                return false;
            }
        }
        return true;
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