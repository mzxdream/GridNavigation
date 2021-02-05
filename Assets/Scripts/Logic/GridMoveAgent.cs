using UnityEngine;

public class GridMoveAgentParams
{
    public int teamID; //enemy can't push
    public int unitSize;
    public float mass; //calc push distance
    public float maxSpeed;
    public bool isPushResistant;
}

public class GridMoveAgent
{
    private int unitSize = 3; //odd number and >= 3
    private Vector3 pos = Vector3.zero;

    public GridMoveAgent()
    {
    }

    public void Update()
    {
    }
    
    public void LateUpdate()
    {
    }
}