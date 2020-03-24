using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    //Temel değerler
    [SerializeField] private  Vector2Int GridSize; //Oyundaki gridin büyüklüğü
    [SerializeField] private  Color [] TileColors; //Tile renkleri
    [SerializeField] private GameObject Tile; //Altıgen objesi
    [SerializeField] private Transform TileParent; //Altıgenlerin parent objesi
    [SerializeField] private GameObject Particle; //Particle efektinin objesi
    [SerializeField] private Transform ParticleParent; //Particle efekti için parent obje
    [SerializeField] private Transform TileOutline; //Altıgenler seçildiğinde çıkan outline objesi
    [SerializeField] private float TileScale = 1f; //Altıgenlerin büyüklüğü
    [SerializeField] private float Gap = 0.65f; //Altıgenlerin arasındaki boşluk miktarı
    [SerializeField] private float VerticalAdjusment = 1f; //Oyun sahası ekranın ne kadar altında
    [SerializeField] private float AllowedDistance = 0.8f; //Altıgenleri seçebilmek için oyuncunun basabileceği maksimum uzaklık
    [SerializeField] private float RotationTime = 0.3f; //Altıgenlerin bir defalık dönme süresi

    //Skor, hareket sayısı, yıldızlı altıgen, bomba
    [SerializeField] private int Score = 0; //Oyuncunun skoru
    [SerializeField] private int Moves = 0; //Oyuncunun yaptığı toplam hareket sayısı
    [SerializeField] private int StarPercent = 8; //Yıldızlı altıgenin çıkmasının yüzde ihtimali
    [SerializeField] private Sprite StarSprite; //Yıldızlı altıgenin görseli
    [SerializeField] private Sprite BombSprite; //Bombalı altıgenin görseli
    [SerializeField] private GameObject BombUIObject; //Bombanın UI objesi
    [SerializeField] private GameObject ScoreObject; //Skorun UI objesi
    [SerializeField] private GameObject MovesObject; //"MovesText" objesi
    [SerializeField] private int BombCountdownMax = 10; //Bomba için geri sayımın başladığı sayı
    [SerializeField] private int BombMinimumScore = 1000; //Bomba için gerekli skor miktarı
    private List<BombList> Bombs = new List<BombList>(); //Bombaların pozisyon ve sayaçlarını tutan liste
    private int BombCount = 0; //Oyun boyunca yaratılan bomba sayısı
    private Text ScoreText; //Skor texti
    private Text MovesText; //Moves sayısı

    //Altıgenlerin ve üçlü keşismelerin noktaları, seçim objesi, seçilen objeler, renk eşleşmeleri
    private GameGridList [,] GameGrid; //Altıgenler ile ilgili gerekli tüm bilgileri tutan grid arrayi
    private List<MiddlePointsList> MiddlePoints = new List<MiddlePointsList>(); //Altıgenlerin üçlü olarak seçilebildiği her nokta
    private GameObject Selection = null; //Altıgenleri seçen outline objesi
    private Vector2Int [] SelectedTiles = new Vector2Int [3]; //Hangi üç grid pozisyonunun seçili olduğunu tutan array
    private GameObject [] SelectedTileObjects = new GameObject [3]; //Hangi üç objenin seçili olduğunu tutan array
    private List<Vector2Int> ColorMatchPoints = new List<Vector2Int>(); //Hangi noktalarda renk eşleşmesi olduğunu tutan liste

    //Swipe mekaniği
    [SerializeField] private int MinimumAngle = 30; //Swipe mekaniğinin çalışması için gereken minimum açı
    private Vector2 InputStartPos, InputLastPos; //Oyuncunun ekrana bastığı ilk pozisyon ve parmağını sürüklediği son pozisyon
    private bool MovementPermission = true; //Oyuncu oyun ekranına basabilir mi?
    private bool Swipe = false; //Swipe yapabilme iznini kontrol eden değer

    //Diğer değerler
    [SerializeField] private GameObject GameOverObject; //Game over ekranı için UI objesi
    [SerializeField] private Transform CanvasObject; //Canvas UI parent objesi
    private Vector3 StartPosition; //Gridin başladığı dünya koordinatları
    private float previousTileScale; //Altıgenlerin boyutunu oyun sırasında ayarlayabilmek için gereken bir değer
    private float debugWaitingTime = 0.05f; //Debug amaçlı olarak altıgenlerin düşmesi ve yenilerinin yaratılmasının bekleme miktarı


    
    // Start is called before the first frame update
    void Start()
    {
        TileParent.transform.localScale = new Vector3(TileScale,TileScale,TileScale); //Altıgenlerin boyutunu ayarla
        GameGrid = new GameGridList[GridSize.x,GridSize.y]; //Altıgenlerle ilgili gerekli her bilgiyi tutan arrayin boyutunu belirle
        CreateField(); //Alandaki altıgenleri yarat
        previousTileScale = TileScale;
        ScoreText = ScoreObject.GetComponent<Text>();
        MovesText = MovesObject.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        ScaleTiles();
    }

    //Alanı yaratan fonksiyon
    void CreateField()
    {
        //Altıgenlerin oyun koordinatlarında nereden başlaması gerektiğini hesapla
        var distanceX = (GridSize.x-1) * Mathf.Cos(Mathf.Deg2Rad * 30) * Gap;
        var distanceY = (GridSize.y-1) * Gap + Gap * Mathf.Sin(Mathf.Deg2Rad*30);
        StartPosition = transform.position - new Vector3(distanceX / 2, distanceY / 2 + VerticalAdjusment, 0);

        //İhtiyaç olacak geçici değerleri başlat
        int rnd, starRnd;
        bool boolPos;
        Vector2 _middlePoint;
        GameObject tileObj;
        string tileType;

        //Griddeki her bir birim için altıgeni yarat, altıgen bilgilerini doldur, üçlü kesişmelerin noktalarını belirle
        for(var j = 0; j < GridSize.y; j++) {
            for(var i = 0; i < GridSize.x; i++) {
                
                //Altıgenin pozisyonunu belirle, ardından yarat
                boolPos = (i % 2 == 0); //Altıgenler her iki yatay birimde bir, dikeyde aşağı kayıyor
                Vector3 TilePos = StartPosition + new Vector3(i * Mathf.Cos(Mathf.Deg2Rad * 30) * Gap,j * Gap + (boolPos ? Gap * Mathf.Sin(Mathf.Deg2Rad * 30) : 0));
                tileObj = Instantiate(Tile,TilePos,Quaternion.identity,TileParent) as GameObject;
                tileObj.GetComponent<TilePosition>().TilePositionVector = new Vector2Int(i,j); //Editörden altıgenlerin pozisyonu görünebilsin

                //Rastgele bir renk belirle, yıldız olma ihtimalini belirle, ardından atamaları yap
                rnd = Random.Range(0,TileColors.Length);
                Color col = new Color(TileColors[rnd].r,TileColors[rnd].g,TileColors[rnd].b);
                starRnd = Random.Range(0,100);
                var _spriteRenderer = tileObj.GetComponent<SpriteRenderer>();
                if (starRnd <= StarPercent) {
                    tileType = "Star";
                    _spriteRenderer.sprite = StarSprite;
                }
                else {
                    tileType = "Standart";
                }
                _spriteRenderer.color = col;

                //Altıgenlerin bilgilerini grid arrayine doldur
                GameGrid[i,j] = new GameGridList(tileObj,col,new Vector2Int(i,j),TilePos,tileType,false);

                //Üçlü kesişmelerin olduğu noktaların koordinatlarını listeye doldur. 
                //Üç tane altıgenin tam orta noktasını alarak listeye koyuyor.
                //Burada, köşelerde olmayan her altıgen için iki adet kesişme belirliyor. Köşeler için ise birer tane belirliyor.
                //Bu şekilde griddeki her satır için (altıgen sayısı . 2 + 2) kadar kesişme noktası belirlenmiş oluyor.
                //MiddlePointsList, kesişme noktasının oyun koordinatlarını, etrafındaki üç altıgenin grid koordinatlarını ve
                //seçim sırasında "TileOutline" objesinin 60 derece dönmesinin gerekip gerekmeyeceğini tutar.
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

        //Renk eşleşmesi olduysa, renk eşleşmesi olan altıgenlerin rengini yenile. Hiçbir eşleşme kalmayana kadar bu işlemi tekrarla.
        while (ColorMatchDetection()) {
            for (var h = 0; h < ColorMatchPoints.Count; h++) {
                rnd = Random.Range(0,TileColors.Length);
                Color col = new Color(TileColors [rnd].r,TileColors [rnd].g,TileColors [rnd].b);
                GameGrid [ColorMatchPoints [h].x,ColorMatchPoints [h].y].TileColor = col;
                GameGrid [ColorMatchPoints [h].x,ColorMatchPoints [h].y].TileObject.GetComponent<SpriteRenderer>().color = col;
            }
        }

        //Yapılabilecek hiçbir hamle yoksa sahneyi yenile
        if (!AnyMovesLeft()) {
            Debug.Log("Oyun başarısız başladı, yenileniyor.");
            for(var q = 0; q < TileParent.childCount; q++) {
                Destroy(TileParent.GetChild(q).gameObject);
            }
            CreateField();
        }
    }
    
    //Grid içerisindeki pozisyonu bilinen bir altıgenin oyun içerisindeki koordinatlarını bul
    Vector2 GetWorldPosition(int i, int j)
    {
        bool Pos = (i % 2 == 1);
        return StartPosition + new Vector3(i * Mathf.Cos(Mathf.Deg2Rad * 30) * Gap,j * Gap + (Pos ? 0 : Gap * Mathf.Sin(Mathf.Deg2Rad * 30)));
    }

    //Üç altıgenin tam orta noktasını al
    Vector2 GetMiddlePoint(Vector2 Point1, Vector2 Point2, Vector2 Point3)
    {
        return (Point1 + Point2 + Point3) / 3;
    }

    //Renk eşleşmelerini belirle
    bool ColorMatchDetection()
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

            //Renk eşleşmesi varsa, eşleşme olan her altıgen grid pozisyonunu listeye ekle
            if (TileColor[0] == TileColor[1] && TileColor[0] == TileColor[2]) { 
                for (var m = 0; m < 3; m++) {
                    if (!ColorMatchPoints.Contains(TileCheck [m])) {
                        ColorMatchPoints.Add(TileCheck [m]);
                    }
                }
            }
        }
        if (ColorMatchPoints.Count == 0) {
            return false;
        }
        else return true;
    }

    //Oyuncu etkileşimi ile ilgili her şeyi halleden fonksiyon
    void HandleInput() {
        if (MovementPermission) { //Oyun alanı ile etkileşime girme iznimiz varsa
            if (Selection != null && !Swipe) { //Üçlü bir seçim varsa ve oyuncunun swipe yapma izni varsa
                if (Input.GetMouseButtonDown(0)) {
                    //Oyuncunun bastığı ilk pozisyonu al
                    InputStartPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y),0) - Selection.transform.position;
                }

                if (Input.GetMouseButton(0)) {
                    //Oyuncunun basılı tutuyorsa, bastığı yerin koordinatlarını al
                    InputLastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y),0) - Selection.transform.position;
                    if (InputLastPos != InputStartPos) { //Oyuncu parmağını ilerletti
                        if (Vector2.Angle(InputStartPos,InputLastPos) > MinimumAngle) { //Bastığı ilk nokta ve son nokta arasında yeterli swipe açısı oluştu mu?
                            Swipe = true; 
                            StartCoroutine(RotateSelection(Mathf.FloorToInt(Mathf.Sign(Vector2.SignedAngle(InputStartPos,InputLastPos))))); //Seçimi döndürme fonksiyonu
                        }
                    }
                }

                /*if (Input.touchCount > 0) {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began) {
                        InputStartPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y),0) - Selection.transform.position;
                    }
                    else if (touch.phase == TouchPhase.Moved) {
                        InputLastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y),0) - Selection.transform.position;
                        if (InputLastPos != InputStartPos) {
                            if (Vector2.Angle(InputStartPos,InputLastPos) > MinimumAngle) {
                                Swipe = true;
                                StartCoroutine(RotateSelection(Mathf.FloorToInt(Mathf.Sign(Vector2.SignedAngle(InputStartPos,InputLastPos)))));
                            }
                        }
                    }
                }*/
            }
            
            if (Input.GetMouseButtonUp(0)) { //Oyuncu parmağını kaldırdıysa
                if (!Swipe) { 
                    int MP = MinimumDistance(); //Üçlü kesişme noktalarından en yakını hangisi, onun liste indexini al
                    if (MP != -1) {
                        if (Selection != null) {
                            //Seçilen altıgenlerin derinliğini ve rotasyonlarını sıfırla
                            Vector3 Pos;
                            for (var s = 0; s < SelectedTileObjects.Length; s++) {
                                Pos = SelectedTileObjects [s].transform.position;
                                SelectedTileObjects [s].transform.position = new Vector3(Pos.x,Pos.y,0f);
                                SelectedTileObjects [s].transform.localRotation = Quaternion.identity;
                            }
                        }
                        else {
                            //Yeni "TileOutline" objesini yarat ve üçlü kesişmedeki her bir grid pozisyonunu SelectedTiles arrayinde tut
                            Selection = Instantiate(TileOutline,new Vector3(MiddlePoints [MP].PointPosition.x,MiddlePoints [MP].PointPosition.y,-2),GetSelectionType(MP)).gameObject;
                        }
                        Selection.transform.position = new Vector3(MiddlePoints [MP].PointPosition.x,MiddlePoints [MP].PointPosition.y,-2);
                        Selection.transform.rotation = GetSelectionType(MP);
                        SelectedTiles [0] = MiddlePoints [MP].GridPositions [0];
                        SelectedTiles [1] = MiddlePoints [MP].GridPositions [1];
                        SelectedTiles [2] = MiddlePoints [MP].GridPositions [2];
                        SelectTileObjects();
                        //Debug.Log("Tile0: " + SelectedTiles [0]);
                        //Debug.Log("Tile1: " + SelectedTiles [1]);
                        //Debug.Log("Tile2: " + SelectedTiles [2]);
                    }
                }
            }
            /*if (Input.touchCount > 0) {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended) {
                    if (!Swipe) { 
                        int MP = MinimumDistance();
                        if (MP != -1) {
                            if (Selection != null) {
                                Destroy(Selection);
                            }
                            Selection = Instantiate(TileOutline,new Vector3(MiddlePoints [MP].PointPosition.x,MiddlePoints [MP].PointPosition.y,-2),GetSelectionType(MP)).gameObject;
                            SelectedTiles [0] = MiddlePoints [MP].GridPositions [0];
                            SelectedTiles [1] = MiddlePoints [MP].GridPositions [1];
                            SelectedTiles [2] = MiddlePoints [MP].GridPositions [2];
                            SelectTileObjects();
                            //Debug.Log("Tile0: " + SelectedTiles [0]);
                            //Debug.Log("Tile1: " + SelectedTiles [1]);
                            //Debug.Log("Tile2: " + SelectedTiles [2]);
                        }
                    }
                }
            }*/
        }
    }

    //Üçlü kesişme noktalarından en yakını hangisi, onun liste indexini al
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
            //Debug.Log("Maksimum mesafeyi aştı!");
            return -1;
        }
        return Result;
    }

    //Üçlü kesişme noktasına göre "TileOutline" objesi 60 derece dönmüş olarak mı yaratılmalı, yoksa 0 derece olarak mı?
    Quaternion GetSelectionType(int MP)
    {
        return ((MiddlePoints [MP].Rotate) ? Quaternion.Euler(new Vector3(0,0,60)) : Quaternion.identity);
    }

    //Swipe mekaniğini çalıştır
    IEnumerator RotateSelection(int sign)
    {
        StartCoroutine(Rotation(sign)); //Bir defa döndür
        yield return new WaitForSeconds(RotationTime);
        SwitchTiles(sign); //Üç altıgenin bilgilerini takas et

        //Yeni durumda renk eşleşmesi varsa eşleşme olan her altıgeni yok et, hareket sayısını arttır, bomba varsa sayacını çalıştır
        if (ColorMatchDetection()) {
            DestroyTiles();
            Moves++;
        }
        else { //Eşleşme yok, bir kez daha döndür
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(Rotation(sign));
            yield return new WaitForSeconds(RotationTime);
            SwitchTiles(sign);
            ColorMatchDetection();
            if (ColorMatchDetection()) {
                DestroyTiles();
                Moves++;
                BombCountdown();
            }
            else { //Eşleşme yok, son bir kez daha döndür
                yield return new WaitForSeconds(0.1f);
                StartCoroutine(Rotation(sign));
                yield return new WaitForSeconds(RotationTime);
                SwitchTiles(sign);
                if (ColorMatchDetection()) {
                    DestroyTiles();
                    Moves++;
                    BombCountdown();
                }
                else {
                    Swipe = false; //Hala eşleşme yoksa, oyuncunun swipe yapabilme iznini tekrar aç
                    BombCountdown(); //Bombaların sayacını çalıştır
                    AnyMovesLeft(); //Yapılabilecek hareket kaldı mı?
                }
            }
        }
        MovesText.text = Moves.ToString();
    }
    
    //Seçilen altıgenleri döndüren fonksiyon
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
                if (SelectedTileObjects[s].transform.childCount > 0) { //Altıgen bomba taşıyorsa, bombanın sayacını döndürme
                    Vector3 rot = SelectedTileObjects [s].transform.rotation.eulerAngles;
                    SelectedTileObjects [s].transform.GetChild(0).GetChild(0).localRotation = Quaternion.Euler(new Vector3(0,0,0-rot.z));
                }

            }
            Selection.transform.Rotate(new Vector3(0f,0f,-Ang));
            yield return null;
        }
    }

    //Seçilen üç altıgenin bilgierini takas eden fonksiyon
    void SwitchTiles(int SwitchingDirection)
    {
        int dir, r;
        if (SwitchingDirection == -1) { //Negatif yön
            dir = 1;
            r = 0;
        }
        else { //Pozitif yön
            dir = -1;
            r = 2;
        }
        //Önce sıfır nolu seçili tileın bilgilerini yedeğe al
        var _tileObject = GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileObject;
        var _tileColor = GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileColor;
        var _tileType = GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileType;
        var _tileWorldPos = GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileWorldPosition;
        var _tilePos = SelectedTiles [r];

        //Ardından yöne bağlı olarak tileların bilgilerini takas et
        for(var p = 0; p < 3; p++) {
            if (p == 2) {
                GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileColor = _tileColor;
                GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileObject = _tileObject;
                GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileType = _tileType;
                GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileWorldPosition = _tileWorldPos;
                SelectedTiles [r] = _tilePos;
            }
            else {
                GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileColor = GameGrid [SelectedTiles [r+dir].x,SelectedTiles [r+dir].y].TileColor;
                GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileObject = GameGrid [SelectedTiles [r+dir].x,SelectedTiles [r+dir].y].TileObject;
                GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileType = GameGrid [SelectedTiles [r+dir].x,SelectedTiles [r+dir].y].TileType;
                GameGrid [SelectedTiles [r].x,SelectedTiles [r].y].TileWorldPosition = GameGrid [SelectedTiles [r+dir].x,SelectedTiles [r+dir].y].TileWorldPosition;
                SelectedTiles [r] = SelectedTiles [r+dir];
                r += dir;
            }
        }

    }

    //Renk eşleşmesi olduğunda eşleşen altıgenleri yok eden fonksiyon
    void DestroyTiles()
    {
        if (ColorMatchPoints.Count > 0) {
            int TempScore = 0;
            int StarCount = 0;
            for(var i = 0; i < ColorMatchPoints.Count; i++) { //Her eşleşme noktası için
                GameGrid [ColorMatchPoints [i].x,ColorMatchPoints [i].y].Empty = true; //Grid pozisyonunu boşalt
                Destroy(GameGrid [ColorMatchPoints [i].x,ColorMatchPoints [i].y].TileObject,0.1f); //Altıgeni yok et
                
                StartCoroutine(ParticleEffect(i)); //Partikül efekti yarat

                //Eşleşme koordinatlarındaki yıldızları ve bombaları belirle
                var _tileType = GameGrid [ColorMatchPoints [i].x,ColorMatchPoints [i].y].TileType;
                if (_tileType == "Star") {
                    StarCount++;
                }
                else if (_tileType == "Bomb") {
                    for(var g = 0; g < Bombs.Count; g++) {
                        if (Bombs[g].TileGridPosition == new Vector2Int(ColorMatchPoints [i].x,ColorMatchPoints [i].y)) {
                            Bombs.RemoveAt(g); //Bombayı listeden sil
                            break;
                        }
                    }
                }
                TempScore += 5; //Yok edilen her altıgen için geçici skoru 5 arttır
            }

            //Geçici skoru her bir yıldız için ikiye katla, ardından oyuncunun skoruna ekle
            for(var d = 0; d < StarCount; d++) {
                TempScore = TempScore * 2;
            }
            Score += TempScore;
            ScoreText.text = Score.ToString();

            //Yok olan altıgenler için yukarıdan yeni altıgenler indir
            StartCoroutine(ShiftTiles());
        }
    }

    //Bir renk eşleşmesi durumunda altıgenler yok olduğunda, yok olan altıgenleri yukarıdan yeni altıgenler indirerek doldur
    IEnumerator ShiftTiles()
    {
        yield return new WaitForSeconds(debugWaitingTime + 0.1f);

        List<int> XList = new List<int>();

        //Her bir renk eşleşmesi pozisyonu için çalıştır
        for (int n = 0;  n < ColorMatchPoints.Count; n++) { 

            //Çoktan aynı dikey hizada bir pozisyon için çalıştırmışsan sonraki eşleşme noktasına geç
            if (!XList.Contains(ColorMatchPoints [n].x)) { 
                XList.Add(ColorMatchPoints [n].x);
            }
            else continue;

            //Eşleşme koordinatlarını al
            int pointX = ColorMatchPoints[n].x;
            int pointY = ColorMatchPoints[n].y;
            //Debug.Log(ColorMatchPoints [n]);
            int targetTile = pointY+1; //Koordinatın üstündeki ilk altıgen
            int emptyTile; //Boşluk oluşan koordinat

            //Renk eşleşmesi olan pozisyonunun üstündeki her altıgen için çalıştır
            for (emptyTile = pointY; targetTile < GridSize.y; emptyTile++) { 

                //Üstündeki her bir boşluk için targetTile'ı bir birim arttır
                while (GameGrid [pointX,targetTile].Empty) {
                    targetTile++;
                    if (targetTile == GridSize.y) { //Üstünde başka altıgen yok
                        //Debug.Log("Mesafe aşıldı");
                        emptyTile--; //Bunu neden yaptığımı ben de bilmiyorum, denedim
                        break;
                    }
                }

                //Üstündeki her bir altıgeni aşağı çek, grid içindeki bilgilerini ayarla
                if (targetTile < GridSize.y) {
                    GameGrid [pointX,targetTile].TileObject.transform.position = GetWorldPosition(pointX,emptyTile);
                    GameGrid [pointX,targetTile].Empty = true;

                    GameGrid [pointX,emptyTile].Empty = false;
                    GameGrid [pointX,emptyTile].TileObject = GameGrid [pointX,targetTile].TileObject;
                    GameGrid [pointX,emptyTile].TileColor = GameGrid [pointX,targetTile].TileColor;
                    GameGrid [pointX,emptyTile].TileType = GameGrid [pointX,targetTile].TileType;
                    GameGrid [pointX,emptyTile].TileWorldPosition = GameGrid [pointX,targetTile].TileWorldPosition;
                    targetTile++;
                    yield return new WaitForSeconds(debugWaitingTime);
                }
                else break;
            }

            //Aşağı çekilen tilelar bittiğinde, boşlukları yeni altıgenler ile doldur
            var v = 0;
            while(!GameGrid [pointX,v].Empty) {
                v++;
            }
            for(var w = v; w < GridSize.y; w++) { 
                CreateNewTiles(pointX,w);
                yield return new WaitForSeconds(debugWaitingTime);
            }
        }
        //Gerekli altıgenlerin aşağı inmesi ve yenilerinin yaratılması bittiğinde, tekrar eşleşme var mı diye kontrol et
        if (!ColorMatchDetection()) {
            SelectTileObjects(); //Yeri değişen ve yeni yaratılan altıgenler, yeni bir eşleşme yarattı mı?
            Swipe = false; //Her şey bittiğinde oyuncuya tekrar swipe yapabilme hakkını ver
            BombCountdown(); //Bombaların sayacını çalıştır
            AnyMovesLeft(); //Yapılabilecek hareket kaldı mı?
        }
        else {
            DestroyTiles(); //Tekrar eşleşme varsa eşleşen altıgenleri yok et
        }
    }

    //Yeni altıgen yarat
    void CreateNewTiles(int pointX,int pointY)
    {
        GameObject tileObj = Instantiate(Tile,GetWorldPosition(pointX,pointY),Quaternion.identity,TileParent) as GameObject;

        //Rastgele bir renk ata
        int rnd = Random.Range(0,TileColors.Length);
        Color col = new Color(TileColors[rnd].r,TileColors[rnd].g,TileColors[rnd].b);

        //Yeni yaratılan altıgenin bilgilerini grid arrayine doldur
        GameGrid [pointX,pointY].TileColor = col;
        GameGrid [pointX,pointY].TileObject = tileObj;
        GameGrid [pointX,pointY].TileWorldPosition = tileObj.transform.position;
        GameGrid [pointX,pointY].Empty = false;

        tileObj.GetComponent<TilePosition>().TilePositionVector = new Vector2Int(pointX,pointY); //Editörden altıgenlerin pozisyonu görünebilsin
        
        string _tileType;

        //Yıldızlı altıgen oluşma ihtimali için rastgele bir sayı seç
        int starRnd = Random.Range(0,100);


        if (Score >= BombMinimumScore*(BombCount+1) && Bombs.Count == 0) {
            BombCount++;
            _tileType = "Bomb";
            tileObj.GetComponent<SpriteRenderer>().sprite = BombSprite;
            GameObject bomb = Instantiate(BombUIObject,tileObj.transform.position,Quaternion.identity,tileObj.transform);
            Text bombText = bomb.transform.GetChild(0).GetComponent<Text>();
            bombText.text = BombCountdownMax.ToString();
            Bombs.Add(new BombList(new Vector2Int(pointX,pointY),BombCountdownMax,bombText)); //Bombanın grid pozisyonu ve sayacını listeye ekle
        }
        else if (starRnd < StarPercent) {
            _tileType = "Star";
            tileObj.GetComponent<SpriteRenderer>().sprite = StarSprite;
        }
        else {
            _tileType = "Standart";
        }
        GameGrid [pointX,pointY].TileType = _tileType;
        tileObj.GetComponent<SpriteRenderer>().color = col;
    }

    //Editörden altıgenlerin boyutunun ayarlanabilmesi için gereken fonksiyon
    void ScaleTiles()
    {
        if (TileScale != previousTileScale) {
            TileParent.transform.localScale = new Vector3(TileScale,TileScale,TileScale);
            previousTileScale = TileScale;
        }
    }


    //Altıgenler patladığında oluşan partikül efekti
    IEnumerator ParticleEffect (int i)
    {
        yield return new WaitForSeconds(0.1f);
        var Part = Instantiate(Particle,GameGrid [ColorMatchPoints [i].x,ColorMatchPoints [i].y].TileWorldPosition,Quaternion.Euler(new Vector3(0f,0f,180f)),ParticleParent);
        Color col = GameGrid [ColorMatchPoints [i].x,ColorMatchPoints [i].y].TileColor;
        ParticleSystem PartSys = Part.GetComponent<ParticleSystem>();
        var main = PartSys.main;
        main.startColor = col;

    }

    //Bomba varsa geri sayımı çalıştır
    void BombCountdown ()
    {
        if (Bombs.Count != 0) {
            for(var c = 0; c < Bombs.Count; c++) {
                if (Bombs[c].BombCountdown > 0) {
                    Bombs [c].BombText.text = (--Bombs [c].BombCountdown).ToString();
                    if (Bombs[c].BombCountdown == 0) { //Bombanın sayacı biterse oyunu bitir
                        GameOver();
                    }
                    //Debug.Log(Bombs [c].BombCountdown);
                }
            }
        }
    }

    bool AnyMovesLeft()
    {
        bool boolPos;
        for (var j = 0; j < GridSize.y; j++) {
            for (var i = 0; i < GridSize.x; i++) {
                boolPos = (i % 2 == 0); //Altıgenler her iki yatay birimde bir, dikeyde aşağı kayıyor
                if ((i != 0 && j != GridSize.y-1) || (!boolPos && j == GridSize.y-1)) {
                    if (GameGrid[i,j].TileColor == GameGrid[i - 1,j + (boolPos ? 1 : 0)].TileColor) { //Sol üstündeki ile aynı renk mi?
                        if (MoveCheck(i,j,-2,0)) return true;
                        if (MoveCheck(i,j,-2,-1)) return true;
                        if (MoveCheck(i,j,-1,-2 + (boolPos ? 1 : 0))) return true;
                        if (MoveCheck(i,j,0,-1)) return true;

                        if (MoveCheck(i,j,-1,1 + (boolPos ? 1 : 0))) return true;
                        if (MoveCheck(i,j,0,+2)) return true;
                        if (MoveCheck(i,j,1,1 + (boolPos ? 1 : 0))) return true;
                        if (MoveCheck(i,j,1,(boolPos ? 1 : 0))) return true;
                    }
                }
                if (j != GridSize.y - 1) {
                    if (GameGrid [i,j].TileColor == GameGrid [i,j + 1].TileColor) { //Bir üstündeki ile aynı renk mi?
                        if (MoveCheck(i,j,-1,1 + (boolPos ? 0 : 0))) return true;
                        if (MoveCheck(i,j,-2,1)) return true;
                        if (MoveCheck(i,j,-2,0)) return true;
                        if (MoveCheck(i,j,-1,(boolPos ? 0 : -1))) return true;

                        if (MoveCheck(i,j,1,1 + (boolPos ? 1 : 0))) return true;
                        if (MoveCheck(i,j,2,1)) return true;
                        if (MoveCheck(i,j,2,0)) return true;
                        if (MoveCheck(i,j,1,(boolPos ? 0 : -1))) return true;
                    }
                }
                if (i != GridSize.x-1 && (j != GridSize.y-1 || (!boolPos && j == GridSize.y-1))) {
                    if (GameGrid[i,j].TileColor == GameGrid[i + 1,j + (boolPos ? 1 : 0)].TileColor) { //Sağ üstündeki ile aynı renk mi?
                        if (MoveCheck(i,j,-1,1 + (boolPos ? 0 : -1))) return true;
                        if (MoveCheck(i,j,-1,2 + (boolPos ? 0 : -1))) return true;
                        if (MoveCheck(i,j,0,2)) return true;
                        if (MoveCheck(i,j,1,2 + (boolPos ? 0 : -1))) return true;

                        if (MoveCheck(i,j,0,-1)) return true;
                        if (MoveCheck(i,j,1,-1 + (boolPos ? 0 : -1))) return true;
                        if (MoveCheck(i,j,2,-1)) return true;
                        if (MoveCheck(i,j,2,0)) return true;
                    }
                }
            }
        }
        Debug.Log("No moves left!");
        return false;
    }

    bool MoveCheck(int i, int j, int newX, int newY)
    {
        if (i + newX >= 0 && i + newX < GridSize.x && j + newY >= 0 && j + newY < GridSize.y) {
            if (GameGrid[i,j].TileColor == GameGrid[i + newX,j + newY].TileColor) {
                return true;
            }
        }
        return false;
    }

    //Oyunu bitiren fonksiyon
    void GameOver()
    {
        MovementPermission = false;
        Instantiate(GameOverObject,CanvasObject.position,Quaternion.identity,CanvasObject);
    }

    //Üçlü seçimdeki objeleri SelectedTileObjects arrayine koy / yenile
    void SelectTileObjects()
    {
        for (var s = 0; s < SelectedTiles.Length; s++) {
            SelectedTileObjects [s] = GameGrid [SelectedTiles [s].x,SelectedTiles [s].y].TileObject;
            var Pos = SelectedTileObjects [s].transform.position;
            SelectedTileObjects [s].transform.position = new Vector3(Pos.x,Pos.y,-1f);
        }
    }
}