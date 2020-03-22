using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public Vector2Int GridSize; //Oyundaki gridin büyüklüğü
    public Color [] TileColors; //Tile sayısı ve renkleri
    public int [] TileCounts; //Hangi renk tiledan kaç tane olduğu
    [SerializeField] private GameObject Tile;
    [SerializeField] private Transform TileEdge;
    [SerializeField] private Transform TileParent;
    [SerializeField] private float TileScale = 1f;
    [SerializeField] private float Gap = 0.65f;
    [SerializeField] private float VerticalAdjusment = 1f;
    [SerializeField] private float AllowedDistance = 0.8f;
    [SerializeField] private float RotationTime = 0.3f;
    private GameObject Selection = null;
    private Vector3 StartPosition;
    private List<MiddlePointsList> MiddlePoints = new List<MiddlePointsList>();
    private GameGridList [,] GameGrid;
    private List<Vector2Int> ColorMatchPoints = new List<Vector2Int>();
    private Vector2 InputStartPos, InputLastPos;
    private bool MovementPermission = true;
    private int MinimumAngle = 30;
    private bool Swipe = false;
    private float previousTileScale;
    private Vector2Int [] SelectedTiles = new Vector2Int [3];
    private GameObject [] SelectedTileObjects = new GameObject [3];
    private int SelectedMiddlePoint;


    
    // Start is called before the first frame update
    void Start()
    {
        TileParent.transform.localScale = new Vector3(TileScale,TileScale,TileScale);
        TileCounts = new int [TileColors.Length];
        GameGrid = new GameGridList[GridSize.x,GridSize.y];
        CreateField();
        previousTileScale = TileScale;
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        ScaleTiles();
    }

    void CreateField()
    {
        var DistanceX = (GridSize.x-1) * Mathf.Cos(Mathf.Deg2Rad * 30) * Gap;
        var DistanceY = (GridSize.y-1) * Gap + Gap * Mathf.Sin(Mathf.Deg2Rad*30);
        StartPosition = transform.position - new Vector3(DistanceX / 2, DistanceY / 2 + VerticalAdjusment, 0);
        int Rnd;
        bool boolPos;
        Vector2 _middlePoint;
        GameObject TileObj;
        for(var j = 0; j < GridSize.y; j++) {
            for(var i = 0; i < GridSize.x; i++) {
                boolPos = (i % 2 == 0);
                Vector3 TilePos = StartPosition + new Vector3(i * Mathf.Cos(Mathf.Deg2Rad * 30) * Gap,j * Gap + (boolPos ? Gap * Mathf.Sin(Mathf.Deg2Rad * 30) : 0));
                TileObj = Instantiate(Tile,TilePos,Quaternion.identity,TileParent) as GameObject;

                Rnd = Random.Range(0,TileColors.Length);
                Color col = new Color(TileColors[Rnd].r,TileColors[Rnd].g,TileColors[Rnd].b);
                GameGrid[i,j] = new GameGridList(TileObj,col,new Vector2Int(i,j),TilePos);
                TileObj.GetComponent<SpriteRenderer>().color = col;
                //TileCounts [Rnd]++;

                //Middle points
                if (i != 0 && j != GridSize.y-1) {
                    _middlePoint = GetMiddlePoint(GetWorldPosition(i,j),GetWorldPosition(i,j + 1),GetWorldPosition(i - 1,j + (boolPos ? 1 : 0)));
                    MiddlePoints.Add(new MiddlePointsList(_middlePoint,new Vector2Int(i,j),new Vector2Int(i,j + 1),new Vector2Int(i - 1,j + (boolPos ? 1 : 0)),true));
                }

                if (i < GridSize.x-1 && j != GridSize.y-1) {
                    _middlePoint = GetMiddlePoint(GetWorldPosition(i,j),GetWorldPosition(i + 1,j + (boolPos ? 1 : 0)),GetWorldPosition(i,j + 1));
                    MiddlePoints.Add(new MiddlePointsList(_middlePoint,new Vector2Int(i,j),new Vector2Int(i + 1,j + (boolPos ? 1 : 0)),new Vector2Int(i,j + 1),false));
                }
            }
        }

        //Renk eşleşmesi olduysa, renk eşleşmesi olan tileların rengini yenile. Hiçbir eşleşme kalmayana kadar devam et.
        ColorMatchDetection();
        while (ColorMatchPoints.Count != 0) {
            for (var h = 0; h < ColorMatchPoints.Count; h++) {
                Rnd = Random.Range(0,TileColors.Length);
                Color col = new Color(TileColors [Rnd].r,TileColors [Rnd].g,TileColors [Rnd].b);
                GameGrid [ColorMatchPoints [h].x,ColorMatchPoints [h].y].TileColor = col;
                GameGrid [ColorMatchPoints [h].x,ColorMatchPoints [h].y].TileObject.GetComponent<SpriteRenderer>().color = col;
            }
            ColorMatchDetection();
        }
    }
    
    Vector2 GetWorldPosition(int i, int j)
    {
        bool Pos = (i % 2 == 1);
        return StartPosition + new Vector3(i * Mathf.Cos(Mathf.Deg2Rad * 30) * Gap,j * Gap + (Pos ? 0 : Gap * Mathf.Sin(Mathf.Deg2Rad * 30)));
    }

    Vector2 GetMiddlePoint(Vector2 Point1, Vector2 Point2, Vector2 Point3)
    {
        return (Point1 + Point2 + Point3) / 3;
    }
    void ColorMatchDetection()
    {
        ColorMatchPoints.Clear();
        Vector2Int [] TileCheck = new Vector2Int [3];
        Color[] TileColor = new Color [3];
        for (var k = 0; k < MiddlePoints.Count; k++) {
            TileCheck [0] = MiddlePoints [k].GridPositions [0];
            TileCheck [1] = MiddlePoints [k].GridPositions [1];
            TileCheck [2] = MiddlePoints [k].GridPositions [2];
            TileColor[0] = GameGrid [TileCheck [0].x,TileCheck [0].y].TileColor;
            TileColor[1] = GameGrid [TileCheck [1].x,TileCheck [1].y].TileColor;
            TileColor[2] = GameGrid [TileCheck [2].x,TileCheck [2].y].TileColor;
            if (TileColor[0] == TileColor[1] && TileColor[0] == TileColor[2]) {
                for (var m = 0; m < 3; m++) {
                    if (!ColorMatchPoints.Contains(TileCheck [m])) {
                        ColorMatchPoints.Add(TileCheck [m]);
                    }
                }
            }
        }
    }
    void HandleInput() {
        if (MovementPermission) {
            if (Selection != null && !Swipe) {
                if (Input.GetMouseButtonDown(0)) {
                    InputStartPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y),0) - Selection.transform.position;
                }

                if (Input.GetMouseButton(0)) {
                    InputLastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y),0) - Selection.transform.position;
                    if (InputLastPos != InputStartPos) {
                        if (Vector2.Angle(InputStartPos,InputLastPos) > MinimumAngle) {
                            Swipe = true;
                            StartCoroutine(RotateSelection(Mathf.FloorToInt(Mathf.Sign(Vector2.SignedAngle(InputStartPos,InputLastPos)))));
                        }
                    }
                }
            }

            if (Input.GetMouseButtonUp(0)) {
                if (Swipe) {
                }
                else { 
                    Debug.Log("Up");
                    int MP = MinimumDistance();
                    SelectedMiddlePoint = MP;
                    if (MP != -1) {
                        if (Selection != null) {
                            Destroy(Selection);
                        }
                        Selection = Instantiate(TileEdge,new Vector3(MiddlePoints [MP].PointPosition.x,MiddlePoints [MP].PointPosition.y,-2),GetSelectionType(MP)).gameObject;
                        SelectedTiles [0] = MiddlePoints [MP].GridPositions [0];
                        SelectedTiles [1] = MiddlePoints [MP].GridPositions [1];
                        SelectedTiles [2] = MiddlePoints [MP].GridPositions [2];
                        for (var s = 0; s < SelectedTiles.Length; s++) {
                            SelectedTileObjects [s] = GameGrid [SelectedTiles [s].x,SelectedTiles [s].y].TileObject;
                            var Pos = SelectedTileObjects [s].transform.position;
                            SelectedTileObjects [s].transform.position = new Vector3(Pos.x,Pos.y,-1f);
                            //SelectedTileObjects [s].GetComponent<SpriteRenderer>().color = Color.blue;
                        }
                        Debug.Log("Tile0: " + SelectedTiles [0]);
                        Debug.Log("Tile1: " + SelectedTiles [1]);
                        Debug.Log("Tile2: " + SelectedTiles [2]);
                    }
                }
            }
        }
    }
    int MinimumDistance ()
    {
        Vector2 MousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y),0);
        float MinDist = Mathf.Infinity;
        int Result = -1;
        for(var k = 0; k < MiddlePoints.Count; k++) {
            if ((MiddlePoints[k].PointPosition - MousePos).sqrMagnitude < MinDist) {
                MinDist = (MiddlePoints [k].PointPosition - MousePos).sqrMagnitude;
                Result = k;
            }
        }
        if ((MiddlePoints[Result].PointPosition - MousePos).sqrMagnitude > AllowedDistance) {
            Debug.Log("Maksimum mesafeyi aştı!");
            return -1;
        }
        return Result;
    }
    Quaternion GetSelectionType(int MP)
    {
        return ((MiddlePoints [MP].Rotate) ? Quaternion.Euler(new Vector3(0,0,60)) : Quaternion.identity);
    }

    IEnumerator RotateSelection(int sign)
    {
        StartCoroutine(Rotation(sign));
        yield return new WaitForSeconds(RotationTime);
        SwitchTiles(sign);
        
        ColorMatchDetection();

        if (ColorMatchPoints.Count != 0) {
            foreach (Vector2Int vect in ColorMatchPoints) {
                GameGrid [vect.x,vect.y].TileColor = Color.black;
                GameGrid [vect.x,vect.y].TileObject.GetComponent<SpriteRenderer>().color = Color.black;
            }
        }
        else {
            StartCoroutine(Rotation(sign));
            yield return new WaitForSeconds(RotationTime);
        }

        SwitchTiles(sign);
        
        ColorMatchDetection();

        if (ColorMatchPoints.Count != 0) {
            foreach (Vector2Int vect in ColorMatchPoints) {
                GameGrid [vect.x,vect.y].TileColor = Color.black;
                GameGrid [vect.x,vect.y].TileObject.GetComponent<SpriteRenderer>().color = Color.black;
            }
        }
        else {
            StartCoroutine(Rotation(sign));
            yield return new WaitForSeconds(RotationTime);
        }
        
        //Seçilen tileların derinliğini ve rotasyonlarını sıfırla
        Vector3 Pos;
        for (var s = 0; s < SelectedTileObjects.Length; s++) {
            Pos = SelectedTileObjects[s].transform.position;
            SelectedTileObjects[s].transform.position = new Vector3(Pos.x,Pos.y,0f);
            SelectedTileObjects[s].transform.localRotation = Quaternion.identity;
        }

        Swipe = false;
        MovementPermission = true;
    }

    void SwitchTiles(int SwitchingDirection)
    {
        /*SelectedTiles [0] = MiddlePoints [SelectedMiddlePoint].GridPositions [0];
        SelectedTiles [1] = MiddlePoints [SelectedMiddlePoint].GridPositions [1];
        SelectedTiles [2] = MiddlePoints [SelectedMiddlePoint].GridPositions [2];
        for (var s = 0; s < SelectedTiles.Length; s++) {
            SelectedTileObjects [s] = GameGrid [SelectedTiles [s].x,SelectedTiles [s].y].TileObject;
            var Pos = SelectedTileObjects [s].transform.position;
            SelectedTileObjects [s].transform.position = new Vector3(Pos.x,Pos.y,-1f);
            //SelectedTileObjects [s].GetComponent<SpriteRenderer>().color = Color.blue;
        }*/

        if (SwitchingDirection == -1) {

            GameObject _tileObject = GameGrid [SelectedTiles [0].x,SelectedTiles [0].y].TileObject;
            var _tileColor = GameGrid [SelectedTiles [0].x,SelectedTiles [0].y].TileColor;
            var _tilePos = SelectedTiles [0];

            GameGrid [SelectedTiles [0].x,SelectedTiles [0].y].TileColor = GameGrid [SelectedTiles [1].x,SelectedTiles [1].y].TileColor;
            GameGrid [SelectedTiles [0].x,SelectedTiles [0].y].TileObject = GameGrid [SelectedTiles [1].x,SelectedTiles [1].y].TileObject;
            SelectedTiles [0] = SelectedTiles [1];

            GameGrid [SelectedTiles [1].x,SelectedTiles [1].y].TileColor = GameGrid [SelectedTiles [2].x,SelectedTiles [2].y].TileColor;
            GameGrid [SelectedTiles [1].x,SelectedTiles [1].y].TileObject = GameGrid [SelectedTiles [2].x,SelectedTiles [2].y].TileObject;
            SelectedTiles [1] = SelectedTiles [2];

            GameGrid [SelectedTiles [2].x,SelectedTiles [2].y].TileColor = _tileColor;
            GameGrid [SelectedTiles [2].x,SelectedTiles [2].y].TileObject = _tileObject;
            SelectedTiles [2] = _tilePos;

        }
        
        else {
            GameObject _tileObject = GameGrid [SelectedTiles [0].x,SelectedTiles [0].y].TileObject;
            var _tileColor = GameGrid [SelectedTiles [0].x,SelectedTiles [0].y].TileColor;
            var _tilePos = SelectedTiles [0];

            GameGrid [SelectedTiles [0].x,SelectedTiles [0].y].TileColor = GameGrid [SelectedTiles [2].x,SelectedTiles [2].y].TileColor;
            GameGrid [SelectedTiles [0].x,SelectedTiles [0].y].TileObject = GameGrid [SelectedTiles [2].x,SelectedTiles [2].y].TileObject;
            SelectedTiles [0] = SelectedTiles [2];

            GameGrid [SelectedTiles [2].x,SelectedTiles [2].y].TileColor = GameGrid [SelectedTiles [1].x,SelectedTiles [1].y].TileColor;
            GameGrid [SelectedTiles [2].x,SelectedTiles [2].y].TileObject = GameGrid [SelectedTiles [1].x,SelectedTiles [1].y].TileObject;
            SelectedTiles [2] = SelectedTiles [1];

            GameGrid [SelectedTiles [1].x,SelectedTiles [1].y].TileColor = _tileColor;
            GameGrid [SelectedTiles [1].x,SelectedTiles [1].y].TileObject = _tileObject;
            SelectedTiles [1] = _tilePos;
        }

        for (var s = 0; s < SelectedTiles.Length; s++) {
            SelectedTileObjects [s] = GameGrid [SelectedTiles [s].x,SelectedTiles [s].y].TileObject;
            var Pos = SelectedTileObjects [s].transform.position;
            SelectedTileObjects [s].transform.position = new Vector3(Pos.x,Pos.y,-1f);
        }
    }

    IEnumerator Rotation(int sign)
    {
        float time = 0f;
        float TotalAngle = 0f;
        float targetAngle = -120f * sign;
        while (time <= 1f) {
            time += Time.deltaTime / RotationTime;
            float Ang = Mathf.Lerp(0f,targetAngle,Time.deltaTime / RotationTime);
            if (Mathf.Abs(TotalAngle+Ang) > Mathf.Abs(targetAngle)) {
                Ang = targetAngle - TotalAngle;
            }
            TotalAngle += Ang;
            for(var s = 0; s < SelectedTileObjects.Length; s++) {
                SelectedTileObjects[s].transform.RotateAround(Selection.transform.position, Vector3.back, Ang);
            }
            Selection.transform.Rotate(new Vector3(0f,0f,-Ang));
            yield return null;
        }
    }

    void ScaleTiles()
    {
        if (TileScale != previousTileScale) {
            TileParent.transform.localScale = new Vector3(TileScale,TileScale,TileScale);
            previousTileScale = TileScale;
        }
    }

}