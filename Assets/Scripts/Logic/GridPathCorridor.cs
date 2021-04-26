using System.Collections.Generic;
using UnityEngine;

class GridPathCorridor
{
    private GridNavQueryFilterExtraBlockedCheck filter;
    private GridNavQueryConstraintCircle constraint;
    private Vector3 targetPos;
    private List<int> path;

    public void Reset()
    {
    }
}