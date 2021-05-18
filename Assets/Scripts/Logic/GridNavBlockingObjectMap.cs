using System.Collections.Generic;
using UnityEngine;

public class GridNavBlockingObjectMap
{
    private Dictionary<int, List<GridNavAgent>> agents;

    public void AddAgent(GridNavAgent agent)
    {
        int halfUnitSize = agent.unitSize >> 1;
        GridNavMath.SquareXZ(agent.squareIndex, out var x, out var z);
        int xmin = Mathf.Max(0, x - halfUnitSize);
        int zmin = Mathf.Max(0, z - halfUnitSize);
        int xmax = Mathf.Min(0xFFFF, x + halfUnitSize);
        int zmax = Mathf.Min(0xFFFF, z + halfUnitSize);
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                var index = GridNavMath.SquareIndex(tx, tz);
                if (!agents.TryGetValue(index, out var agentList))
                {
                    agentList = new List<GridNavAgent>();
                    agents.Add(index, agentList);
                }
                agentList.Add(agent);
            }
        }
    }
    public void RemoveSquareAgent(GridNavAgent agent)
    {
        int halfUnitSize = agent.unitSize >> 1;
        GridNavMath.SquareXZ(agent.squareIndex, out var x, out var z);
        int xmin = Mathf.Max(0, x - halfUnitSize);
        int zmin = Mathf.Max(0, z - halfUnitSize);
        int xmax = Mathf.Min(0xFFFF, x + halfUnitSize);
        int zmax = Mathf.Min(0xFFFF, z + halfUnitSize);
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                var index = GridNavMath.SquareIndex(tx, tz);
                if (agents.TryGetValue(index, out var agentList))
                {
                    agentList.Remove(agent);
                    if (agentList.Count == 0)
                    {
                        agents.Remove(index);
                    }
                }
            }
        }
    }
    public GridNavBlockType TestBlockTypes(GridNavAgent agent, int x, int z)
    {
        return GridNavBlockType.None;
    }
}