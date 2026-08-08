using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// EVI KURAR — menuden: ALEV → Evi Kur
///
/// TASARIM: odalar AYNI BOYUTTA DEGIL. Olcek cesitliligi atmosferin kendisi.
///   Salon        56x28 — iki kat yuksek, evin gosterisli odasi
///   Cocuk odasi  28x28 — yuksek, tirmanis burada
///   Koridor      alcak tavanli tunel (oynanabilir yukseklik 6)
///   Digerleri    28x14 veya 42x14
///
/// KACIS YOLU SALONUN TEPESINDEN GECIYOR. Arama sirasinda salonun altindan
/// yurursunuz; kacarken tavanindaki yanmis kirislerden atlarsiniz. Ayni oda,
/// iki farkli yukseklik, iki farkli duygu. Kiristen duserseniz asagi inersiniz
/// ve tirmanmaniz gerekir — olum degil, bedel.
///
/// ATMOSFER: her odada titreyen dekoratif alevler, turuncu isik halkalari,
/// mobilya siluetleri. "Ev yanmiyor gibi duruyor" sorununun cozumu bu.
/// </summary>
public static class AlevLevelBuilder
{
    const float LevelW = 210f;
    const float GroundTop = 0f;
    const float SlabY = 14f;        // katlar arasi doseme (13.5 - 14.5)
    const float AtticTop = 14.5f;
    const float RoofY = 28.5f;
    const float DoorH = 3.2f;

    // Dikey saftlar
    const float DropMin = 4f, DropMax = 14f;   // TavanAntre -> Antre

    static Sprite square, glow;
    static Transform rooms, geo, plats, props, water, debris, triggers, markers, fires;

    static readonly Color CWall  = new Color(0.11f, 0.09f, 0.09f);
    static readonly Color CFloor = new Color(0.17f, 0.13f, 0.12f);
    static readonly Color CPlat  = new Color(0.30f, 0.22f, 0.17f);
    static readonly Color CProp  = new Color(0.22f, 0.16f, 0.14f);
    static readonly Color CDoor  = new Color(0.36f, 0.22f, 0.14f);
    static readonly Color CBack  = new Color(0.135f, 0.105f, 0.10f);

    [MenuItem("ALEV/Evi Kur", false, 1)]
    public static void BuildHouse()
    {
        if (!EditorUtility.DisplayDialog("Evi kur",
                "'Level' silinip yeniden kurulacak.\n\n" +
                "10 oda, degisken boyutlar, mobilya ve yangin atmosferi.\n" +
                "Player ve HUD etkilenmez.\n\nDevam?", "Kur", "Vazgec"))
            return;

        square = AlevSceneSetup.GetOrCreateSquareSprite();
        glow = GetOrCreateGlowSprite();

        GameObject old = GameObject.Find("Level");
        if (old != null) Object.DestroyImmediate(old);

        foreach (string n in new[] { "Ground", "Platform", "Musluk", "Akvaryum",
                                     "TestTarget_Su", "TestTarget_Ezme" })
        {
            GameObject l;
            while ((l = GameObject.Find(n)) != null) Object.DestroyImmediate(l);
        }

        var root = new GameObject("Level").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Evi kur");

        rooms    = Child(root, "Rooms");
        geo      = Child(root, "Geometry");
        plats    = Child(root, "Platforms");
        props    = Child(root, "Props");
        fires    = Child(root, "AmbientFires");
        water    = Child(root, "WaterSources");
        debris   = Child(root, "FallingDebris");
        triggers = Child(root, "Triggers");
        markers  = Child(root, "SpawnMarkers");

        BuildRooms();
        BuildShell();
        BuildDoors();
        BuildAntre();
        BuildSalon();
        BuildMutfak();
        BuildKoridor();
        BuildBanyo();
        BuildCocukOdasi();
        BuildAttic();
        BuildTriggers();
        BuildMarkers();
        SetupCamera();
        MovePlayerToStart();

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<b>[ALEV]</b> Ev kuruldu — 10 oda, degisken boyut.\n" +
                  "ARAMA: Antre → Salon(alt) → Mutfak → Koridor → Banyo → Cocuk odasi\n" +
                  "KACIS: Cocuk odasi tirmanis → TavanB/C/D → SALONUN TEPESINDEKI " +
                  "KIRISLER → TavanAntre → Antre'ye dusus → BOSS\n" +
                  "⚠️ Once dusmansiz tam tur atin.");
    }

    // ==================================================================
    //  Odalar
    // ==================================================================

    static readonly (string name, float x0, float y0, float x1, float y1)[] RoomDefs =
    {
        ("Antre",       0f,   0f,  28f, 14f),
        ("Salon",      28f,   0f,  84f, 28f),   // 56x28 — iki kat
        ("Mutfak",     84f,   0f, 126f, 14f),   // 42x14 — genis
        ("Koridor",   126f,   0f, 154f, 14f),   // alcak tunel
        ("Banyo",     154f,   0f, 182f, 14f),
        ("CocukOdasi",182f,   0f, 210f, 28f),   // 28x28 — yuksek

        ("TavanAntre",  0f,  14f,  28f, 28f),
        ("TavanD",     84f,  14f, 126f, 28f),
        ("TavanC",    126f,  14f, 154f, 28f),
        ("TavanB",    154f,  14f, 182f, 28f),
    };

    static void BuildRooms()
    {
        foreach (var (name, x0, y0, x1, y1) in RoomDefs)
        {
            var go = new GameObject("Oda_" + name);
            go.transform.SetParent(rooms, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);

            var r = go.AddComponent<Room>();
            r.roomName = name;
            r.size = new Vector2(x1 - x0, y1 - y0);
        }
    }

    // ==================================================================
    //  Kabuk
    // ==================================================================

    static void BuildShell()
    {
        Solid(geo, "Zemin", 105f, -0.5f, LevelW, 1f, CFloor);
        Solid(geo, "Cati", 105f, RoofY, LevelW, 1f, CWall);
        Solid(geo, "SolDuvar", -0.5f, 14f, 1f, 30f, CWall);
        Solid(geo, "SagDuvar", LevelW + 0.5f, 14f, 1f, 30f, CWall);

        // Katlar arasi doseme — SADECE Antre ve Mutfak/Koridor/Banyo uzerinde.
        // Salon (28-84) ve Cocuk odasi (182-210) iki kat yuksek, dosemeleri yok.
        Solid(geo, "Doseme_Antre_Sol", DropMin * 0.5f, SlabY, DropMin, 1f, CFloor);
        Solid(geo, "Doseme_Antre_Sag", (DropMax + 28f) * 0.5f, SlabY, 28f - DropMax, 1f, CFloor);
        Solid(geo, "Doseme_Orta", 133f, SlabY, 98f, 1f, CFloor);

        // Koridor'un ALCAK TAVANI — oynanabilir yukseklik sadece 6 birim.
        // Dar ve bogucu hissettirmesi kasitli: kitlik odasi.
        Solid(geo, "Koridor_AlcakTavan", 140f, 9.75f, 28f, 7.5f, CWall);

        // Arka duvarlar — odalar bos bir bosluk gibi durmasin
        foreach (var (name, x0, y0, x1, y1) in RoomDefs)
            Prop($"ArkaDuvar_{name}", (x0 + x1) * 0.5f, (y0 + y1) * 0.5f,
                 x1 - x0 - 1f, y1 - y0 - 1f, CBack, -20);
    }

    static void BuildDoors()
    {
        // Alt kat kapilari
        DoorAt(28f, GroundTop, SlabY - 0.5f);    // Antre | Salon
        DoorAt(84f, GroundTop, SlabY - 0.5f);    // Salon | Mutfak
        DoorAt(126f, GroundTop, 6f);             // Mutfak | Koridor (alcak)
        DoorAt(154f, GroundTop, 6f);             // Koridor | Banyo (alcak)
        DoorAt(182f, GroundTop, SlabY - 0.5f);   // Banyo | Cocuk odasi

        // Ust kat kapilari
        DoorAt(28f, AtticTop, RoofY - 0.5f);     // TavanAntre | Salon tepesi
        DoorAt(84f, AtticTop, RoofY - 0.5f);     // Salon tepesi | TavanD
        DoorAt(126f, AtticTop, RoofY - 0.5f);    // TavanD | TavanC
        DoorAt(154f, AtticTop, RoofY - 0.5f);    // TavanC | TavanB
        DoorAt(182f, AtticTop, RoofY - 0.5f);    // TavanB | Cocuk odasi tepesi
    }

    static void DoorAt(float x, float floorY, float ceilY)
    {
        float wb = floorY + DoorH;
        if (ceilY > wb)
            Solid(geo, $"Duvar_{x}_{floorY}", x, (wb + ceilY) * 0.5f, 0.6f, ceilY - wb, CWall);

        Prop($"Kapi_{x}_{floorY}", x, floorY + DoorH * 0.5f, 1.6f, DoorH, CDoor, -1);
    }

    // ==================================================================
    //  ANTRE — dar, karanlik, ogretici
    // ==================================================================

    static void BuildAntre()
    {
        Prop("Antre_Portmanto", 3f, 2.4f, 0.8f, 4.8f, CProp);
        Prop("Antre_Tablo", 15f, 7f, 2.4f, 3f, CProp);
        Prop("Antre_Hali", 14f, 0.15f, 10f, 0.3f, new Color(0.26f, 0.14f, 0.12f), 1);
        Solid(plats, "Antre_Ayakkabilik", 8f, 0.7f, 3.5f, 1.4f, CPlat);
        Solid(plats, "Antre_Konsol", 21f, 2f, 4f, 0.6f, CPlat);

        Fire("Antre_Alev1", 24f, 0.9f, 1.6f, 2.4f);
        Fire("Antre_Alev2", 2f, 5.5f, 1.2f, 1.8f);
    }

    // ==================================================================
    //  SALON — 56x28, evin gosterisli odasi
    // ==================================================================

    static void BuildSalon()
    {
        // Mobilya
        Prop("Salon_Somine", 33f, 3f, 7f, 6f, new Color(0.20f, 0.15f, 0.14f));
        Prop("Salon_Kitaplik", 60f, 5f, 3.5f, 10f, CProp);
        Prop("Salon_Kitaplik2", 78f, 4f, 3f, 8f, CProp);
        Prop("Salon_Avize", 56f, 25f, 5f, 2.5f, new Color(0.35f, 0.28f, 0.14f));
        Prop("Salon_Perde", 46f, 20f, 3f, 12f, new Color(0.24f, 0.13f, 0.13f));
        Prop("Salon_Hali", 50f, 0.15f, 18f, 0.3f, new Color(0.26f, 0.14f, 0.12f), 1);

        Solid(plats, "Salon_Koltuk", 42f, 1.2f, 7f, 1.6f, CPlat);
        Solid(plats, "Salon_Piyano", 72f, 1.6f, 8f, 2.4f, CPlat);

        // Somineden cikan buyuk alev — odanin isik kaynagi
        Fire("Salon_SomineAtes", 33f, 2f, 4.5f, 4.5f, 1.4f);
        Fire("Salon_Alev1", 66f, 0.9f, 1.8f, 2.6f);
        Fire("Salon_Alev2", 30f, 15f, 1.4f, 2f);
        Fire("Salon_Alev3", 80f, 12f, 1.6f, 2.2f);

        // ---- Tirmanis: yerden kirislere kadar ----
        // Kacis sirasinda kiristen duserseniz buradan geri cikarsiniz.
        Solid(plats, "Salon_T1", 36f, 2.6f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T2", 44f, 4.4f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T3", 52f, 6.2f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T4", 44f, 8.0f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T5", 52f, 9.8f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T6", 44f, 11.6f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T7", 34f, 13.4f, 5f, 0.5f, CPlat);

        // ---- KACIS KIRISLERI — salonun tepesinde, yanmis tavan kirisleri ----
        // Kacarken buradan gecilir. Asagisi 15 birim bosluk: dusmek olum degil,
        // ama tirmanis bedeli var. Gerilim boyle kurulur.
        Solid(plats, "Salon_Kiris1", 80f, 15.5f, 6f, 0.5f, CPlat);
        Solid(plats, "Salon_Kiris2", 71f, 16.8f, 6f, 0.5f, CPlat);
        Solid(plats, "Salon_Kiris3", 62f, 15.5f, 6f, 0.5f, CPlat);
        Solid(plats, "Salon_Kiris4", 53f, 17.2f, 6f, 0.5f, CPlat);
        Solid(plats, "Salon_Kiris5", 44f, 15.5f, 6f, 0.5f, CPlat);
        Solid(plats, "Salon_Kiris6", 35f, 16.6f, 6f, 0.5f, CPlat);
        Solid(plats, "Salon_Kiris7", 30f, 15.0f, 5f, 0.5f, CPlat);

        // Su
        Tap("Salon_Akvaryum", 31f, 0.6f, WaterSource.SourceType.OneShot, 100f,
            new Color(0.15f, 0.85f, 0.75f), 2f, 1.2f);
    }

    // ==================================================================
    //  MUTFAK — genis, su bol, guvenli. Nefes odasi.
    // ==================================================================

    static void BuildMutfak()
    {
        Prop("Mutfak_Buzdolabi", 88f, 3f, 3.5f, 6f, CProp);
        Prop("Mutfak_UstDolap", 104f, 6f, 14f, 2.2f, CProp);
        Prop("Mutfak_Fayans", 105f, 4f, 36f, 6f, new Color(0.17f, 0.16f, 0.15f), -10);

        Solid(plats, "Mutfak_Tezgah", 96f, 1.6f, 12f, 1.6f, CPlat);
        Solid(plats, "Mutfak_Masa", 116f, 1.8f, 7f, 0.6f, CPlat);

        Fire("Mutfak_Alev1", 122f, 0.9f, 1.6f, 2.2f);
        Fire("Mutfak_Alev2", 92f, 7.5f, 1.4f, 2f);

        Tap("Mutfak_Musluk", 98f, 3f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1f, 1.6f);
        Tap("Mutfak_Musluk2", 118f, 0.7f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1f, 1.4f);
    }

    // ==================================================================
    //  KORIDOR — alcak tavan, su YOK. Kitlik odasi.
    // ==================================================================

    static void BuildKoridor()
    {
        Prop("Koridor_Tablo1", 133f, 4f, 2f, 2.4f, CProp);
        Prop("Koridor_Tablo2", 147f, 4f, 2f, 2.4f, CProp);
        Prop("Koridor_Hali", 140f, 0.15f, 24f, 0.3f, new Color(0.24f, 0.13f, 0.12f), 1);

        Solid(plats, "Koridor_DevrikDolap", 140f, 1.6f, 4f, 0.6f, CPlat);

        Fire("Koridor_Alev1", 130f, 0.9f, 1.4f, 2f);
        Fire("Koridor_Alev2", 151f, 0.9f, 1.4f, 2f);
        // Su kaynagi YOK — kasitli. Yer atesi de konmuyor (kilitlenme riski).
    }

    // ==================================================================
    //  BANYO — yogun dusman, ama kuvet var
    // ==================================================================

    static void BuildBanyo()
    {
        Prop("Banyo_Ayna", 172f, 4.5f, 3f, 3.5f, new Color(0.20f, 0.22f, 0.24f));
        Prop("Banyo_Fayans", 168f, 5f, 26f, 8f, new Color(0.16f, 0.17f, 0.18f), -10);
        Prop("Banyo_Lavabo", 172f, 1.4f, 3f, 2.8f, CProp);

        Solid(plats, "Banyo_Kuvet", 161f, 1.4f, 8f, 1.8f, CPlat);
        Solid(plats, "Banyo_Raf", 177f, 4.2f, 3.5f, 0.5f, CPlat);

        Fire("Banyo_Alev1", 166f, 0.9f, 1.5f, 2.2f);
        Fire("Banyo_Alev2", 180f, 8f, 1.3f, 1.8f);

        Tap("Banyo_Kuvet_Su", 161f, 2.4f, WaterSource.SourceType.Infinite, 50f,
            new Color(0.25f, 0.60f, 0.95f), 6f, 0.6f);
    }

    // ==================================================================
    //  COCUK ODASI — 28x28, Karabas burada, tirmanis burada
    // ==================================================================

    static void BuildCocukOdasi()
    {
        Prop("Cocuk_Duvar", 196f, 14f, 26f, 26f, new Color(0.20f, 0.15f, 0.16f), -15);
        Prop("Cocuk_Yatak", 187f, 0.9f, 7f, 1.8f, CProp);
        Prop("Cocuk_Cizim", 192f, 8f, 2f, 2.4f, new Color(0.30f, 0.26f, 0.20f));
        Prop("Cocuk_BoyCizgisi", 184f, 4f, 0.4f, 8f, new Color(0.28f, 0.24f, 0.20f));

        Solid(plats, "Cocuk_Ranza", 187f, 2.4f, 7f, 0.6f, CPlat);
        Solid(plats, "Cocuk_OyuncakSandigi", 194f, 0.8f, 3f, 1.6f, CPlat);

        // ---- TIRMANIS: yerden ust kata (y 14.5) ----
        Solid(plats, "Tirmanis_1", 200f, 2.0f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_2", 206f, 3.8f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_3", 200f, 5.6f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_4", 206f, 7.4f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_5", 200f, 9.2f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_6", 206f, 11.0f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_7", 199f, 12.8f, 4f, 0.5f, CPlat);
        // Ust kata gecis sahanligi — TavanB kapisina (x=182) yuruyor
        Solid(plats, "Tirmanis_Sahanlik", 188f, 14.2f, 12f, 0.6f, CPlat);

        Fire("Cocuk_Alev1", 208f, 0.9f, 1.4f, 2f);
        Fire("Cocuk_Alev2", 184f, 18f, 1.6f, 2.2f);
    }

    // ==================================================================
    //  TAVAN ARASI + kacis
    // ==================================================================

    static void BuildAttic()
    {
        // TavanB / C / D — kirisler, cesitlilik icin birkac platform
        Solid(plats, "TavanB_Kiris", 168f, 17.2f, 5f, 0.5f, CPlat);
        Solid(plats, "TavanC_Kiris", 140f, 17.5f, 5f, 0.5f, CPlat);
        Solid(plats, "TavanD_Kiris", 105f, 17.2f, 5f, 0.5f, CPlat);

        Prop("Tavan_Kiris1", 170f, 26f, 40f, 1f, new Color(0.19f, 0.14f, 0.11f), -5);
        Prop("Tavan_Kiris2", 105f, 26f, 40f, 1f, new Color(0.19f, 0.14f, 0.11f), -5);

        Fire("Tavan_Alev1", 176f, 15.4f, 1.6f, 2.2f);
        Fire("Tavan_Alev2", 148f, 15.4f, 1.5f, 2f);
        Fire("Tavan_Alev3", 120f, 15.4f, 1.6f, 2.2f);
        Fire("Tavan_Alev4", 96f, 15.4f, 1.5f, 2f);
        Fire("Tavan_Alev5", 20f, 15.4f, 1.6f, 2.2f);

        // TavanAntre — inis safti (x 4-14) zaten dosemede bosluk
        Prop("TavanAntre_Sandik", 22f, 15.4f, 3f, 2.4f, CProp);

        // ---- Su ----
        // Olumden ONCE dolsun diye TavanB'de. Kacis yolu susuz kalmasin.
        Tap("TavanB_Musluk", 172f, 15.2f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1f, 1.6f);
        // Yalniz bolumde tek kullanimlik acil dolum
        Tap("TavanD_Bidon", 92f, 15.1f, WaterSource.SourceType.OneShot, 50f,
            new Color(0.20f, 0.70f, 0.85f), 1.2f, 1.4f);

        // ---- Dusen tahtalar ----
        Beam(1, 44f, 12.6f, "Salon - ILK KARSILASMA, genis ve guvenli");
        Beam(2, 108f, 12.6f, "Mutfak");
        Beam(3, 140f, 5.2f, "Koridor - alcak tavan, gerilim yuksek");
        Beam(4, 170f, 12.6f, "Banyo");
        Beam(5, 164f, 27f, "TavanB");
        Beam(6, 112f, 27f, "TavanD - yalniz bolum");
        Beam(7, 62f, 27f, "Salon kirisleri uzerinde - yalniz bolum");

        // ---- OLUM TAHTASI: TavanD, autoTrigger KAPALI ----
        var death = Sprite2(debris, "Tahta_OLUM (EndingSequence -> Death Debris)",
                            105f, 27f, 3.5f, 0.5f, new Color(0.5f, 0.3f, 0.16f));
        death.AddComponent<BoxCollider2D>().isTrigger = true;
        var dfd = death.AddComponent<FallingDebris>();
        dfd.groundLayer = LayerMask.GetMask("Ground");
        dfd.autoTrigger = false;
        dfd.groundMarker = null;
        dfd.lifetimeAfterLanding = 0f;
    }

    static void Beam(int i, float x, float y, string note)
    {
        var go = Sprite2(debris, $"Tahta_{i:00} ({note.Split('-')[0].Trim()})",
                         x, y, 2.8f, 0.4f, new Color(0.45f, 0.28f, 0.15f));
        go.AddComponent<BoxCollider2D>().isTrigger = true;

        var gm = Sprite2(go.transform, "YerIsareti", x, y, 2.4f, 0.25f,
                         new Color(1f, 0.45f, 0.1f, 0.9f));
        gm.GetComponent<SpriteRenderer>().sortingOrder = 3;
        gm.SetActive(false);

        var fd = go.AddComponent<FallingDebris>();
        fd.groundLayer = LayerMask.GetMask("Ground");
        fd.groundMarker = gm;
        fd.warningDuration = 1f;
        fd.triggerHeight = 13f;
        fd.autoTrigger = true;
    }

    // ==================================================================
    //  Tetikleyiciler
    // ==================================================================

    static void BuildTriggers()
    {
        // Karabas'in olumu — TavanD ortasi. Kacisin ~%60'i.
        Zone("Tetik_KopeginOlumu", 105f, 16f, 2f, 4f, EndingTriggerZone.What.DogDeath);

        // Boss — TavanAntre, inis saftina girmeden hemen once.
        Zone("Tetik_BossSahnesi", 20f, 16f, 2f, 4f, EndingTriggerZone.What.BossScene);
    }

    static void Zone(string name, float x, float y, float w, float h,
                     EndingTriggerZone.What what)
    {
        var go = new GameObject(name);
        go.transform.SetParent(triggers, false);
        go.transform.position = new Vector3(x, y, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(w, h);
        var z = go.AddComponent<EndingTriggerZone>();
        z.trigger = what;
        z.oneShot = true;
    }

    // ==================================================================
    //  Isaretler
    // ==================================================================

    static readonly (SpawnMarker.Kind k, float x, float y, string note)[] MarkerSpots =
    {
        (SpawnMarker.Kind.PlayerStart,   5f,  1f, "Oyuncu baslangici"),
        (SpawnMarker.Kind.FlameWall,     2f,  1f, "Giris alev duvari. Karabas kurtulunca GORUNUR sekilde yukselir."),
        (SpawnMarker.Kind.Boss,          4f,  1f, "Dev alev. Oyuncu tavandan buraya duser. Cani hic azalmaz."),
        (SpawnMarker.Kind.DogSilhouette,12f,  0.8f,"Kopek mamasi. 'Burada bir kopek yasiyor' ipucu."),

        (SpawnMarker.Kind.WallMonster,  18f,  1f, "Antre - ilk, ZAYIF (can 40)"),
        (SpawnMarker.Kind.WallMonster,  60f,  9f, "Salon - kitaplik, YUKSEK. Yukari nisan gerektirir."),
        (SpawnMarker.Kind.WallMonster,  78f,  7f, "Salon - ikinci kitaplik"),
        (SpawnMarker.Kind.WallMonster, 104f,  7f, "Mutfak - ust dolap"),
        (SpawnMarker.Kind.WallMonster, 131f,  1f, "Koridor 1/3 - su YOK, 2'sine yeter"),
        (SpawnMarker.Kind.WallMonster, 140f,  4f, "Koridor 2/3 - YUKSEK"),
        (SpawnMarker.Kind.WallMonster, 150f,  1f, "Koridor 3/3"),
        (SpawnMarker.Kind.WallMonster, 172f,  5f, "Banyo - ayna, YUKSEK"),
        (SpawnMarker.Kind.WallMonster, 158f,  1f, "Banyo"),
        (SpawnMarker.Kind.WallMonster, 166f, 18f, "TavanB"),
        (SpawnMarker.Kind.WallMonster,  98f, 18f, "TavanD - yalniz bolum"),

        (SpawnMarker.Kind.FloorFire,   164f, 0.5f, "Banyo"),
        (SpawnMarker.Kind.FloorFire,   177f, 0.5f, "Banyo"),
        (SpawnMarker.Kind.FloorFire,   112f, 0.5f, "Mutfak"),

        (SpawnMarker.Kind.FloorFire,   176f,15.0f, "KACIS Karabas'li - seyrek"),
        (SpawnMarker.Kind.FloorFire,   162f,15.0f, "KACIS Karabas'li"),
        (SpawnMarker.Kind.FloorFire,   146f,15.0f, "KACIS Karabas'li"),
        (SpawnMarker.Kind.FloorFire,   132f,15.0f, "KACIS Karabas'li"),
        (SpawnMarker.Kind.FloorFire,   118f,15.0f, "KACIS Karabas'li - olumden hemen once"),

        (SpawnMarker.Kind.FloorFire,    99f,15.0f, "YALNIZ - sik araliklar kaybi hissettirir"),
        (SpawnMarker.Kind.FloorFire,    94f,15.0f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,    89f,15.0f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,    80f,16.0f, "YALNIZ - salon kirisi uzerinde"),
        (SpawnMarker.Kind.FloorFire,    62f,16.0f, "YALNIZ - salon kirisi uzerinde"),
        (SpawnMarker.Kind.FloorFire,    44f,16.0f, "YALNIZ - salon kirisi uzerinde"),
        (SpawnMarker.Kind.FloorFire,    24f,15.0f, "YALNIZ - son"),

        (SpawnMarker.Kind.Dog,         186f, 1f, "KARABAS - sikismis halde baslar"),
        (SpawnMarker.Kind.CoveringFire,186f, 1f, "Karabas'i orten ates (can 50). Sondurulunce kurtulur."),
    };

    static void BuildMarkers()
    {
        foreach (var (k, x, y, note) in MarkerSpots)
        {
            var go = new GameObject($"[{k}] {note.Split('-')[0].Trim()}");
            go.transform.SetParent(markers, false);
            go.transform.position = new Vector3(x, y, 0f);
            var m = go.AddComponent<SpawnMarker>();
            m.kind = k;
            m.note = note;
        }
    }

    // ==================================================================
    //  Kamera / oyuncu
    // ==================================================================

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[ALEV] Main Camera yok."); return; }

        cam.orthographic = true;
        cam.orthographicSize = 7f;              // ekran 14 birim yuksek
        cam.backgroundColor = new Color(0.05f, 0.035f, 0.04f);

        Transform rig = cam.transform.parent;
        if (rig == null) { Debug.LogWarning("[ALEV] CameraRig yok — once Test Sahnesini Kur."); return; }

        var follow = rig.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = false;

        var rc = rig.GetComponent<RoomCamera>();
        if (rc == null) rc = rig.gameObject.AddComponent<RoomCamera>();
        rc.enabled = true;
    }

    static void MovePlayerToStart()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) p.transform.position = new Vector3(5f, 1f, 0f);
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================

    static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject Sprite2(Transform parent, string name, float x, float y,
                              float w, float h, Color c, int order = 0)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(w, h, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = c;
        sr.sortingOrder = order;
        return go;
    }

    static GameObject Solid(Transform parent, string name, float x, float y,
                            float w, float h, Color c)
    {
        var go = Sprite2(parent, name, x, y, w, h, c);
        go.layer = LayerMask.NameToLayer("Ground");
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    /// <summary>Dekoratif — collider yok, uzerine basilamaz.</summary>
    static void Prop(string name, float x, float y, float w, float h, Color c, int order = -2)
        => Sprite2(props, name, x, y, w, h, c, order);

    /// <summary>Dekoratif yanan alev + turuncu isik halkasi. Hasar vermez.</summary>
    static void Fire(string name, float x, float y, float w, float h, float glowScale = 1f)
    {
        var go = Sprite2(fires, name, x, y, w, h, new Color(1f, 0.55f, 0.12f, 0.95f), 5);

        var g = new GameObject("Isik");
        g.transform.SetParent(go.transform, false);
        g.transform.localScale = new Vector3(8f * glowScale / Mathf.Max(w, 0.01f),
                                             8f * glowScale / Mathf.Max(h, 0.01f), 1f);
        var gsr = g.AddComponent<SpriteRenderer>();
        gsr.sprite = glow;
        gsr.color = new Color(1f, 0.5f, 0.15f, 0.35f);
        gsr.sortingOrder = 4;

        var af = go.AddComponent<AmbientFire>();
        af.glow = gsr;
    }

    static void Tap(string name, float x, float y, WaterSource.SourceType type,
                    float rate, Color c, float w, float h)
    {
        var go = Sprite2(water, name, x, y, w, h, c, 2);
        go.tag = "WaterSource";

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3f / Mathf.Max(w, 0.01f), 3f / Mathf.Max(h, 0.01f));

        var src = go.AddComponent<WaterSource>();
        src.type = type;
        if (type == WaterSource.SourceType.Infinite) src.refillPerSecond = rate;
        else { src.instantAmount = rate; src.destroyAfterUse = true; }
    }

    /// <summary>Ortasi parlak, kenari saydam yuvarlak parilti dokusu.</summary>
    static Sprite GetOrCreateGlowSprite()
    {
        const string path = "Assets/Art/Generated/Glow.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder("Assets/Art/Generated"))
            AssetDatabase.CreateFolder("Assets/Art", "Generated");

        const int size = 128;
        var tex = new Texture2D(size, size);
        var px = new Color[size * size];
        var c = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                a = a * a;                       // kenara dogru hizla sonsun
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }

        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
