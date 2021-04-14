using System;
using UnityEngine;

public interface IGridNavQueryFilter
{
    bool PassFilter(GridNavMesh navMesh, int index);
    float GetCost(GridNavMesh navMesh, int index, int parentIndex);
}

public class GridNavQueryFilterDef : IGridNavQueryFilter
{
    public bool PassFilter(GridNavMesh navMesh, int index)
    {
        return !navMesh.IsSquareBlocked(index);
    }
    public float GetCost(GridNavMesh navMesh, int index, int parentIndex)
    {
        return navMesh.GetSquareCost(index) + navMesh.DistanceApproximately(parentIndex, index);
    }
}

public class GridNavQueryFilterExtraFilter
{
    private Func<int, bool> extraFilterFunc;

    public GridNavQueryFilterExtraFilter(Func<int, bool> extraFilterFunc)
    {
        this.extraFilterFunc = extraFilterFunc;
    }
    public bool PassFilter(GridNavMesh navMesh, int index)
    {
        return !navMesh.IsSquareBlocked(index) && extraFilterFunc(index);
    }
    public float GetCost(GridNavMesh navMesh, int index, int parentIndex)
    {
        return navMesh.GetSquareCost(index) + navMesh.DistanceApproximately(parentIndex, index);
    }
}

public interface IGridNavQueryConstraint
{
    bool IsGoal(GridNavMesh navMesh, int index);
    bool WithinConstraints(GridNavMesh navMesh, int index);
    float GetHeuristicCost(GridNavMesh navMesh, int index);
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

    public GridNavQueryConstraintDef(int goalIndex, float goalRadius)
    {
        this.goalIndex = goalIndex;
    }
    public bool IsGoal(GridNavMesh navMesh, int index)
    {
        return index == goalIndex;
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