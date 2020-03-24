using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiddlePointsList
{
    public Vector2 PointPosition;
    public Vector2Int [] GridPositions = new Vector2Int [3];
    public bool Rotate = false;

    public MiddlePointsList(Vector2 NewPosition0, Vector2Int NewGridPosition1,Vector2Int NewGridPosition2,Vector2Int NewGridPosition3, bool NewRotate)
    {
        PointPosition = NewPosition0;
        GridPositions[0] = NewGridPosition1;
        GridPositions[1] = NewGridPosition2;
        GridPositions[2] = NewGridPosition3;
        Rotate = NewRotate;
    }
}
