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

            if (NavUtils.IsSquareBlocked(navMap, blockingObjectMap, agent, queryData.sx, queryData.sz))
            {
                return queryData.status;
            }

            nodePool.Clear();
            openQueue.Clear();

            var snode = nodePool.GetNode(queryData.sx, queryData.sz);
            if (snode == null)
            {
                return queryData.status;
            }
            snode.gCost = 0;
            snode.fCost = GetHeuristicCost(queryData.sx, queryData.sz, queryData.ex, queryData.ez);
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
        public bool FindCorners(NavAgent agent, Vector3 startPos, Vector3 goalPos, int maxNodes, out List<Vector3> corners)
        {
            Debug.Assert(agent != null);

            corners = new List<Vector3>();
            if (IsStraightWalkable(agent, startPos, goalPos, false, out _, out _))
            {
                corners.Add(goalPos);
                return true;
            }
            InitSlicedFindPath(agent, startPos, goalPos, 0.0f);
            var status = UpdateSlicedFindPath(maxNodes, out _);
            if ((status & NavQueryStatus.Success) == 0 || (status & NavQueryStatus.Partial) != 0)
            {
                return false;
            }
            var curNode = queryData.lastBestNode;
            Debug.Assert(curNode != null);
            var nodes = new List<NavQueryNode>();
            do
            {
                nodes.Add(curNode);
                curNode = curNode.parent;
            } while (curNode != null);
            Debug.Assert(nodes.Count >= 2);
            //去除多余的点
            corners.Add(goalPos);
            var oldDirX = nodes[1].x - nodes[0].x;
            var oldDirZ = nodes[1].z - nodes[0].z;
            for (int i = 2; i < nodes.Count; i++)
            {
                var newDirX = nodes[i].x - nodes[i - 1].x;
                var newDirZ = nodes[i].z - nodes[i - 1].z;
                if (newDirX != oldDirX || newDirZ != oldDirZ)
                {
                    oldDirX = newDirX;
                    oldDirZ = newDirZ;
                    corners.Add(navMap.GetSquarePos(nodes[i - 1].x, nodes[i - 1].z));
                }
            }
            corners.Add(startPos);
            //去除可以直达的拐点
            for (int i = corners.Count - 1; i > 1; i--)
            {
                for (int j = 0; j < i - 1; j++)
                {
                    if (IsStraightWalkable(agent, corners[j], corners[i], false, out _, out _))
                    {
                        for (int k = i - 1; k > j; k--)
                        {
                            corners.RemoveAt(k);
                        }
                        i = j + 1;
                        break;
                    }
                }
            }
            corners.RemoveAt(corners.Count - 1);
            return true;
        }
        public bool FindNearestSquare(NavAgent agent, Vector3 pos, float radius, out int nearestIndex, out Vector3 nearestPos)
        {
            Debug.Assert(agent != null && radius > 0);

            navMap.ClampInBounds(pos, out var x, out var z, out nearestPos);
            nearestIndex = NavUtils.SquareIndex(x, z);
            if (!NavUtils.IsSquareBlocked(navMap, blockingObjectMap, agent, x, z, true))
            {
                return true;
            }
            var ext = (int)(radius / navMap.SquareSize);
            for (int k = 1; k <= ext; k++)
            {
                int xmin = x - k;
                int xmax = x + k;
                int zmin = z - k;
                int zmax = z + k;
                if (!TestBlocked(agent, x, zmax, ref nearestIndex, ref nearestPos) // forward
                    || !TestBlocked(agent, x, zmin, ref nearestIndex, ref nearestPos) // back
                    || !TestBlocked(agent, xmin, z, ref nearestIndex, ref nearestPos) // left
                    || !TestBlocked(agent, xmax, z, ref nearestIndex, ref nearestPos)) // right
                {
                    return true;
                }
                for (int t = 1; t < k; t++)
                {
                    if (!TestBlocked(agent, xmin, z + t, ref nearestIndex, ref nearestPos) // left [forward] 
                        || !TestBlocked(agent, xmin, z - t, ref nearestIndex, ref nearestPos) // left [back]
                        || !TestBlocked(agent, xmax, z + t, ref nearestIndex, ref nearestPos) // right [forward]
                        || !TestBlocked(agent, xmax, z - t, ref nearestIndex, ref nearestPos) // right [back]
                        || !TestBlocked(agent, x - t, zmax, ref nearestIndex, ref nearestPos) // [left] forward
                        || !TestBlocked(agent, x + t, zmax, ref nearestIndex, ref nearestPos) // [right] forwad
                        || !TestBlocked(agent, x - t, zmin, ref nearestIndex, ref nearestPos) // [left] back
                        || !TestBlocked(agent, x + t, zmin, ref nearestIndex, ref nearestPos)) // [right] back
                    {
                        return true;
                    }
                }
                if (!TestBlocked(agent, xmin, zmax, ref nearestIndex, ref nearestPos) // left forward
                    || !TestBlocked(agent, xmax, zmax, ref nearestIndex, ref nearestPos) // right forward 
                    || !TestBlocked(agent, xmin, zmin, ref nearestIndex, ref nearestPos) // left back
                    || !TestBlocked(agent, xmax, zmin, ref nearestIndex, ref nearestPos)) // right back
                {
                    return true;
                }
            }
            return false;
        }
        public void Raycast(NavAgent agent, Vector3 startPos, Vector3 endPos, out float t)
        {
            Debug.Assert(agent != null);
            t = 1.0f;
            if (IsStraightWalkable(agent, startPos, endPos, true, out var x, out var z))
            {
                return;
            }
            var pos = navMap.GetSquarePos(x, z);
            var radius = agent.param.radius + navMap.SquareSize * NavMathUtils.HALF_SQRT2;
            NavMathUtils.IsSegmentCircleIntersection(pos, radius, startPos, endPos, out t);
        }
        private bool IsStraightWalkable(NavAgent agent, Vector3 startPos, Vector3 endPos, bool isExcludeMoving, out int x, out int z)
        {
            Debug.Assert(agent != null);

            //startPos = new Vector3(16.00237f, 0, -3.808542f);
            //endPos = new Vector3(15.97629f, 0, 0.09902147f);

            navMap.GetSquareXZ(startPos, out var sx, out var sz);
            navMap.GetSquareXZ(endPos, out var ex, out var ez);

            int signX = ex > sx ? 1 : -1, signZ = ez > sz ? 1 : -1;
            int nx = Mathf.Abs(ex - sx), nz = Mathf.Abs(ez - sz);
            float dx = Mathf.Abs(endPos.x - startPos.x), dz = Mathf.Abs(endPos.z - startPos.z);
            x = sx;
            z = sz;
            if (NavUtils.IsSquareBlocked(navMap, blockingObjectMap, agent, x, z, isExcludeMoving))
            {
                return false;
            }
            for (int ix = 0, iz = 0; ix < nx && iz < nz;)
            {
                var tx = dz * (ix + 0.5f);
                var tz = dx * (iz + 0.5f);
                if (tx < tz)
                {
                    ix++;
                    x += signX;
                }
                else
                {
                    iz++;
                    z += signZ;
                }
                if (NavUtils.IsSquareBlocked(navMap, blockingObjectMap, agent, x, z, isExcludeMoving))
                {
                    return false;
                }
            }
            while (x != ex)
            {
                x += signX;
                if (NavUtils.IsSquareBlocked(navMap, blockingObjectMap, agent, x, z, isExcludeMoving))
                {
                    return false;
                }
            }
            while (z != ez)
            {
                z += signZ;
                if (NavUtils.IsSquareBlocked(navMap, blockingObjectMap, agent, x, z, isExcludeMoving))
                {
                    return false;
                }
            }
            return true;
        }
        private bool TestBlocked(NavAgent agent, int x, int z, ref int index, ref Vector3 pos)
        {
            if (x < 0 || x >= navMap.XSize || z < 0 || z >= navMap.ZSize)
            {
                return true;
            }
            if (!NavUtils.IsSquareBlocked(navMap, blockingObjectMap, agent, x, z, true))
            {
                index = NavUtils.SquareIndex(x, z);
                pos = navMap.GetSquarePos(x, z);
                return false;
            }
            return true;
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
            if ((blockTypes & NavBlockType.Block) != 0)
            {
                neighborNode.flags |= (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked);
                return true;
            }
            var speed = NavUtils.GetSquareSpeed(navMap, agent, neighborNode.x, neighborNode.z, NavUtils.DirToVector3(dir));
            if (agent.moveParam.isAvoidMobilesOnPath)
            {
                if ((blockTypes & NavBlockType.Busy) != 0)
                {
                    speed *= agent.moveParam.speedModMults[(int)NavSpeedModMultType.Busy];
                }
                else if ((blockTypes & NavBlockType.Idle) != 0)
                {
                    speed *= agent.moveParam.speedModMults[(int)NavSpeedModMultType.Idle];
                }
                else if ((blockTypes & NavBlockType.Moving) != 0)
                {
                    speed *= agent.moveParam.speedModMults[(int)NavSpeedModMultType.Move];
                }
            }
            float dirMoveCost = NavUtils.DirDistanceApproximately(dir) * navMap.SquareSize;
            float nodeCost = dirMoveCost / Mathf.Max(NavMathUtils.EPSILON, speed);
            float gCost = node.gCost + nodeCost;
            float hCost = GetHeuristicCost(neighborNode.x, neighborNode.z, queryData.ex, queryData.ez);
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
        private float GetHeuristicCost(int sx, int sz, int ex, int ez)
        {
            return NavUtils.DistanceApproximately(sx, sz, ex, ez) * navMap.SquareSize;
        }
    }
}