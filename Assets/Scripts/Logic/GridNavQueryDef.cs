using System;

public interface IGridNavQueryFilter
{
    bool IsBlocked(GridNavMesh navMesh, int index);
    float GetCost(GridNavMesh navMesh, int index, GridNavDirection dir); //小于0表示无法通过
}

public interface IGridNavQueryConstraint
{
    bool IsGoal(GridNavMesh navMesh, int index);
    bool WithinConstraints(GridNavMesh navMesh, int index);
    float GetHeuristicCost(GridNavMesh navMesh, int index);
}

public class GridNavQueryFilterDef : IGridNavQueryFilter
{
    public bool IsBlocked(GridNavMesh navMesh, int index)
    {
        return navMesh.IsSquareBlocked(index);
    }
    public float GetCost(GridNavMesh navMesh, int index, GridNavDirection dir)
    {
        return navMesh.GetSquareCost(index, dir);
    }
}

public class GridNavQueryFilterExtraBlockedCheck : IGridNavQueryFilter
{
    private Func<int, bool> extraBlockedFunc;

    public GridNavQueryFilterExtraBlockedCheck(Func<int, bool> extraBlockedFunc)
    {
        this.extraBlockedFunc = extraBlockedFunc;
    }
    public bool IsBlocked(GridNavMesh navMesh, int index)
    {
        return navMesh.IsSquareBlocked(index) && extraBlockedFunc(index);
    }
    public float GetCost(GridNavMesh navMesh, int index, GridNavDirection dir)
    {
        return navMesh.GetSquareCost(index, dir);
    }
}

public class GridNavQueryConstraintDef : IGridNavQueryConstraint
{
    private int goalIndex;
    private float goalRadius;

    public GridNavQueryConstraintDef(int goalIndex, float goalRadius)
    {
        this.goalIndex = goalIndex;
        this.goalRadius = goalRadius;
    }
    public bool IsGoal(GridNavMesh navMesh, int index)
    {
        return navMesh.DistanceApproximately(index, goalIndex) <= goalRadius;
    }
    public bool WithinConstraints(GridNavMesh navMesh, int index)
    {
        return true;
    }
    public float GetHeuristicCost(GridNavMesh navMesh, int index)
    {
        return navMesh.DistanceApproximately(index, goalIndex);
    }
}

public class GridNavQueryConstraintCircle : IGridNavQueryConstraint
{
    private int goalIndex;
    private float goalRadius;
    private int circleIndex;
    private float circleRadius;

    public GridNavQueryConstraintCircle(int goalIndex, float goalRadius, int circleIndex, float circleRadius)
    {
        this.goalIndex = goalIndex;
        this.goalRadius = goalRadius;
        this.circleIndex = circleIndex;
        this.circleRadius = circleRadius;
    }
    public bool IsGoal(GridNavMesh navMesh, int index)
    {
        return navMesh.DistanceApproximately(index, goalIndex) <= goalRadius;
    }
    public bool WithinConstraints(GridNavMesh navMesh, int index)
    {
        return navMesh.DistanceApproximately(index, circleIndex) <= circleRadius;
    }
    public float GetHeuristicCost(GridNavMesh navMesh, int index)
    {
        return navMesh.DistanceApproximately(index, goalIndex);
    }
}