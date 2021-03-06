﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BombList
{
    public Vector2Int TileGridPosition;
    public int BombCountdown;
    public Text BombText;
    public bool IsBombNew;

    public BombList(Vector2Int NewTileGridPosition, int NewBombCountdown, Text NewBombText, bool NewIsBombNew)
    {
        TileGridPosition = NewTileGridPosition;
        BombCountdown = NewBombCountdown;
        BombText = NewBombText;
        IsBombNew = NewIsBombNew;
    }
}
