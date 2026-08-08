using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// EVI KURAR — menuden: ALEV → Evi Kur
///
/// KAT_PLANI.md'deki butun koordinatlari sahneye doker: zemin, tavan,
/// platformlar, su kaynaklari, dusen tahtalar, tetikleyici bolgeler,
/// kamera sinirlari.
///
/// Kisi B'nin dusmanlari henuz prefab olmadigi icin onlarin yerine
/// SpawnMarker koyuyor — prefablar gelince isaretlerin uzerine surukleyip
/// isaretleri silersiniz.
///
/// Tekrar calistirilabilir: "Level" objesini silip sifirdan kurar.
/// </summary>
public static class AlevLevelBuilder
{
    // ---- KAT_PLANI.md'den gelen sabitler ----
    const float HouseWidth = 100f;
    const float GroundY = -0.5f;
    const float LowerCeilY = 7.5f;
    const float UpperFloorY = 8.5f;
    const float UpperCeilY = 15.5f;

    static Sprite square;
    static Transform geometry, platforms, water, debris, triggers, markers;

    [MenuItem("ALEV/Evi Kur", false, 1)]
    public static void BuildHouse()
    {
        if (!EditorUtility.DisplayDialog(
                "Evi kur",
                "Sahnedeki 'Level' objesi silinip KAT_PLANI.md'ye gore yeniden " +
                "kurulacak.\n\nPlayer, HUD ve kamera etkilenmez.\n\nDevam?",
                "Kur", "Vazgec"))
            return;

        square = AlevSceneSetup.GetOrCreateSquareSprite();

        GameObject old = GameObject.Find("Level");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("Level").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Evi kur");

        geometry  = NewChild(root, "Geometry");
        platforms = NewChild(root, "Platforms");
        water     = NewChild(root, "WaterSources");
        debris    = NewChild(root, "FallingDebris");
        triggers  = NewChild(root, "Triggers");
        markers   = NewChild(root, "SpawnMarkers");

        BuildGeometry();
        BuildPlatforms();
        BuildWaterSources();
        BuildDebris();
        BuildTriggers();
        BuildMarkers();
        SetCameraBounds();
        MovePlayerToStart();

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<b>[ALEV]</b> Ev kuruldu.\n" +
                  "Alt kat: soldan saga arama · Ust kat: sagdan sola kacis\n" +
                  "Kirmizi/turuncu kureler = Kisi B'nin prefablari buraya gelecek.\n" +
                  "⚠️ Once dusmansiz bir tur atin: bastan sona yuruyup ziplayin.");
    }

    // ==================================================================
    //  Zemin, tavan, oda zeminleri
    // ==================================================================

    static void BuildGeometry()
    {
        // Ana zemin
        Solid(geometry, "Zemin", new Vector2(50f, GroundY), new Vector2(HouseWidth, 1f),
              new Color(0.16f, 0.13f, 0.12f));

        // Alt kat tavani — x 88-100 arasi YOK, tirmanis oradan geciyor
        Solid(geometry, "AltKatTavan", new Vector2(44f, LowerCeilY), new Vector2(88f, 1f),
              new Color(0.13f, 0.11f, 0.10f));

        // Ust kat
        Solid(geometry, "UstKatZemin", new Vector2(60f, UpperFloorY), new Vector2(84f, 1f),
              new Color(0.16f, 0.13f, 0.12f));
        Solid(geometry, "UstKatTavan", new Vector2(60f, UpperCeilY), new Vector2(84f, 1f),
              new Color(0.13f, 0.11f, 0.10f));

        // Evin iki ucu — disari cikilmasin
        Solid(geometry, "SolDuvar", new Vector2(-0.5f, 3.5f), new Vector2(1f, 9f),
              new Color(0.12f, 0.10f, 0.09f));
        Solid(geometry, "SagDuvar", new Vector2(100.5f, 7f), new Vector2(1f, 17f),
              new Color(0.12f, 0.10f, 0.09f));

        // Oda zeminleri — hafif renk farkiyla odalar okunsun (collider yok, sadece gorsel)
        RoomFloor("Antre",         0f,  14f, new Color(0.20f, 0.16f, 0.14f));
        RoomFloor("Salon",        14f,  34f, new Color(0.23f, 0.17f, 0.15f));
        RoomFloor("Mutfak",       34f,  52f, new Color(0.19f, 0.18f, 0.16f));
        RoomFloor("Koridor",      52f,  62f, new Color(0.15f, 0.12f, 0.12f));
        RoomFloor("Banyo",        62f,  78f, new Color(0.17f, 0.18f, 0.19f));
        RoomFloor("NefesKoridoru",78f,  86f, new Color(0.21f, 0.19f, 0.17f));
        RoomFloor("CocukOdasi",   86f, 100f, new Color(0.24f, 0.19f, 0.16f));
    }

    static void RoomFloor(string name, float x0, float x1, Color c)
    {
        float w = x1 - x0;
        var go = NewSprite(geometry, "Oda_" + name, new Vector2(x0 + w * 0.5f, 0.05f),
                           new Vector2(w, 0.12f), c);
        go.GetComponent<SpriteRenderer>().sortingOrder = 1;
    }

    // ==================================================================
    //  Platformlar
    // ==================================================================

    static void BuildPlatforms()
    {
        var c = new Color(0.26f, 0.21f, 0.18f);

        // Salon — TEK dikey oda burasi
        Solid(platforms, "Salon_Sehpa",       new Vector2(18f, 1.5f), new Vector2(4f, 0.5f), c);
        Solid(platforms, "Salon_KitaplikUstu",new Vector2(23f, 3.5f), new Vector2(4f, 0.5f), c);
        Solid(platforms, "Salon_Raf",         new Vector2(28f, 5.5f), new Vector2(4f, 0.5f), c);

        Solid(platforms, "Mutfak_Tezgah",     new Vector2(38f, 2f),   new Vector2(6f, 0.5f), c);
        Solid(platforms, "Koridor_Dolap",     new Vector2(56f, 1.5f), new Vector2(3f, 0.5f), c);
        Solid(platforms, "Banyo_KuvetKenari", new Vector2(73f, 1.5f), new Vector2(4f, 0.5f), c);
        Solid(platforms, "Cocuk_Ranza",       new Vector2(90f, 2.5f), new Vector2(5f, 0.5f), c);

        // Tirmanis — cocuk odasindan ust kata
        Solid(platforms, "Tirmanis_1",        new Vector2(92f, 3f),   new Vector2(3f, 0.5f), c);
        Solid(platforms, "Tirmanis_2",        new Vector2(97f, 5f),   new Vector2(3f, 0.5f), c);
        Solid(platforms, "Tirmanis_3",        new Vector2(93f, 7f),   new Vector2(3f, 0.5f), c);
        Solid(platforms, "Tirmanis_UstGiris", new Vector2(96f, 8.5f), new Vector2(4f, 0.5f), c);

        // Inis — ust kattan cikis holune
        Solid(platforms, "Inis_1",            new Vector2(22f, 7f),   new Vector2(3f, 0.5f), c);
        Solid(platforms, "Inis_2",            new Vector2(18f, 4.5f), new Vector2(3f, 0.5f), c);
        Solid(platforms, "Inis_3",            new Vector2(21f, 2f),   new Vector2(3f, 0.5f), c);
    }

    // ==================================================================
    //  Su kaynaklari
    // ==================================================================

    static void BuildWaterSources()
    {
        Tap("Salon_Akvaryum", new Vector2(24f, 1f), WaterSource.SourceType.OneShot,  100f,
            new Color(0.15f, 0.85f, 0.75f), new Vector2(1.6f, 1f));

        Tap("Mutfak_Musluk",  new Vector2(42f, 1f), WaterSource.SourceType.Infinite,  30f,
            new Color(0.25f, 0.60f, 0.95f), new Vector2(1f, 1.5f));

        Tap("Banyo_Kuvet",    new Vector2(70f, 1f), WaterSource.SourceType.Infinite,  50f,
            new Color(0.25f, 0.60f, 0.95f), new Vector2(3f, 1f));

        // ⚠️ Kacis yolundaki musluk — bu olmadan oyuncu kilitlenir
        Tap("UstKat_Musluk",  new Vector2(50f, 9.5f), WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), new Vector2(1f, 1.5f));

        // Yalniz bolumde acil dolum — gerilimi bozmasin diye tek kullanimlik
        Tap("UstKat_Bidon",   new Vector2(30f, 9.5f), WaterSource.SourceType.OneShot,  50f,
            new Color(0.20f, 0.70f, 0.85f), new Vector2(1f, 1f));
    }

    static void Tap(string name, Vector2 pos, WaterSource.SourceType type,
                    float rate, Color color, Vector2 scale)
    {
        var go = NewSprite(water, name, pos, scale, color);
        go.tag = "WaterSource";

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        // Yaklasinca dolsun, tam ustune basmaya gerek kalmasin
        col.size = new Vector2(2.5f / Mathf.Max(scale.x, 0.01f), 2.5f / Mathf.Max(scale.y, 0.01f));

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
        (new Vector2(30f,  6.5f), "Salon — ILK KARSILASMA. Genis ve guvenli yer, ogretici."),
        (new Vector2(48f,  6.5f), "Mutfak cikisi"),
        (new Vector2(58f,  6.5f), "Koridor — dar yer, gerilim yuksek"),
        (new Vector2(72f,  6.5f), "Banyo"),
        (new Vector2(76f, 14.5f), "Ust kat, Karabas'li bolum"),
        (new Vector2(60f, 14.5f), "Ust kat"),
        (new Vector2(38f, 14.5f), "Ust kat — olumden hemen sonra, yalnizlik vurgusu"),
        (new Vector2(30f, 14.5f), "Ust kat, yalniz bolum"),
    };

    static void BuildDebris()
    {
        var beamColor   = new Color(0.45f, 0.28f, 0.15f);
        var markerColor = new Color(1f, 0.45f, 0.1f, 0.9f);

        for (int i = 0; i < DebrisSpots.Length; i++)
        {
            var (pos, note) = DebrisSpots[i];

            string shortNote = note.Split('—')[0].Trim();
            var go = NewSprite(debris, $"Tahta_{i + 1:00} ({shortNote})", pos,
                               new Vector2(2.5f, 0.4f), beamColor);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Yer isareti — dusecegi noktayi gosterir, EN ONEMLI uyari katmani.
            // Script raycast ile kendi konumlandiriyor, buradaki yer onemsiz.
            var gm = NewSprite(go.transform, "YerIsareti", pos, new Vector2(2.2f, 0.25f), markerColor);
            gm.GetComponent<SpriteRenderer>().sortingOrder = 2;
            gm.SetActive(false);

            var fd = go.AddComponent<FallingDebris>();
            fd.groundLayer = LayerMask.GetMask("Ground");
            fd.groundMarker = gm;
            fd.warningDuration = 1f;
            fd.autoTrigger = true;
        }
    }

    // ==================================================================
    //  Tetikleyiciler
    // ==================================================================

    static void BuildTriggers()
    {
        Zone("Tetik_KopeginOlumu", new Vector2(44f, 10.5f), new Vector2(2f, 4f),
             EndingTriggerZone.What.DogDeath);

        Zone("Tetik_BossSahnesi",  new Vector2(16f, 1.5f),  new Vector2(2f, 4f),
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
        (SpawnMarker.Kind.PlayerStart,   new Vector2( 4f, 1f),  "Oyuncu baslangici"),
        (SpawnMarker.Kind.DogSilhouette, new Vector2(13f, 3f),  "Antre'den gorunen Karabas silueti. Kurtarilinca gizlenir."),
        (SpawnMarker.Kind.FlameWall,     new Vector2( 1f, 1f),  "Giris alev duvari. Karabas kurtulunca GORUNUR sekilde yukselir."),
        (SpawnMarker.Kind.Boss,          new Vector2( 6f, 1f),  "Dev alev. Cani hic azalmaz."),

        // Duvar canavarlari — y=4/5 olanlar YUKARI NISAN aldirmak icin
        (SpawnMarker.Kind.WallMonster, new Vector2(10f, 1f),  "Antre — ilk, zayif (can 40)"),
        (SpawnMarker.Kind.WallMonster, new Vector2(20f, 5f),  "Salon — YUKSEK, yukari nisan gerektirir"),
        (SpawnMarker.Kind.WallMonster, new Vector2(46f, 3f),  "Mutfak dolabi"),
        (SpawnMarker.Kind.WallMonster, new Vector2(54f, 1f),  "Koridor 1/3 — su 2'sine yeter"),
        (SpawnMarker.Kind.WallMonster, new Vector2(57f, 5f),  "Koridor 2/3 — YUKSEK"),
        (SpawnMarker.Kind.WallMonster, new Vector2(60f, 1f),  "Koridor 3/3"),
        (SpawnMarker.Kind.WallMonster, new Vector2(65f, 4f),  "Banyo — YUKSEK"),
        (SpawnMarker.Kind.WallMonster, new Vector2(74f, 1f),  "Banyo"),
        (SpawnMarker.Kind.WallMonster, new Vector2(68f,12f),  "Ust kat, Karabas'li bolum"),
        (SpawnMarker.Kind.WallMonster, new Vector2(34f,12f),  "Ust kat, yalniz bolum"),

        // Alt kat yer atesleri — Koridor'a KOYULMADI (kilitlenme riski)
        (SpawnMarker.Kind.FloorFire, new Vector2(67f, 0.5f), "Banyo"),
        (SpawnMarker.Kind.FloorFire, new Vector2(76f, 0.5f), "Banyo"),

        // Ust kat, Karabas'li bolum — 8 birim arayla, kopek sonduruyor
        (SpawnMarker.Kind.FloorFire, new Vector2(88f, 9.5f), "Karabas'li bolum 1/6"),
        (SpawnMarker.Kind.FloorFire, new Vector2(80f, 9.5f), "Karabas'li bolum 2/6"),
        (SpawnMarker.Kind.FloorFire, new Vector2(72f, 9.5f), "Karabas'li bolum 3/6"),
        (SpawnMarker.Kind.FloorFire, new Vector2(64f, 9.5f), "Karabas'li bolum 4/6"),
        (SpawnMarker.Kind.FloorFire, new Vector2(56f, 9.5f), "Karabas'li bolum 5/6"),
        (SpawnMarker.Kind.FloorFire, new Vector2(48f, 9.5f), "Karabas'li bolum 6/6"),

        // Yalniz bolum — 4 birim arayla, YARI MESAFE, sifir yardim
        (SpawnMarker.Kind.FloorFire, new Vector2(40f, 9.5f), "YALNIZ 1/4 — sik araliklarin sebebi kaybi hissettirmek"),
        (SpawnMarker.Kind.FloorFire, new Vector2(36f, 9.5f), "YALNIZ 2/4"),
        (SpawnMarker.Kind.FloorFire, new Vector2(32f, 9.5f), "YALNIZ 3/4"),
        (SpawnMarker.Kind.FloorFire, new Vector2(28f, 9.5f), "YALNIZ 4/4"),

        (SpawnMarker.Kind.Dog,          new Vector2(96f, 1f), "KARABAS — sikismis halde baslar"),
        (SpawnMarker.Kind.CoveringFire, new Vector2(96f, 1f), "Karabas'i orten ates. Sondurulunce kopek kurtulur (can 50)."),
    };

    static void BuildMarkers()
    {
        foreach (var (kind, pos, note) in MarkerSpots)
        {
            var go = new GameObject($"[{kind}] {note.Split('—')[0].Trim()}");
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

    static void SetCameraBounds()
    {
        var follow = Object.FindFirstObjectByType<CameraFollow>();
        if (follow == null)
        {
            Debug.LogWarning("[ALEV] CameraFollow bulunamadi — once 'Test Sahnesini Kur' calistirin.");
            return;
        }

        follow.useBounds = true;
        follow.minBounds = new Vector2(10f, 2f);
        follow.maxBounds = new Vector2(90f, 10f);
    }

    static void MovePlayerToStart()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.transform.position = new Vector3(4f, 1f, 0f);
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

    static GameObject NewSprite(Transform parent, string name, Vector2 pos, Vector2 scale, Color color)
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

    /// <summary>Uzerine basilabilen kati nesne — Ground layer'inda.</summary>
    static GameObject Solid(Transform parent, string name, Vector2 pos, Vector2 scale, Color color)
    {
        var go = NewSprite(parent, name, pos, scale, color);
        go.layer = LayerMask.NameToLayer("Ground");
        go.AddComponent<BoxCollider2D>();
        return go;
    }
}
