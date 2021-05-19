using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public class NavBlockingObjectMap
    {
        private int xsize;
        private int zsize;
        private Dictionary<int, List<NavAgent>> agents;

        public NavBlockingObjectMap(int xsize, int zsize)
        {
            this.xsize = xsize;
            this.zsize = zsize;
            this.agents = new Dictionary<int, List<NavAgent>>();
        }

        public void AddAgent(NavAgent agent)
        {
            NavDef.SquareXZ(agent.squareIndex, out var x, out var z);
            int xmin = Mathf.Max(0, x - agent.halfUnitSize);
            int xmax = Mathf.Min(xsize - 1, x + agent.halfUnitSize);
            int zmin = Mathf.Max(0, z - agent.halfUnitSize);
            int zmax = Mathf.Min(zsize - 1, z + agent.halfUnitSize);
            for (int tz = zmin; tz <= zmax; tz++)
            {
                for (int tx = xmin; tx <= xmax; tx++)
                {
                    var index = NavDef.SquareIndex(tx, tz);
                    if (!agents.TryGetValue(index, out var agentList))
                    {
                        agentList = new List<NavAgent>();
                        agents.Add(index, agentList);
                    }
                    agentList.Add(agent);
                }
            }
        }
        public void RemoveAgent(NavAgent agent)
        {
            NavDef.SquareXZ(agent.squareIndex, out var x, out var z);
            int xmin = Mathf.Max(0, x - agent.halfUnitSize);
            int xmax = Mathf.Min(xsize - 1, x + agent.halfUnitSize);
            int zmin = Mathf.Max(0, z - agent.halfUnitSize);
            int zmax = Mathf.Min(zsize - 1, z + agent.halfUnitSize);
            for (int tz = zmin; tz <= zmax; tz++)
            {
                for (int tx = xmin; tx <= xmax; tx++)
                {
                    var index = NavDef.SquareIndex(tx, tz);
                    if (!agents.TryGetValue(index, out var agentList))
                    {
                        continue;
                    }
                    agentList.Remove(agent);
                    if (agentList.Count == 0)
                    {
                        agents.Remove(index);
                    }
                }
            }
        }
        public NavBlockType ObjectBlockType(NavAgent collider, NavAgent collidee)
        {
            if (collider == collidee)
            {
                return NavBlockType.None;
            }
            if (collidee.isMoving)
            {
                return NavBlockType.Moving;
            }
            if (collidee.param.isPushResistant)
            {
                return NavBlockType.Block;
            }
            if (collidee.moveState != NavMoveState.Idle)
            {
                return NavBlockType.Busy;
            }
            return NavBlockType.Idle;
        }
        public NavBlockType TestObjectBlockTypes(NavAgent agent, int x, int z)
        {
            var blockTypes = NavBlockType.None;
            int xmin = Mathf.Max(0, x - agent.halfUnitSize);
            int xmax = Mathf.Min(xsize - 1, x + agent.halfUnitSize);
            int zmin = Mathf.Max(0, z - agent.halfUnitSize);
            int zmax = Mathf.Min(zsize - 1, z + agent.halfUnitSize);
            for (int tz = zmin; tz <= zmax; tz++)
            {
                for (int tx = xmin; tx <= xmax; tx++)
                {
                    var index = NavDef.SquareIndex(tx, tz);
                    if (!agents.TryGetValue(index, out var agentList))
                    {
                        continue;
                    }
                    foreach (var collidee in agentList)
                    {
                        blockTypes |= ObjectBlockType(agent, collidee);
                        if ((blockTypes & NavBlockType.Block) != 0)
                        {
                            return blockTypes;
                        }
                    }
                }
            }
            return blockTypes;
        }
    }
}