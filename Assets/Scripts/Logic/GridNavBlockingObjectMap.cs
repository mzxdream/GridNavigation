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
            int xmin = agent.mapPos.x;
            int zmin = agent.mapPos.y;
            int xmax = Mathf.Min(xsize - 1, xmin + agent.moveParam.unitSize);
            int zmax = Mathf.Min(zsize - 1, zmin + agent.moveParam.unitSize);
            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    var index = x + z * xsize;
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
            int xmin = agent.mapPos.x;
            int zmin = agent.mapPos.y;
            int xmax = Mathf.Min(xsize - 1, xmin + agent.moveParam.unitSize);
            int zmax = Mathf.Min(zsize - 1, zmin + agent.moveParam.unitSize);
            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    var index = x + z * xsize;
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
        public bool GetSquareAgents(int x, int z, out List<NavAgent> agentList)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            var index = x + z * xsize;
            return agents.TryGetValue(index, out agentList);
        }
    }
}