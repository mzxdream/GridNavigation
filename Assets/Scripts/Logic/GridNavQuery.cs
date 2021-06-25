using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public enum NavQueryStatus { Success = 1, Failed = 2, InProgress = 4, Partial = 8 }

    public class NavQuery
    {
        class QueryData
        {
            public NavQueryStatus status;
            public NavAgent agent;
            public int sx;
            public int sz;
            public Vector3 startPos;
            public int ex;
            public int ez;
            public Vector3 goalPos;
            public int goalRadiusSqr;
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
        public NavMap GetNavMap()
        {
            return navMap;
        }
        public NavBlockingObjectMap GetBlockingObjectMap()
        {
            return blockingObjectMap;
        }
        public NavQueryStatus InitSlicedFindPath(NavAgent agent, Vector3 startPos, Vector3 goalPos, float goalRadius)
        {
            Debug.Assert(agent != null && goalRadius >= 0.0f);

            queryData.status = NavQueryStatus.Failed;
            queryData.agent = agent;
            navMap.ClampInBounds(startPos, out queryData.sx, out queryData.sz, out queryData.startPos);
            navMap.ClampInBounds(goalPos, out queryData.ex, out queryData.ez, out queryData.goalPos);
            queryData.goalRadiusSqr = (int)NavMathUtils.Square(goalRadius / navMap.SquareSize);
            queryData.lastBestNode = null;
            queryData.lastBestNodeCost = 0.0f;

            nodePool.Clear();
            openQueue.Clear();

            var snode = nodePool.GetNode(queryData.sx, queryData.sz);
            if (snode == null)
            {
                return queryData.status;
            }
            snode.gCost = 0;
            snode.fCost = NavUtils.DistanceApproximately(queryData.sx, queryData.sz, queryData.ex, queryData.ez) * navMap.SquareSize;
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
            if ((queryData.status & NavQueryStatus.InProgress) == 0)
            {
                return queryData.status;
            }
            NavQueryNode bestNode = null;
            while (doneNodes < maxNodes && (bestNode = openQueue.Pop()) != null)
            {
                doneNodes++;
                bestNode.flags &= ~(int)NavNodeFlags.Open;
                bestNode.flags |= (int)NavNodeFlags.Closed;

                if (NavMathUtils.Square(bestNode.x - queryData.ex) + NavMathUtils.Square(bestNode.z - queryData.ez) <= queryData.goalRadiusSqr)
                {
                    queryData.lastBestNode = bestNode;
                    queryData.status = NavQueryStatus.Success;
                    return queryData.status;
                }
                var ForwardBlocked = TestNeighborBlocked(bestNode, NavDirection.Forward);
                var BackBlocked = TestNeighborBlocked(bestNode, NavDirection.Back);
                var LeftBlocked = TestNeighborBlocked(bestNode, NavDirection.Left);
                var RightBlocked = TestNeighborBlocked(bestNode, NavDirection.Right);
                if (!LeftBlocked || !ForwardBlocked)
                {
                    TestNeighborBlocked(bestNode, NavDirection.LeftForward);
                }
                if (!RightBlocked || !ForwardBlocked)
                {
                    TestNeighborBlocked(bestNode, NavDirection.RightForward);
                }
                if (!LeftBlocked || !ForwardBlocked)
                {
                    TestNeighborBlocked(bestNode, NavDirection.LeftBack);
                }
                if (!LeftBlocked || !BackBlocked)
                {
                    TestNeighborBlocked(bestNode, NavDirection.RightBack);
                }
            }
            if (openQueue.IsEmpty())
            {
                queryData.status = NavQueryStatus.Success | NavQueryStatus.Partial;
            }
            return queryData.status;
        }
        public NavQueryStatus FinalizeSlicedFindPath(out List<Vector3> path)
        {
            path = new List<Vector3>();
            if ((queryData.status & NavQueryStatus.Failed) != 0)
            {
                return queryData.status;
            }
            var curNode = queryData.lastBestNode;
            if (curNode == null)
            {
                queryData.status = NavQueryStatus.Failed;
                return queryData.status;
            }
            do
            {
                path.Add(navMap.GetSquarePos(curNode.x, curNode.z));
                curNode = curNode.parent;
            } while (curNode != null);
            return queryData.status;
        }
        private bool TestNeighborBlocked(NavQueryNode node, NavDirection dir)
        {
            NavUtils.GetNeighborXZ(node.x, node.z, dir, out var nx, out var nz);
            if (nx < 0 || nx >= navMap.XSize || nz < 0 || nz >= navMap.ZSize)
            {
                return true;
            }
            var neighborNode = nodePool.GetNode(nx, nz);
            if (neighborNode == null)
            {
                return true;
            }
            var agent = queryData.agent;
            if ((neighborNode.flags & (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked)) != 0)
            {
                return (neighborNode.flags & (int)NavNodeFlags.Blocked) != 0;
            }
            if (!NavUtils.TestMoveSquare(navMap, agent, neighborNode.x, neighborNode.z))
            {
                neighborNode.flags |= (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked);
                return true;
            }
            var blockTypes = NavUtils.TestBlockTypesSquare(blockingObjectMap, agent, neighborNode.x, neighborNode.z);
            var speedMult = 1.0f;
            {
                if ((blockTypes & NavBlockType.Idle) != 0)
                {
                    speedMult = Mathf.Min(speedMult, agent.moveDef.GetSpeedModMult(NavSpeedModMultType.Idle));
                }
                if ((blockTypes & NavBlockType.Busy) != 0)
                {
                    speedMult = Mathf.Min(speedMult, agent.moveDef.GetSpeedModMult(NavSpeedModMultType.Busy));
                }
                if ((blockTypes & NavBlockType.Moving) != 0)
                {
                    speedMult = Mathf.Min(speedMult, agent.moveDef.GetSpeedModMult(NavSpeedModMultType.Moving));
                }
                if ((blockTypes & NavBlockType.Blocked) != 0)
                {
                    speedMult = Mathf.Min(speedMult, agent.moveDef.GetSpeedModMult(NavSpeedModMultType.Blocked));
                }
            }
            var speed = NavUtils.GetSquareSpeed(navMap, agent, neighborNode.x, neighborNode.z, dir) * speedMult;
            float nodeCost = NavUtils.DirDistanceApproximately(dir) * navMap.SquareSize / Mathf.Max(NavMathUtils.EPSILON, speed);
            float gCost = node.gCost + nodeCost;
            float hCost = NavUtils.DistanceApproximately(neighborNode.x, neighborNode.z, queryData.ex, queryData.ez) * navMap.SquareSize;
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
                if (hCost < queryData.lastBestNodeCost)
                {
                    queryData.lastBestNodeCost = hCost;
                    queryData.lastBestNode = neighborNode;
                }
            }
            return false;
        }
    }
}