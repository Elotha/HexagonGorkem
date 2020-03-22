using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGridList
{
    public GameObject TileObject;
    public Color TileColor;
    public Vector2Int TileGridPosition;
    public Vector2 TileWorldPosition;

    public GameGridList(GameObject NewTileObject, Color NewTileColor, Vector2Int NewTileGridPosition,Vector2 NewTileWorldPosition)
    {
        TileObject = NewTileObject;
        TileColor = NewTileColor;
        TileGridPosition = NewTileGridPosition;
        TileWorldPosition = NewTileWorldPosition; //Bu gereksiz olabilir, bakıcaz
    }
}
