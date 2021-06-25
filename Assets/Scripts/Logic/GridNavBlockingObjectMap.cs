using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public class NavBlockingObjectMap
    {
        private int xsize;
        private int zsize;
        private List<NavAgent>[] suqareAgents;

        public NavBlockingObjectMap(int xsize, int zsize)
        {
            Debug.Assert(xsize > 0 && zsize > 0);

            this.xsize = xsize;
            this.zsize = zsize;
            this.suqareAgents = new List<NavAgent>[xsize * zsize];
            for (int z = 0; z < zsize; z++)
            {
                for (int x = 0; x < xsize; x++)
                {
                    this.suqareAgents[x + z * xsize] = new List<NavAgent>();
                }
            }
        }
        public void AddAgent(NavAgent agent)
        {
            int unitSize = agent.moveDef.GetUnitSize();
            int xmin = agent.mapPos.x;
            int zmin = agent.mapPos.y;
            int xmax = Mathf.Min(xsize - 1, xmin + unitSize);
            int zmax = Mathf.Min(zsize - 1, zmin + unitSize);
            for (int z = zmin; z < zmax; z++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    this.suqareAgents[x + z * xsize].Add(agent);
                }
            }
        }
        public void RemoveAgent(NavAgent agent)
        {
            int unitSize = agent.moveDef.GetUnitSize();
            int xmin = agent.mapPos.x;
            int zmin = agent.mapPos.y;
            int xmax = Mathf.Min(xsize - 1, xmin + unitSize);
            int zmax = Mathf.Min(zsize - 1, zmin + unitSize);
            for (int z = zmin; z < zmax; z++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    var index = x + z * xsize;
                    this.suqareAgents[x + z * xsize].Remove(agent);
                }
            }
        }
        public List<NavAgent> GetSquareAgents(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            return this.suqareAgents[x + z * xsize];
        }
    }
}