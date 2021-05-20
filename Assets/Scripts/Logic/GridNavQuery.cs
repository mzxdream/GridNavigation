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
        public bool testMobile;

        public virtual float GetHeuristicCost(NavQuery navQuery, int x, int z)
        {
            return NavMathUtils.DistanceApproximately(x, z, ex, ez) * navQuery.GetNavMap().SquareSize;
        }
        public virtual bool IsGoal(NavQuery navQuery, int x, int z)
        {
            return NavMathUtils.SqrDistance2D(navQuery.GetNavMap().GetSquarePos(x, z), goalPos) <= goalRadius * goalRadius;
        }
        public virtual bool WithinConstraints(NavQuery navQuery, int x, int z)
        {
            return true;
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
            this.nodePool = new NavQueryNodePool(navMap.XSize, navMap.ZSize, maxNodes);
            this.openQueue = new NavQueryPriorityQueue(maxNodes);
            this.queryData = new QueryData();
            return true;
        }
        public void Clear()
        {
        }
        public NavMap GetNavMap()
        {
            return navMap;
        }
        public NavBlockingObjectMap GetBlockingObjectMap()
        {
            return blockingObjectMap;
        }
        public NavQueryStatus InitSlicedFindPath(NavQueryConstraint constraint)
        {
            Debug.Assert(constraint != null);

            queryData.status = NavQueryStatus.Failed;
            queryData.constraint = constraint;
            queryData.lastBestNode = null;
            queryData.lastBestNodeCost = 0.0f;

            var speed = NavUtils.GetAgentSquareSpeed(constraint.agent, navMap, constraint.sx, constraint.sz);
            if (speed <= 0.0f)
            {
                return NavQueryStatus.Failed;
            }
            var blockTypes = blockingObjectMap.TestObjectBlockTypes(constraint.agent, constraint.sx, constraint.sz);
            if ((blockTypes & NavBlockType.Block) != 0)
            {
                return NavQueryStatus.Failed;
            }

            nodePool.Clear();
            openQueue.Clear();

            var snode = nodePool.GetNode(constraint.sx, constraint.sz);
            if (snode == null)
            {
                return NavQueryStatus.Failed;
            }
            snode.gCost = 0;
            snode.fCost = constraint.GetHeuristicCost(this, constraint.sx, constraint.sz);
            snode.parent = null;
            snode.flags |= (int)NavNodeFlags.Open;
            openQueue.Push(snode);

            queryData.lastBestNode = snode;
            queryData.lastBestNodeCost = snode.fCost;
            queryData.status = NavQueryStatus.InProgress;
            return queryData.status;
        }
        public NavQueryStatus UpdateSlicedFindPath(int maxNodes, out int doneNodes)
        {
            doneNodes = 0;
            if (queryData.status != NavQueryStatus.InProgress)
            {
                return queryData.status;
            }
            NavQueryNode bestNode = null;
            while (doneNodes < maxNodes && (bestNode = openQueue.Pop()) != null)
            {
                doneNodes++;
                bestNode.flags &= ~(int)NavNodeFlags.Open;
                bestNode.flags |= (int)NavNodeFlags.Closed;
                if (queryData.constraint.IsGoal(this, bestNode.x, bestNode.z))
                {
                    queryData.lastBestNode = bestNode;
                    queryData.status = NavQueryStatus.Success;
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
        private bool TestNeighborBlocked(NavQueryConstraint constraint, NavQueryNode node, NavDirection dir, ref float lastBestNodeCost, ref NavQueryNode lastBestNode)
        {
            NavMathUtils.GetNeighborXZ(node.x, node.z, dir, out var nx, out var nz);
            if (nx < 0 || nx >= navMap.XSize || nz < 0 || nz >= navMap.ZSize)
            {
                return true;
            }
            var neighborNode = nodePool.GetNode(nx, nz);
            if (neighborNode == null)
            {
                return true;
            }
            if ((neighborNode.flags & (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked)) != 0)
            {
                return (neighborNode.flags & (int)NavNodeFlags.Blocked) != 0;
            }
            if (!constraint.WithinConstraints(this, neighborNode.x, neighborNode.z))
            {
                neighborNode.flags |= (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked);
                return true;
            }
            var blockTypes = blockingObjectMap.TestObjectBlockTypes(constraint.agent, neighborNode.x, neighborNode.z);
            if ((blockTypes & NavBlockType.Block) != 0)
            {
                neighborNode.flags |= (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked);
                return true;
            }
            var speed = NavUtils.GetAgentSquareSpeed(constraint.agent, navMap, neighborNode.x, neighborNode.z, NavMathUtils.DirToVector3(dir));
            if (speed <= 0.0f)
            {
                //neighborNode.flags |= (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked); // 从其他方向的速度可能不为0
                return true;
            }
            var moveParam = constraint.agent.moveParam;
            if (constraint.testMobile && moveParam.isAvoidMobilesOnPath)
            {
                if ((blockTypes & NavBlockType.Busy) != 0)
                {
                    speed *= moveParam.speedModMults[(int)NavSpeedModMultType.Busy];
                }
                else if ((blockTypes & NavBlockType.Idle) != 0)
                {
                    speed *= moveParam.speedModMults[(int)NavSpeedModMultType.Idle];
                }
                else if ((blockTypes & NavBlockType.Moving) != 0)
                {
                    speed *= moveParam.speedModMults[(int)NavSpeedModMultType.Move];
                }
            }
            float dirMoveCost = NavMathUtils.DirCost(dir) * navMap.SquareSize;
            float nodeCost = dirMoveCost / Mathf.Max(1e-4f, speed);
            float gCost = node.gCost + nodeCost;
            float hCost = constraint.GetHeuristicCost(this, neighborNode.x, neighborNode.z);
            float fCost = gCost + hCost;

            if ((neighborNode.flags & (int)NavNodeFlags.Open) != 0)
            {
                if (fCost < neighborNode.fCost)
                {
                    neighborNode.gCost = gCost;
                    neighborNode.fCost = fCost;
                    neighborNode.parent = node;
                    openQueue.Modify(neighborNode);
                }
            }
            else
            {
                neighborNode.gCost = gCost;
                neighborNode.fCost = fCost;
                neighborNode.parent = node;
                neighborNode.flags |= (int)NavNodeFlags.Open;
                openQueue.Push(neighborNode);
                if (hCost < lastBestNodeCost)
                {
                    lastBestNodeCost = hCost;
                    lastBestNode = neighborNode;
                }
            }
            return false;
        }
    }
}