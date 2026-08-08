using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// EVI KURAR — menuden: ALEV → Evi Kur
///
/// ODA IZGARASI: her oda tam bir ekran. Oda yuksekligi (12) kameranin
/// gorus yuksekligine esit, bu yuzden zeminin alti veya evin disi ASLA
/// gorunmez. Kapidan gecince kamera yeni odaya kayar.
///
/// 12 oda, 2 sira:
///   Alt sira (arama, soldan saga):  Antre · Salon · Mutfak · Koridor · Banyo · Cocuk
///   Ust sira (kacis, sagdan sola):  TavanA..TavanF
///
/// Iki dikey gecis:
///   x 148-156 → tirmanis (Cocuk odasindan TavanA'ya)
///   x   4-14  → inis     (TavanF'ten Antre'ye, boss oraya iniyorsunuz)
/// </summary>
public static class AlevLevelBuilder
{
    // ---- Oda izgarasi ----
    const float RoomW = 26f;
    const float RoomH = 12f;          // = kamera ortho size (6) x 2
    const float Row0Y = 6f;           // alt sira oda merkezi → zemin ust yuzeyi y=0
    const float Row1Y = 18f;          // ust sira oda merkezi → zemin ust yuzeyi y=12.5
    const float LevelW = 156f;        // 6 oda x 26

    const float GroundTop = 0f;
    const float SlabY = 12f;          // katlar arasi doseme (11.5 - 12.5)
    const float AtticTop = 12.5f;
    const float RoofY = 24.5f;

    const float DoorHeight = 3.2f;

    // Dikey gecis saftlari
    const float ClimbHoleMin = 148f, ClimbHoleMax = 156f;
    const float DropHoleMin = 4f,   DropHoleMax = 14f;

    static Sprite square;
    static Transform rooms, geometry, platforms, water, debris, triggers, markers;

    static readonly Color ColWall     = new Color(0.13f, 0.11f, 0.10f);
    static readonly Color ColFloor    = new Color(0.16f, 0.13f, 0.12f);
    static readonly Color ColPlatform = new Color(0.26f, 0.21f, 0.18f);
    static readonly Color ColDoor     = new Color(0.32f, 0.20f, 0.13f);

    [MenuItem("ALEV/Evi Kur", false, 1)]
    public static void BuildHouse()
    {
        if (!EditorUtility.DisplayDialog(
                "Evi kur",
                "Sahnedeki 'Level' objesi silinip yeniden kurulacak.\n" +
                "12 oda, oda basina bir ekran, kapi gecisleri.\n\n" +
                "Player ve HUD etkilenmez. Kamera RoomCamera'ya cevrilir.\n\nDevam?",
                "Kur", "Vazgec"))
            return;

        square = AlevSceneSetup.GetOrCreateSquareSprite();

        GameObject old = GameObject.Find("Level");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("Level").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Evi kur");

        rooms     = NewChild(root, "Rooms");
        geometry  = NewChild(root, "Geometry");
        platforms = NewChild(root, "Platforms");
        water     = NewChild(root, "WaterSources");
        debris    = NewChild(root, "FallingDebris");
        triggers  = NewChild(root, "Triggers");
        markers   = NewChild(root, "SpawnMarkers");

        BuildRooms();
        BuildShell();
        BuildDoors();
        BuildPlatforms();
        BuildWater();
        BuildDebris();
        BuildTriggers();
        BuildMarkers();
        SetupCamera();
        MovePlayerToStart();

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<b>[ALEV]</b> Ev kuruldu — 12 oda, oda basina bir ekran.\n" +
                  "Tirmanis: Cocuk odasi sagi (x 148-156) · Inis: TavanF (x 4-14)\n" +
                  "⚠️ Once dusmansiz tur atin: Antre'den Cocuk odasina, sonra tavan " +
                  "arasindan geri Antre'ye kadar yuruyup ziplayin.");
    }

    // ==================================================================
    //  Odalar
    // ==================================================================

    static readonly (string name, int col, int row)[] RoomList =
    {
        ("Antre",   0, 0), ("Salon",  1, 0), ("Mutfak", 2, 0),
        ("Koridor", 3, 0), ("Banyo",  4, 0), ("CocukOdasi", 5, 0),

        ("TavanF",  0, 1), ("TavanE", 1, 1), ("TavanD", 2, 1),
        ("TavanC",  3, 1), ("TavanB", 4, 1), ("TavanA", 5, 1),
    };

    static float ColCenterX(int col) => col * RoomW + RoomW * 0.5f;
    static float RowCenterY(int row) => row == 0 ? Row0Y : Row1Y;

    static void BuildRooms()
    {
        foreach (var (name, col, row) in RoomList)
        {
            var go = new GameObject("Oda_" + name);
            go.transform.SetParent(rooms, false);
            go.transform.position = new Vector3(ColCenterX(col), RowCenterY(row), 0f);

            var r = go.AddComponent<Room>();
            r.roomName = name;
            r.size = new Vector2(RoomW, RoomH);
        }
    }

    // ==================================================================
    //  Kabuk: zemin, doseme, cati, yan duvarlar
    // ==================================================================

    static void BuildShell()
    {
        // Alt kat zemini
        Solid(geometry, "Zemin", new Vector2(LevelW * 0.5f, GroundTop - 0.5f),
              new Vector2(LevelW, 1f), ColFloor);

        // Katlar arasi doseme — iki saftta BOSLUK var
        Solid(geometry, "Doseme_Sol", new Vector2(DropHoleMin * 0.5f, SlabY),
              new Vector2(DropHoleMin, 1f), ColFloor);
        Solid(geometry, "Doseme_Orta",
              new Vector2((DropHoleMax + ClimbHoleMin) * 0.5f, SlabY),
              new Vector2(ClimbHoleMin - DropHoleMax, 1f), ColFloor);
        // x 148-156 arasi bilerek bos — tirmanis oradan

        // Cati
        Solid(geometry, "Cati", new Vector2(LevelW * 0.5f, RoofY),
              new Vector2(LevelW, 1f), ColWall);

        // Yan duvarlar
        Solid(geometry, "SolDuvar", new Vector2(-0.5f, 12f), new Vector2(1f, 26f), ColWall);
        Solid(geometry, "SagDuvar", new Vector2(LevelW + 0.5f, 12f), new Vector2(1f, 26f), ColWall);

        // Oda zeminleri — hafif renk farkiyla odalar okunsun (collider yok)
        var tints = new[]
        {
            new Color(0.20f, 0.16f, 0.14f), new Color(0.23f, 0.17f, 0.15f),
            new Color(0.19f, 0.18f, 0.16f), new Color(0.15f, 0.12f, 0.12f),
            new Color(0.17f, 0.18f, 0.19f), new Color(0.24f, 0.19f, 0.16f),
        };

        for (int c = 0; c < 6; c++)
        {
            Tint($"Zemin_Alt_{c}", new Vector2(ColCenterX(c), GroundTop + 0.06f),
                 new Vector2(RoomW, 0.12f), tints[c]);
            Tint($"Zemin_Ust_{c}", new Vector2(ColCenterX(c), AtticTop + 0.06f),
                 new Vector2(RoomW, 0.12f), tints[5 - c] * 0.85f);
        }
    }

    // ==================================================================
    //  Oda arasi duvarlar + kapilar
    // ==================================================================

    static void BuildDoors()
    {
        // Odalar arasindaki sinirlar: x = 26, 52, 78, 104, 130
        for (int c = 1; c < 6; c++)
        {
            float bx = c * RoomW;

            // ---- Alt sira ----
            // Kapi bosluu y 0 - 3.2, ustu duvar
            float wallBottom = GroundTop + DoorHeight;
            float wallTop = SlabY - 0.5f;
            Solid(geometry, $"Duvar_Alt_{c}",
                  new Vector2(bx, (wallBottom + wallTop) * 0.5f),
                  new Vector2(0.6f, wallTop - wallBottom), ColWall);
            DoorFrame($"Kapi_Alt_{c}", new Vector2(bx, GroundTop + DoorHeight * 0.5f));

            // ---- Ust sira ----
            float aWallBottom = AtticTop + DoorHeight;
            float aWallTop = RoofY - 0.5f;
            Solid(geometry, $"Duvar_Ust_{c}",
                  new Vector2(bx, (aWallBottom + aWallTop) * 0.5f),
                  new Vector2(0.6f, aWallTop - aWallBottom), ColWall);
            DoorFrame($"Kapi_Ust_{c}", new Vector2(bx, AtticTop + DoorHeight * 0.5f));
        }
    }

    /// <summary>Kapi cercevesi — sadece gorsel, collider yok, gecilebilir.</summary>
    static void DoorFrame(string name, Vector2 pos)
    {
        var go = NewSprite(geometry, name, pos, new Vector2(1.4f, DoorHeight), ColDoor);
        go.GetComponent<SpriteRenderer>().sortingOrder = -1;
    }

    // ==================================================================
    //  Platformlar
    // ==================================================================

    static void BuildPlatforms()
    {
        // ⚠️ ZIPLAMA LIMITI: jumpForce 15 + gravityScale 4 → maksimum ~2.87 birim.
        // Platformlar arasi fark 1.8'i gecmesin.

        // Antre
        Plat("Antre_Sandik", 20f, 1.8f, 3f);

        // Salon — TEK dikey oda
        Plat("Salon_Sehpa",   31f, 1.8f, 4f);
        Plat("Salon_Kitaplik",37f, 3.6f, 4f);
        Plat("Salon_Raf",     43f, 5.4f, 4f);
        Plat("Salon_Ust",     37f, 7.2f, 4f);
        Plat("Salon_Tavan",   45f, 9.0f, 4f);

        // Mutfak
        Plat("Mutfak_Tezgah", 60f, 1.8f, 6f);
        Plat("Mutfak_Dolap",  70f, 3.6f, 4f);

        // Koridor
        Plat("Koridor_Dolap", 86f, 1.8f, 3f);
        Plat("Koridor_Kiris", 96f, 3.6f, 3f);

        // Banyo
        Plat("Banyo_Kuvet",  112f, 1.8f, 4f);
        Plat("Banyo_Raf",    123f, 3.6f, 3f);

        // Cocuk odasi + TIRMANIS (saft x 148-156)
        Plat("Cocuk_Ranza",  136f, 1.8f, 5f);
        Plat("Tirmanis_1",   151f, 1.8f, 3f);
        Plat("Tirmanis_2",   154f, 3.6f, 3f);
        Plat("Tirmanis_3",   151f, 5.4f, 3f);
        Plat("Tirmanis_4",   154f, 7.2f, 3f);
        Plat("Tirmanis_5",   151f, 9.0f, 3f);
        Plat("Tirmanis_6",   152f, 10.8f, 3f);
        // Buradan doseme kenarina (x=148, ust yuzey 12.5) atlanir: 1.45 birim

        // Tavan arasi platformlari — cesitlilik icin
        Plat("TavanB_Kiris", 118f, 14.3f, 4f);
        Plat("TavanD_Kiris",  64f, 14.3f, 4f);
        Plat("TavanE_Kiris",  40f, 14.3f, 4f);
    }

    static void Plat(string name, float x, float y, float w)
        => Solid(platforms, name, new Vector2(x, y), new Vector2(w, 0.5f), ColPlatform);

    // ==================================================================
    //  Su kaynaklari
    // ==================================================================

    static void BuildWater()
    {
        // Salon — bonus, tek kullanimlik
        Tap("Salon_Akvaryum", new Vector2(29f, 0.5f), WaterSource.SourceType.OneShot, 100f,
            new Color(0.15f, 0.85f, 0.75f), new Vector2(1.6f, 1f));

        // Mutfak — BOLLUK odasi
        Tap("Mutfak_Musluk", new Vector2(56f, 0.75f), WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), new Vector2(1f, 1.5f));

        // Koridor — KASITLI SUSUZ. Yer atesi de yok, sadece duvar canavari.

        // Banyo
        Tap("Banyo_Kuvet_Su", new Vector2(108f, 0.5f), WaterSource.SourceType.Infinite, 50f,
            new Color(0.25f, 0.60f, 0.95f), new Vector2(3f, 1f));

        // ⚠️ TavanB — kacis yolundaki musluk. OLUMDEN ONCE, oyuncu dolu girsin.
        Tap("TavanB_Musluk", new Vector2(124f, 13.25f), WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), new Vector2(1f, 1.5f));

        // TavanD — yalniz bolumde acil dolum. Tek kullanimlik, gerilim bozulmasin.
        Tap("TavanD_Bidon", new Vector2(60f, 13f), WaterSource.SourceType.OneShot, 50f,
            new Color(0.20f, 0.70f, 0.85f), new Vector2(1f, 1f));
    }

    static void Tap(string name, Vector2 pos, WaterSource.SourceType type,
                    float rate, Color color, Vector2 scale)
    {
        var go = NewSprite(water, name, pos, scale, color);
        go.tag = "WaterSource";

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.5f / Mathf.Max(scale.x, 0.01f),
                               2.5f / Mathf.Max(scale.y, 0.01f));

        var src = go.AddComponent<WaterSource>();
        src.type = type;
        if (type == WaterSource.SourceType.Infinite) src.refillPerSecond = rate;
        else { src.instantAmount = rate; src.destroyAfterUse = true; }
    }

    // ==================================================================
    //  Dusen tahtalar
    // ==================================================================

    static readonly (Vector2 pos, string note)[] DebrisSpots =
    {
        (new Vector2( 40f, 10.8f), "Salon - ILK KARSILASMA, genis ve guvenli"),
        (new Vector2( 68f, 10.8f), "Mutfak"),
        (new Vector2( 91f, 10.8f), "Koridor - dar, gerilim yuksek"),
        (new Vector2(120f, 10.8f), "Banyo"),
        (new Vector2(112f, 23.3f), "TavanB"),
        (new Vector2( 70f, 23.3f), "TavanD - yalniz bolum"),
        (new Vector2( 44f, 23.3f), "TavanE - yalniz bolum"),
    };

    static void BuildDebris()
    {
        var beam = new Color(0.45f, 0.28f, 0.15f);
        var mark = new Color(1f, 0.45f, 0.1f, 0.9f);

        for (int i = 0; i < DebrisSpots.Length; i++)
        {
            var (pos, note) = DebrisSpots[i];

            var go = NewSprite(debris, $"Tahta_{i + 1:00} ({note.Split('-')[0].Trim()})",
                               pos, new Vector2(2.5f, 0.4f), beam);
            go.AddComponent<BoxCollider2D>().isTrigger = true;

            var gm = NewSprite(go.transform, "YerIsareti", pos, new Vector2(2.2f, 0.25f), mark);
            gm.GetComponent<SpriteRenderer>().sortingOrder = 2;
            gm.SetActive(false);

            var fd = go.AddComponent<FallingDebris>();
            fd.groundLayer = LayerMask.GetMask("Ground");
            fd.groundMarker = gm;
            fd.warningDuration = 1f;
            fd.triggerHeight = 11f;   // oda yuksekligi kadar
            fd.autoTrigger = true;
        }

        // ---- OLUM TAHTASI ---- TavanC, autoTrigger KAPALI
        var death = NewSprite(debris, "Tahta_OLUM (EndingSequence -> Death Debris)",
                              new Vector2(91f, 23.3f), new Vector2(3f, 0.5f),
                              new Color(0.5f, 0.3f, 0.16f));
        death.AddComponent<BoxCollider2D>().isTrigger = true;

        var dfd = death.AddComponent<FallingDebris>();
        dfd.groundLayer = LayerMask.GetMask("Ground");
        dfd.autoTrigger = false;      // ⚠️ sadece EndingSequence dusurur
        dfd.groundMarker = null;      // uyari gorseli YOK
        dfd.lifetimeAfterLanding = 0f;
    }

    // ==================================================================
    //  Tetikleyiciler
    // ==================================================================

    static void BuildTriggers()
    {
        // Karabas'in olumu — TavanC ortasi
        Zone("Tetik_KopeginOlumu", new Vector2(91f, 14f), new Vector2(2f, 4f),
             EndingTriggerZone.What.DogDeath);

        // Boss — TavanF, inis saftina girmeden hemen once.
        // Oyuncu tetikler, sonra asagi duser ve boss'u karsisinda bulur.
        Zone("Tetik_BossSahnesi", new Vector2(18f, 14f), new Vector2(2f, 4f),
             EndingTriggerZone.What.BossScene);
    }

    static void Zone(string name, Vector2 pos, Vector2 size, EndingTriggerZone.What what)
    {
        var go = new GameObject(name);
        go.transform.SetParent(triggers, false);
        go.transform.position = pos;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        var z = go.AddComponent<EndingTriggerZone>();
        z.trigger = what;
        z.oneShot = true;
    }

    // ==================================================================
    //  Kisi B'nin prefablari icin isaretler
    // ==================================================================

    static readonly (SpawnMarker.Kind kind, Vector2 pos, string note)[] MarkerSpots =
    {
        (SpawnMarker.Kind.PlayerStart,   new Vector2(  5f, 1f), "Oyuncu baslangici"),
        (SpawnMarker.Kind.FlameWall,     new Vector2(  2f, 1f), "Giris alev duvari. Karabas kurtulunca GORUNUR sekilde yukselir."),
        (SpawnMarker.Kind.Boss,          new Vector2(  4f, 1f), "Dev alev. Oyuncu tavandan buraya iniyor. Cani hic azalmaz."),
        (SpawnMarker.Kind.DogSilhouette, new Vector2( 12f, 1f), "Kopek mamasi/fotograf. Oda kamerasiyla uzaktan siluet gorunmuyor, " +
                                                                "onun yerine 'burada bir kopek yasiyor' ipucu. Havlama sesi isi yapiyor."),

        // ---- Duvar canavarlari ----
        (SpawnMarker.Kind.WallMonster, new Vector2( 16f, 1f), "Antre - ilk, ZAYIF (can 40)"),
        (SpawnMarker.Kind.WallMonster, new Vector2( 45f, 7f), "Salon - YUKSEK, yukari nisan gerektirir"),
        (SpawnMarker.Kind.WallMonster, new Vector2( 72f, 3f), "Mutfak dolabi"),
        (SpawnMarker.Kind.WallMonster, new Vector2( 84f, 1f), "Koridor 1/3 - su 2'sine yeter"),
        (SpawnMarker.Kind.WallMonster, new Vector2( 91f, 5f), "Koridor 2/3 - YUKSEK"),
        (SpawnMarker.Kind.WallMonster, new Vector2( 99f, 1f), "Koridor 3/3"),
        (SpawnMarker.Kind.WallMonster, new Vector2(116f, 4f), "Banyo - YUKSEK"),
        (SpawnMarker.Kind.WallMonster, new Vector2(126f, 1f), "Banyo"),
        (SpawnMarker.Kind.WallMonster, new Vector2(118f,16f), "TavanB"),
        (SpawnMarker.Kind.WallMonster, new Vector2( 58f,16f), "TavanD - yalniz bolum"),

        // ---- Alt kat yer atesleri (Koridor'a KONMADI: kasitli susuz bolge) ----
        (SpawnMarker.Kind.FloorFire, new Vector2(113f, 0.5f), "Banyo"),
        (SpawnMarker.Kind.FloorFire, new Vector2(124f, 0.5f), "Banyo"),

        // ---- Kacis: Karabas'li bolum (SEYREK, kopek sonduruyor) ----
        (SpawnMarker.Kind.FloorFire, new Vector2(150f, 13f), "TavanA - Karabas'li"),
        (SpawnMarker.Kind.FloorFire, new Vector2(138f, 13f), "TavanA - Karabas'li"),
        (SpawnMarker.Kind.FloorFire, new Vector2(126f, 13f), "TavanB - Karabas'li"),
        (SpawnMarker.Kind.FloorFire, new Vector2(112f, 13f), "TavanB - Karabas'li"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 98f, 13f), "TavanC - Karabas'li, olumden hemen once"),

        // ---- Kacis: YALNIZ bolum (SIK - yari mesafe, sifir yardim) ----
        (SpawnMarker.Kind.FloorFire, new Vector2( 76f, 13f), "YALNIZ - sik araliklarin sebebi kaybi hissettirmek"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 71f, 13f), "YALNIZ"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 66f, 13f), "YALNIZ"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 56f, 13f), "YALNIZ"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 48f, 13f), "YALNIZ"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 43f, 13f), "YALNIZ"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 38f, 13f), "YALNIZ"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 33f, 13f), "YALNIZ"),
        (SpawnMarker.Kind.FloorFire, new Vector2( 22f, 13f), "TavanF - son"),

        (SpawnMarker.Kind.Dog,          new Vector2(136f, 1f), "KARABAS - sikismis halde baslar"),
        (SpawnMarker.Kind.CoveringFire, new Vector2(136f, 1f), "Karabas'i orten ates. Sondurulunce kopek kurtulur (can 50)."),
    };

    static void BuildMarkers()
    {
        foreach (var (kind, pos, note) in MarkerSpots)
        {
            var go = new GameObject($"[{kind}] {note.Split('-')[0].Trim()}");
            go.transform.SetParent(markers, false);
            go.transform.position = pos;

            var m = go.AddComponent<SpawnMarker>();
            m.kind = kind;
            m.note = note;
        }
    }

    // ==================================================================
    //  Kamera ve oyuncu
    // ==================================================================

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[ALEV] Main Camera yok — once 'Test Sahnesini Kur' calistirin.");
            return;
        }

        cam.orthographic = true;
        cam.orthographicSize = RoomH * 0.5f;   // oda yuksekligi = ekran yuksekligi

        Transform rig = cam.transform.parent;
        if (rig == null)
        {
            Debug.LogWarning("[ALEV] CameraRig yok — once 'Test Sahnesini Kur' calistirin.");
            return;
        }

        // Kaydirmali kamerayi kapat, oda kamerasina gec
        var follow = rig.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = false;

        var roomCam = rig.GetComponent<RoomCamera>();
        if (roomCam == null) roomCam = rig.gameObject.AddComponent<RoomCamera>();
        roomCam.enabled = true;
    }

    static void MovePlayerToStart()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.transform.position = new Vector3(5f, 1f, 0f);
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================

    static Transform NewChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject NewSprite(Transform parent, string name, Vector2 pos,
                                Vector2 scale, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        return go;
    }

    static void Tint(string name, Vector2 pos, Vector2 scale, Color color)
    {
        var go = NewSprite(geometry, name, pos, scale, color);
        go.GetComponent<SpriteRenderer>().sortingOrder = 1;
    }

    static GameObject Solid(Transform parent, string name, Vector2 pos,
                            Vector2 scale, Color color)
    {
        var go = NewSprite(parent, name, pos, scale, color);
        go.layer = LayerMask.NameToLayer("Ground");
        go.AddComponent<BoxCollider2D>();
        return go;
    }
}
