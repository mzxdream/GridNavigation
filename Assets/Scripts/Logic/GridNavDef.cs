
public class GridNavMoveDef
{
    public int unitSize = 1;
    public float maxSlope = 1.0f;
    public float slopeMod = 0.0f;
    public float[] speedModMults = new float[] { 0.10f, 1.0f, 1.0f, 1.0f }; //对应上面的代理状态
    public bool avoidMobilesOnPath = true;
}

public enum GridNavAgentMoveState { Idle = 0, Requesting = 1, WaitForPath = 2, Moving = 3 }