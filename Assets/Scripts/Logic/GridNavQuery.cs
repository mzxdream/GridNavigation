using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public class NavQueryConstraint
    {
        public NavAgent agent;
        public int sx;
        public int sz;
        public Vector3 startPos;
        public int ex;
        public int ez;
        public Vector3 goalPos;
        public float goalRadius;

        public float GetHeuristicCost(NavQuery navQuery, int x, int z)
        {
            return NavMathUtils.DistanceApproximately(x, z, ex, ez);
        }
    }

    public enum NavQueryStatus { Success = 1, Failed = 2, InProgress = 4, Partial = 8 }

    public class NavQuery
    {
        class QueryData
        {
            public NavQueryStatus status;
            public NavQueryConstraint constraint;
            public NavQueryNode lastBestNode;
            public float lastBestNodeCost;
        }

        private NavMap navMap;
        private NavBlockingObjectMap blockingObjectMap;
        private NavQueryNodePool nodePool;
        private NavQueryPriorityQueue openQueue;
        private QueryData queryData;

        public bool Init(NavMap navMap, NavBlockingObjectMap blockingObjectMap, int maxNodes = 8192)
        {
            Debug.Assert(navMap != null && blockingObjectMap != null && maxNodes > 0);
            this.navMap = navMap;
            this.blockingObjectMap = blockingObjectMap;
            this.nodePool = new NavQueryNodePool(maxNodes);
            this.openQueue = new NavQueryPriorityQueue(maxNodes);
            this.queryData = new QueryData();
            return true;
        }
        public void Clear()
        {
        }
        public NavManager GetNavManager()
        {
            return navManager;
        }
        public NavQueryStatus InitSlicedFindPath(NavQueryConstraint constraint)
        {
            Debug.Assert(constraint != null);

            navManager.GetSquareXZ(constraint.startPos, out var sx, out var sz);
            queryData.status = NavQueryStatus.Failed;
            queryData.constraint = constraint;
            queryData.sx = sx;
            queryData.sz = sz;
            queryData.lastBestNode = null;
            queryData.lastBestNodeCost = 0.0f;

            var speedMod = navManager.GetSquareSpeed(constraint.agent, sx, sz);
            if (speedMod <= 0.0f)
            {
                return NavQueryStatus.Failed;
            }
            var blockTypes = navManager.TestObjectBlockTypes(constraint.agent, sx, sz);
            if ((blockTypes & NavBlockType.Block) != 0)
            {
                return NavQueryStatus.Failed;
            }

            nodePool.Clear();
            openQueue.Clear();

            var snode = nodePool.GetNode(sx, sz);
            if (snode == null)
            {
                return NavQueryStatus.Failed;
            }
            snode.gCost = 0;
            snode.fCost = constraint.GetHeuristicCost(this, sx, sz);
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
}