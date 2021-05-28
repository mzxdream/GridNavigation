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

        public NavQueryConstraint(NavAgent agent, int startIndex, Vector3 startPos, int goalIndex, Vector3 goalPos, float goalRadius)
        {
            this.agent = agent;
            NavUtils.SquareXZ(startIndex, out sx, out sz);
            this.startPos = startPos;
            NavUtils.SquareXZ(goalIndex, out ex, out ez);
            this.goalPos = goalPos;
            this.goalRadius = goalRadius;
        }
        public float GetHeuristicCost(NavMap navMap, int x, int z)
        {
            return NavMathUtils.DistanceApproximately(x, z, ex, ez) * navMap.SquareSize;
        }
        public bool IsGoal(NavMap navMap, int x, int z)
        {
            if (x == ex && z == ez)
            {
                return true;
            }
            return NavMathUtils.DistanceApproximately(x, z, ex, ez) * navMap.SquareSize <= goalRadius;
        }
        public virtual bool WithinConstraints(NavMap navMap, int x, int z)
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

            if (!NavUtils.TestMoveSquareRange(navMap, constraint.agent, constraint.sx, constraint.sz)
                || (NavUtils.TestBlockTypesSquareRange(blockingObjectMap, constraint.agent, constraint.sx, constraint.sz) & NavBlockType.Block) != 0)
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
            snode.fCost = constraint.GetHeuristicCost(navMap, constraint.sx, constraint.sz);
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
                if (queryData.constraint.IsGoal(navMap, bestNode.x, bestNode.z))
                {
                    queryData.lastBestNode = bestNode;
                    queryData.status = NavQueryStatus.Success;
                    return queryData.status;
                }
                var ForwardBlocked = TestNeighborBlocked(queryData.constraint, bestNode, NavDirection.Forward, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                var BackBlocked = TestNeighborBlocked(queryData.constraint, bestNode, NavDirection.Back, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                var LeftBlocked = TestNeighborBlocked(queryData.constraint, bestNode, NavDirection.Left, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                var RightBlocked = TestNeighborBlocked(queryData.constraint, bestNode, NavDirection.Right, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                if (!LeftBlocked && !ForwardBlocked)
                {
                    TestNeighborBlocked(queryData.constraint, bestNode, NavDirection.LeftForward, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                }
                if (!RightBlocked && !ForwardBlocked)
                {
                    TestNeighborBlocked(queryData.constraint, bestNode, NavDirection.RightForward, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                }
                if (!LeftBlocked && !ForwardBlocked)
                {
                    TestNeighborBlocked(queryData.constraint, bestNode, NavDirection.LeftBack, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                }
                if (!LeftBlocked && !BackBlocked)
                {
                    TestNeighborBlocked(queryData.constraint, bestNode, NavDirection.RightBack, ref queryData.lastBestNodeCost, ref queryData.lastBestNode);
                }
            }
            if (openQueue.IsEmpty())
            {
                queryData.status = NavQueryStatus.Success | NavQueryStatus.Partial;
            }
            return queryData.status;
        }
        public NavQueryStatus FinalizeSlicedFindPath(out List<int> path)
        {
            path = new List<int>();
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
                path.Add(NavUtils.SquareIndex(curNode.x, curNode.z));
                curNode = curNode.parent;
            } while (curNode != null);
            path.Reverse();
            return queryData.status;
        }
        public bool FindCorners(NavAgent agent, int startIndex, Vector3 startPos, int goalIndex, Vector3 goalPos, int ncorner, int maxNodes, out List<Vector3> cornerVerts)
        {
            Debug.Assert(agent != null);
            cornerVerts = new List<Vector3>();

            if (IsStraightWalkable(agent, startPos, goalPos))
            {
                cornerVerts.Add(goalPos);
                return true;
            }

            var constraint = new NavQueryConstraint(agent, startIndex, startPos, goalIndex, goalPos, NavMathUtils.EPSILON);
            InitSlicedFindPath(constraint);
            var status = UpdateSlicedFindPath(maxNodes, out _);
            if ((status & NavQueryStatus.Success) == 0 || (status & NavQueryStatus.Partial) != 0)
            {
                return false;
            }
            FinalizeSlicedFindPath(out var path);
            //
            var straightPath = new List<int>();
            straightPath.Add(path[0]);
            var oldDir = path[1] - path[0];
            for (int i = 2; i < path.Count; i++)
            {
                var newDir = path[i] - path[i - 1];
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
                var epos = straightPath[i] == goalIndex ? goalPos : navMap.GetSquarePos(straightPath[i]);
                for (int j = 0; j < i - 1; j++)
                {
                    var spos = straightPath[j] == startIndex ? startPos : navMap.GetSquarePos(straightPath[j]);
                    if (IsStraightWalkable(agent, spos, epos))
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
            for (int i = 1; i < ncorner && i < straightPath.Count; i++)
            {
                cornerVerts.Add(straightPath[i] == goalIndex ? goalPos : navMap.GetSquarePos(straightPath[i]));
            }
            return true;
        }
        public bool FindNearestSquare(NavAgent agent, Vector3 pos, float radius, out int nearestIndex, out Vector3 nearestPos)
        {
            Debug.Assert(agent != null && radius > 0);

            navMap.ClampInBounds(pos, out var x, out var z, out nearestPos);
            nearestIndex = NavUtils.SquareIndex(x, z);
            if (!NavUtils.IsBlockedRange(navMap, blockingObjectMap, agent, x, z))
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
        private bool IsStraightWalkable(NavAgent agent, Vector3 startPos, Vector3 endPos)
        {
            Debug.Assert(agent != null);

            //startPos = new Vector3(16.00237f, 0, -3.808542f);
            //endPos = new Vector3(15.97629f, 0, 0.09902147f);

            navMap.GetSquareXZ(startPos, out var sx, out var sz);
            navMap.GetSquareXZ(endPos, out var ex, out var ez);

            int signX = ex > sx ? 1 : -1, signZ = ez > sz ? 1 : -1;
            int nx = Mathf.Abs(ex - sx), nz = Mathf.Abs(ez - sz);
            float dx = Mathf.Abs(endPos.x - startPos.x), dz = Mathf.Abs(endPos.z - startPos.z);
            int x = sx, z = sz;
            if (NavUtils.IsBlockedRange(navMap, blockingObjectMap, agent, x, z))
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
                if (NavUtils.IsBlockedRange(navMap, blockingObjectMap, agent, x, z))
                {
                    return false;
                }
            }
            while (x != ex)
            {
                x += signX;
                if (NavUtils.IsBlockedRange(navMap, blockingObjectMap, agent, x, z))
                {
                    return false;
                }
            }
            while (z != ez)
            {
                z += signZ;
                if (NavUtils.IsBlockedRange(navMap, blockingObjectMap, agent, x, z))
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
            if (!NavUtils.IsBlockedRange(navMap, blockingObjectMap, agent, x, z))
            {
                index = NavUtils.SquareIndex(x, z);
                pos = navMap.GetSquarePos(x, z);
                return false;
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
            if (!constraint.WithinConstraints(navMap, neighborNode.x, neighborNode.z))
            {
                neighborNode.flags |= (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked);
                return true;
            }
            var agent = constraint.agent;
            if (!NavUtils.TestMoveSquareRange(navMap, agent, neighborNode.x, neighborNode.z))
            {
                neighborNode.flags |= (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked);
                return true;
            }
            var blockTypes = NavUtils.TestBlockTypesSquareRange(blockingObjectMap, agent, neighborNode.x, neighborNode.z);
            if ((blockTypes & NavBlockType.Block) != 0)
            {
                neighborNode.flags |= (int)(NavNodeFlags.Closed | NavNodeFlags.Blocked);
                return true;
            }
            var speed = NavUtils.GetSquareSpeed(navMap, agent, neighborNode.x, neighborNode.z, NavMathUtils.DirToVector3(dir));
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
            float dirMoveCost = NavMathUtils.DirCost(dir) * navMap.SquareSize;
            float nodeCost = dirMoveCost / Mathf.Max(1e-4f, speed);
            float gCost = node.gCost + nodeCost;
            float hCost = constraint.GetHeuristicCost(navMap, neighborNode.x, neighborNode.z);
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