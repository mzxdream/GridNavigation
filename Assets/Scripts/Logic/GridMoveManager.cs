using UnityEngine;

public class GridMoveManager
{
    int gameSpeed = 30;
    public int GameSpeed { get => gameSpeed; }

    public bool Init()
    {
        return true;
    }
    public void Clear()
    {
    }
    public GridMoveAgent AddAgent(Vector3 pos, GridMoveAgentParams agentParams)
    {
        var agent = new GridMoveAgent(this);
        if (!agent.Init(pos, agentParams))
        {
            return null;
        }
        return agent;
    }
    public void RemoveAgent(GridMoveAgent agent)
    {
    }
    public void Update()
    {
    }
}