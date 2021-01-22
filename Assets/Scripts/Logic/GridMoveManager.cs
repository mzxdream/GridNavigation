using UnityEngine;

public class GridMoveManager
{
    int gameSpeed = 30;
    public int GameSpeed { get => gameSpeed; }
    float squareSize = 8.0f;
    public float SquareSize { get => squareSize; }

    public bool Init()
    {
        return true;
    }
    public void Clear()
    {
    }
    public GridMoveAgent AddAgent(Vector3 pos, Vector3 forward, GridMoveAgentParams agentParams)
    {
        var agent = new GridMoveAgent(this);
        if (!agent.Init(pos, forward, agentParams))
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