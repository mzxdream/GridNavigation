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

public class GridNavQueryFilterUnitSize : IGridNavQueryFilter
{
    private int halfUnitSize;

    public GridNavQueryFilterUnitSize(int unitSize)
    {
        this.halfUnitSize = unitSize >> 1;
    }
    public bool IsBlocked(GridNavMesh navMesh, int index)
    {
        navMesh.GetSquareXZ(index, out var x, out var z);
        int xmin = x - halfUnitSize;
        int xmax = x + halfUnitSize;
        int zmin = z - halfUnitSize;
        int zmax = z + halfUnitSize;
        if (xmin < 0 || xmax >= navMesh.XSize || zmin < 0 || zmax >= navMesh.ZSize)
        {
            return true;
        }
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                var tIndex = navMesh.GetSquareIndex(tx, tz);
                if (navMesh.IsSquareBlocked(tIndex))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public float GetCost(GridNavMesh navMesh, int index, GridNavDirection dir)
    {
        return navMesh.GetSquareCost(index, dir);
    }
}

public class GridNavQueryFilterExtraBlockedCheck : IGridNavQueryFilter
{
    private int halfUnitSize;
    private Func<int, bool> extraBlockedFunc;

    public GridNavQueryFilterExtraBlockedCheck(int unitSize, Func<int, bool> extraBlockedFunc)
    {
        this.halfUnitSize = unitSize >> 1;
        this.extraBlockedFunc = extraBlockedFunc;
    }
    public bool IsBlocked(GridNavMesh navMesh, int index)
    {
        navMesh.GetSquareXZ(index, out var x, out var z);
        int xmin = x - halfUnitSize;
        int xmax = x + halfUnitSize;
        int zmin = z - halfUnitSize;
        int zmax = z + halfUnitSize;
        if (xmin < 0 || xmax >= navMesh.XSize || zmin < 0 || zmax >= navMesh.ZSize)
        {
            return true;
        }
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                var tIndex = navMesh.GetSquareIndex(tx, tz);
                if (navMesh.IsSquareBlocked(tIndex) || extraBlockedFunc(tIndex))
                {
                    return true;
                }
            }
        }
        return false;
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

public class GridNavQueryConstraintCircleStrict : IGridNavQueryConstraint
{
    private int goalIndex;
    private int circleIndex;
    private float circleRadius;

    public GridNavQueryConstraintCircleStrict(int goalIndex, int circleIndex, float circleRadius)
    {
        this.goalIndex = goalIndex;
        this.circleIndex = circleIndex;
        this.circleRadius = circleRadius;
    }
    public bool IsGoal(GridNavMesh navMesh, int index)
    {
        return index == goalIndex;
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