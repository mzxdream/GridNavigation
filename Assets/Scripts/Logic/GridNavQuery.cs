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

        const float H_SCALE = 0.999f;
        private NavManager navManager;
        private NavQueryNodePool nodePool;
        private NavQueryPriorityQueue openQueue;
        private QueryData queryData;

        public bool Init(NavManager navManager, int maxNodes = 8192)
        {
            Debug.Assert(navManager != null && maxNodes > 0);
            this.navManager = navManager;
            this.nodePool = new NavQueryNodePool(maxNodes);
            this.openQueue = new NavQueryPriorityQueue(maxNodes);
            this.queryData = new QueryData();
            return true;
        }
        public NavQueryStatus InitSlicedFindPath(NavAgent agent, Vector3 startPos, Vector3 goalPos, float goalRadius)
        {
            Debug.Assert(navManager != null && agent != null && goalRadius >= 0.0f);

            var navMap = navManager.GetNavMap();
            queryData.status = NavQueryStatus.Failed;
            queryData.agent = agent;
            navMap.ClampInBounds(startPos, out queryData.sx, out queryData.sz, out queryData.startPos);
            navMap.ClampInBounds(goalPos, out queryData.ex, out queryData.ez, out queryData.goalPos);
            queryData.goalRadiusSqr = NavMathUtils.Square(Mathf.Max(2, (int)(goalRadius / navMap.SquareSize)));
            queryData.lastBestNode = null;
            queryData.lastBestNodeCost = 0.0f;

            nodePool.Clear();
            openQueue.Clear();

            var snode = nodePool.GetNode(queryData.sx, queryData.sz);
            if (snode == null)
            {
                return queryData.status;
            }
            var hCost = NavMathUtils.OctileDistance(queryData.sx, queryData.sz, queryData.ex, queryData.ez) * navMap.SquareSize * H_SCALE;
            snode.gCost = 0;
            snode.fCost = hCost;
            snode.parent = null;
            snode.flags |= (int)NavNodeFlags.Open;
            openQueue.Push(snode);

            queryData.lastBestNode = snode;
            queryData.lastBestNodeCost = hCost;
            queryData.status = NavQueryStatus.InProgress;
            return queryData.status;
        }
        public NavQueryStatus UpdateSlicedFindPath(int maxNodes, out int doneNodes)
        {
            Debug.Assert(navManager != null);

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

                if (NavMathUtils.SqrDistance(bestNode.x, bestNode.z, queryData.ex, queryData.ez) <= queryData.goalRadiusSqr)
                {
                    queryData.lastBestNode = bestNode;
                    queryData.status = NavQueryStatus.Success;
                    return queryData.status;
                }
                var ForwardBlocked = TestNeighborBlocked(bestNode, NavDirection.Forward);
                var BackBlocked = TestNeighborBlocked(bestNode, NavDirection.Back);
                var LeftBlocked = TestNeighborBlocked(bestNode, NavDirection.Left);
                var RightBlocked = TestNeighborBlocked(bestNode, NavDirection.Right);
                if (!LeftBlocked && !ForwardBlocked)
                {
                    TestNeighborBlocked(bestNode, NavDirection.LeftForward);
                }
                if (!RightBlocked && !ForwardBlocked)
                {
                    TestNeighborBlocked(bestNode, NavDirection.RightForward);
                }
                if (!LeftBlocked && !ForwardBlocked)
                {
                    TestNeighborBlocked(bestNode, NavDirection.LeftBack);
                }
                if (!LeftBlocked && !BackBlocked)
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
            Debug.Assert(navManager != null);

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
            var navMap = navManager.GetNavMap();
            do
            {
                path.Add(navMap.GetSquarePos(curNode.x, curNode.z));
                curNode = curNode.parent;
            } while (curNode != null);
            path[path.Count - 1] = queryData.startPos;
            if ((queryData.status & NavQueryStatus.Partial) == 0)
            {
                path[0] = queryData.goalPos;
            }
            return queryData.status;
        }
        public NavQueryStatus FinalizeSlicedFindPathWithSimpleSmooth(out List<Vector3> path)
        {
            Debug.Assert(navManager != null);

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
            var navMap = navManager.GetNavMap();
            var pprevNode = curNode;
            var prevNode = curNode;
            do
            {
                path.Add(navMap.GetSquarePos(curNode.x, curNode.z));
                AdjustFoundPath(ref path, pprevNode, prevNode, curNode);
                pprevNode = prevNode;
                prevNode = curNode;
                curNode = curNode.parent;
            } while (curNode != null);
            path[path.Count - 1] = queryData.startPos;
            if ((queryData.status & NavQueryStatus.Partial) == 0)
            {
                path[0] = queryData.goalPos;
            }
            return queryData.status;
        }
        private bool TestNeighborBlocked(NavQueryNode node, NavDirection dir)
        {
            var navMap = navManager.GetNavMap();
            NavUtils.GetNeighborXZ(node.x, node.z, dir, 2, out var nx, out var nz);
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
            var blockTypes = NavUtils.TestBlockTypesSquare(navManager, agent, neighborNode.x, neighborNode.z);
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
            var speed = NavUtils.GetSquareSpeed(navMap, agent, neighborNode.x, neighborNode.z) * speedMult;
            float nodeCost = NavUtils.DirDistanceApproximately(dir) * 2.0f * navMap.SquareSize / Mathf.Max(NavMathUtils.EPSILON, speed);
            float gCost = node.gCost + nodeCost;
            float hCost = NavMathUtils.OctileDistance(neighborNode.x, neighborNode.z, queryData.ex, queryData.ez) * navMap.SquareSize * H_SCALE;
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
        private void AdjustFoundPath(ref List<Vector3> path, NavQueryNode pprevNode, NavQueryNode prevNode, NavQueryNode node)
        {
            if (pprevNode == prevNode || prevNode == node)
            {
                return;
            }
            {
                // check turn left or turn right
                var prevDirX = prevNode.x - pprevNode.x;
                var prevDirZ = prevNode.z - pprevNode.z;
                var dirX = node.x - prevNode.x;
                var dirZ = node.z - prevNode.z;
                if (prevDirX == dirX)
                {
                    if (Mathf.Abs(prevDirZ + dirZ) != 2)
                    {
                        return;
                    }
                }
                else if (prevDirZ == dirZ)
                {
                    if (Mathf.Abs(prevDirX + dirX) != 2)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            var testNode = nodePool.FindNode(node.x + (pprevNode.x - prevNode.x), node.z + (pprevNode.z - prevNode.z));
            if (testNode == null)
            {
                return;
            }
            if ((testNode.flags & (int)NavNodeFlags.Blocked) != 0)
            {
                return;
            }
            const float CostMod = 1.39f; //(math::sqrt(2) + 1) / math::sqrt(3)
            if (testNode.fCost > CostMod * prevNode.fCost)
            {
                return;
            }
            path[path.Count - 2] = navManager.GetNavMap().ClampInBounds((path[path.Count - 1] + path[path.Count - 3]) / 2);
        }
    }
}