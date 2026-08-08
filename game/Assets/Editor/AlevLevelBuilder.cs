using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// EVI KURAR — menuden: ALEV → Evi Kur
///
/// ── ZIPLAMA MATEMATIGI (parkurlarin bug'siz olmasi buna bagli) ──
/// jumpForce 15, gravityScale 4 → maksimum yukselme ~2.87 birim.
/// Guvenli sinirlar:
///     yukselme 2.0 → yatay bosluk en fazla 3.5
///     yukselme 1.0 → yatay bosluk en fazla 4.5
///     duz        → yatay bosluk en fazla 5.0
/// Bu tablonun disina CIKMAYIN, cikilamayan platform olusur.
///
/// ── ODA BOYUTLARI ──
/// Tek kat odalar 16 yuksek, ekran 14 → 2 birim dikey kayma (canli durur).
/// Salon 68x32 ve Cocuk odasi 34x32 iki kat — kamera serbestce gezer.
/// Koridor oynanabilir yukseklik 6.5 — alcak, bogucu, kasitli.
///
/// ── ANAHTARLAR ──
/// Her anahtar, kilidinin HEMEN ONCEKI odasinda ve ana yoldan GORUNUR.
/// Asla geride degil. "Geri don ara" degil "su tepeye tirman" demek —
/// yanan ev oyununda aciliyeti oldurmemek icin sart.
/// </summary>
public static class AlevLevelBuilder
{
    const float LevelW = 258f;
    const float SlabY = 16f;         // katlar arasi doseme (15.5 - 16.5)
    const float AtticFloor = 16.5f;
    const float RoofY = 32.5f;
    const float DoorH = 3.4f;
    const float DropMin = 5f, DropMax = 16f;   // TavanAntre -> Antre inis safti

    static Sprite square, glow;
    static Transform rooms, geo, plats, props, water, debris, triggers, markers, fires, keys;

    static readonly Color CWall  = new Color(0.11f, 0.09f, 0.09f);
    static readonly Color CFloor = new Color(0.17f, 0.13f, 0.12f);
    static readonly Color CPlat  = new Color(0.31f, 0.23f, 0.17f);
    static readonly Color CProp  = new Color(0.22f, 0.16f, 0.14f);
    static readonly Color CDoor  = new Color(0.36f, 0.22f, 0.14f);
    static readonly Color CBack  = new Color(0.135f, 0.105f, 0.10f);

    [MenuItem("ALEV/Evi Kur", false, 1)]
    public static void BuildHouse()
    {
        if (!EditorUtility.DisplayDialog("Evi kur",
                "'Level' silinip yeniden kurulacak.\n\n" +
                "10 oda, 3 anahtar + 3 kilitli kapi, yangin atmosferi.\n" +
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
        keys     = Child(root, "Keys_and_Doors");
        debris   = Child(root, "FallingDebris");
        triggers = Child(root, "Triggers");
        markers  = Child(root, "SpawnMarkers");

        BuildRooms();
        BuildShell();
        BuildDoorways();
        BuildAntre();
        BuildSalon();
        BuildMutfak();
        BuildKoridor();
        BuildBanyo();
        BuildCocukOdasi();
        BuildAttic();
        BuildLocks();
        BuildTriggers();
        BuildMarkers();
        SetupCamera();
        MovePlayerToStart();

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<b>[ALEV]</b> Ev kuruldu — 258 birim, 10 oda, 3 anahtar.\n" +
                  "ARAMA:  Antre → Salon → [MAVI kilit] → Mutfak → Koridor → " +
                  "[YESIL kilit] → Banyo → [MOR kilit] → Cocuk odasi\n" +
                  "KACIS:  tirmanis → TavanB/C/D → SALONUN TEPESINDEKI KIRISLER " +
                  "→ TavanAntre → Antre'ye dusus → BOSS\n" +
                  "⚠️ Once dusmansiz tam tur atin.");
    }

    // ==================================================================
    //  Odalar
    // ==================================================================

    static readonly (string name, float x0, float y0, float x1, float y1)[] RoomDefs =
    {
        ("Antre",        0f,  0f,  34f, 16f),
        ("Salon",       34f,  0f, 102f, 32f),   // 68x32 — iki kat, gosterisli
        ("Mutfak",     102f,  0f, 152f, 16f),   // 50x16 — genis, su bol
        ("Koridor",    152f,  0f, 188f, 16f),   // alcak tunel (oynanabilir 6.5)
        ("Banyo",      188f,  0f, 224f, 16f),
        ("CocukOdasi", 224f,  0f, 258f, 32f),   // 34x32 — yuksek, tirmanis

        ("TavanAntre",   0f, 16f,  34f, 32f),
        ("TavanD",     102f, 16f, 152f, 32f),
        ("TavanC",     152f, 16f, 188f, 32f),
        ("TavanB",     188f, 16f, 224f, 32f),
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

    static void BuildShell()
    {
        Solid(geo, "Zemin", LevelW * 0.5f, -0.5f, LevelW, 1f, CFloor);
        Solid(geo, "Cati", LevelW * 0.5f, RoofY, LevelW, 1f, CWall);
        Solid(geo, "SolDuvar", -0.5f, 16f, 1f, 34f, CWall);
        Solid(geo, "SagDuvar", LevelW + 0.5f, 16f, 1f, 34f, CWall);

        // Doseme SADECE tek kat odalarin uzerinde.
        // Salon (34-102) ve Cocuk odasi (224-258) iki kat — dosemeleri yok.
        Solid(geo, "Doseme_Antre_Sol", DropMin * 0.5f, SlabY, DropMin, 1f, CFloor);
        Solid(geo, "Doseme_Antre_Sag", (DropMax + 34f) * 0.5f, SlabY, 34f - DropMax, 1f, CFloor);
        Solid(geo, "Doseme_Orta", 163f, SlabY, 122f, 1f, CFloor);   // 102 - 224

        // Koridor'un alcak tavani — oynanabilir yukseklik 6.5
        Solid(geo, "Koridor_AlcakTavan", 170f, 11f, 36f, 8.5f, CWall);

        foreach (var (name, x0, y0, x1, y1) in RoomDefs)
            Prop($"Arka_{name}", (x0 + x1) * 0.5f, (y0 + y1) * 0.5f,
                 x1 - x0 - 1f, y1 - y0 - 1f, CBack, -20);
    }

    static void BuildDoorways()
    {
        // Alt kat
        Doorway(34f, 0f, SlabY - 0.5f);
        Doorway(102f, 0f, SlabY - 0.5f);
        Doorway(152f, 0f, 6.5f);      // alcak koridor
        Doorway(188f, 0f, 6.5f);
        Doorway(224f, 0f, SlabY - 0.5f);

        // Ust kat
        Doorway(34f, AtticFloor, RoofY - 0.5f);
        Doorway(102f, AtticFloor, RoofY - 0.5f);
        Doorway(152f, AtticFloor, RoofY - 0.5f);
        Doorway(188f, AtticFloor, RoofY - 0.5f);
        Doorway(224f, AtticFloor, RoofY - 0.5f);
    }

    static void Doorway(float x, float floorY, float ceilY)
    {
        float wb = floorY + DoorH;
        if (ceilY > wb)
            Solid(geo, $"Duvar_{x}_{floorY}", x, (wb + ceilY) * 0.5f, 0.7f, ceilY - wb, CWall);
        Prop($"Kapi_{x}_{floorY}", x, floorY + DoorH * 0.5f, 1.8f, DoorH, CDoor, -1);
    }

    // ==================================================================
    //  ANTRE — 34x16, dar, ogretici
    // ==================================================================

    static void BuildAntre()
    {
        Prop("Antre_Portmanto", 3f, 2.6f, 0.9f, 5.2f, CProp);
        Prop("Antre_Tablo", 17f, 8f, 2.6f, 3.2f, CProp);
        Prop("Antre_Hali", 17f, 0.15f, 12f, 0.3f, new Color(0.26f, 0.14f, 0.12f), 1);

        Solid(plats, "Antre_Ayakkabilik", 9f, 0.8f, 4f, 1.6f, CPlat);
        Solid(plats, "Antre_Konsol", 24f, 2.2f, 5f, 0.6f, CPlat);

        Fire("Antre_Alev1", 30f, 1f, 1.8f, 2.6f);
        Fire("Antre_Alev2", 2f, 6.5f, 1.3f, 1.9f);
    }

    // ==================================================================
    //  SALON — 68x32, iki kat. Evin gosterisli odasi.
    // ==================================================================

    static void BuildSalon()
    {
        Prop("Salon_Somine", 40f, 3.4f, 8f, 6.8f, new Color(0.20f, 0.15f, 0.14f));
        Prop("Salon_Kitaplik", 62f, 6f, 4f, 12f, CProp);
        Prop("Salon_Kitaplik2", 95f, 5f, 3.5f, 10f, CProp);
        Prop("Salon_Avize", 68f, 29f, 6f, 3f, new Color(0.35f, 0.28f, 0.14f));
        Prop("Salon_Perde", 54f, 23f, 3.5f, 14f, new Color(0.24f, 0.13f, 0.13f));
        Prop("Salon_Hali", 62f, 0.15f, 22f, 0.3f, new Color(0.26f, 0.14f, 0.12f), 1);

        Solid(plats, "Salon_Koltuk", 50f, 1.3f, 8f, 1.8f, CPlat);
        Solid(plats, "Salon_Piyano", 88f, 1.8f, 9f, 2.8f, CPlat);

        Fire("Salon_SomineAtes", 40f, 2.2f, 5f, 5f, 1.5f);
        Fire("Salon_Alev1", 78f, 1f, 2f, 2.8f);
        Fire("Salon_Alev2", 36f, 18f, 1.5f, 2.1f);
        Fire("Salon_Alev3", 98f, 14f, 1.7f, 2.3f);

        // ---- TIRMANIS: sadece MAVI ANAHTARA kadar (y 7.8) ----
        // ⚠️ BILEREK KIRISLERE ULASMIYOR. Eskiden yerden tepeye kadar
        // cikiyordu ve oyuncu daha oyunun basinda kacis yoluna ulasip
        // final tetikleyicilerine basabiliyordu — oyun kiriliyordu.
        // En yuksek nokta 8.05; guvenlik sahanligi 12.25'te, arada 4.2 birim
        // var (maksimum ziplama 2.87). Asagidan cikilamaz.
        Solid(plats, "Salon_T1", 46f, 2.4f, 6f, 0.5f, CPlat);
        Solid(plats, "Salon_T2", 54f, 4.2f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T3", 62f, 6.0f, 5f, 0.5f, CPlat);   // anahtar burada
        Solid(plats, "Salon_T4", 54f, 7.8f, 4f, 0.5f, CPlat);

        // ---- GUVENLIK SAHANLIKLARI ----
        // Kacis sirasinda kiristen duserseniz buraya dusersiniz ve geri
        // tirmanirsiniz. Yerden ULASILAMAZ (4+ birim bosluk), sadece
        // yukaridan dusunce inilir. Olum degil, birkac saniyelik bedel.
        Solid(plats, "Salon_Sahanlik_Sol", 58f, 12.0f, 32f, 0.6f, CPlat);
        Solid(plats, "Salon_Sahanlik_Sag", 90f, 12.0f, 22f, 0.6f, CPlat);

        Solid(plats, "Salon_Geri_1", 48f, 13.8f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_Geri_2", 42f, 15.6f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_Geri_3", 94f, 13.8f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_Geri_4", 99f, 15.6f, 5f, 0.5f, CPlat);

        // ---- KACIS KIRISLERI — salonun tepesi (sagdan sola) ----
        // Bosluklar tek tek hesaplandi: hicbiri 3.5'i gecmiyor.
        // Duserseniz asagi inersiniz ve tirmanirsiniz. Olum degil, bedel.
        Solid(plats, "Kiris_1", 98f, 17.0f, 7f, 0.5f, CPlat);
        Solid(plats, "Kiris_2", 88f, 18.4f, 6f, 0.5f, CPlat);
        Solid(plats, "Kiris_3", 78f, 17.0f, 5f, 0.5f, CPlat);
        Solid(plats, "Kiris_4", 70f, 18.6f, 5f, 0.5f, CPlat);
        Solid(plats, "Kiris_5", 62f, 17.2f, 4f, 0.5f, CPlat);
        Solid(plats, "Kiris_6", 54f, 18.4f, 5f, 0.5f, CPlat);
        Solid(plats, "Kiris_7", 46f, 17.0f, 6f, 0.5f, CPlat);
        Solid(plats, "Kiris_8", 38f, 16.8f, 6f, 0.5f, CPlat);

        Tap("Salon_Akvaryum", 37f, 0.7f, WaterSource.SourceType.OneShot, 100f,
            new Color(0.15f, 0.85f, 0.75f), 2.2f, 1.4f);
    }

    // ==================================================================
    //  MUTFAK — 50x16, genis, su bol, nefes odasi
    // ==================================================================

    static void BuildMutfak()
    {
        Prop("Mutfak_Buzdolabi", 107f, 3.4f, 4f, 6.8f, CProp);
        Prop("Mutfak_UstDolap", 126f, 7f, 18f, 2.4f, CProp);
        Prop("Mutfak_Fayans", 127f, 4.5f, 44f, 7f, new Color(0.17f, 0.16f, 0.15f), -10);

        Solid(plats, "Mutfak_Tezgah", 118f, 1.8f, 14f, 1.8f, CPlat);
        Solid(plats, "Mutfak_Masa", 140f, 2.0f, 8f, 0.6f, CPlat);
        Solid(plats, "Mutfak_Dolap", 147f, 4.0f, 5f, 0.5f, CPlat);

        Fire("Mutfak_Alev1", 148f, 1f, 1.8f, 2.4f);
        Fire("Mutfak_Alev2", 112f, 9f, 1.5f, 2.1f);

        Tap("Mutfak_Musluk", 122f, 3.4f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1.2f, 1.8f);
        Tap("Mutfak_Musluk2", 143f, 0.8f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1.2f, 1.6f);
    }

    // ==================================================================
    //  KORIDOR — 36x16 ama oynanabilir yukseklik 6.5. Su YOK.
    // ==================================================================

    static void BuildKoridor()
    {
        Prop("Koridor_Tablo1", 160f, 4.5f, 2.2f, 2.6f, CProp);
        Prop("Koridor_Tablo2", 182f, 4.5f, 2.2f, 2.6f, CProp);
        Prop("Koridor_Hali", 170f, 0.15f, 30f, 0.3f, new Color(0.24f, 0.13f, 0.12f), 1);

        Solid(plats, "Koridor_DevrikDolap", 166f, 1.6f, 4.5f, 0.6f, CPlat);
        Solid(plats, "Koridor_Raf", 176f, 3.4f, 3.5f, 0.5f, CPlat);   // anahtar buraya

        Fire("Koridor_Alev1", 156f, 1f, 1.5f, 2.1f);
        Fire("Koridor_Alev2", 185f, 1f, 1.5f, 2.1f);
        // ⚠️ Su kaynagi YOK ve yer atesi de YOK — kasitli kitlik odasi,
        //    ama buyuyen ates gecisi kapatamaz, kilitlenme riski sifir.
    }

    // ==================================================================
    //  BANYO — 36x16, yogun dusman, kuvet var
    // ==================================================================

    static void BuildBanyo()
    {
        Prop("Banyo_Ayna", 208f, 5.5f, 3.5f, 4f, new Color(0.20f, 0.22f, 0.24f));
        Prop("Banyo_Fayans", 206f, 6f, 34f, 10f, new Color(0.16f, 0.17f, 0.18f), -10);
        Prop("Banyo_Lavabo", 208f, 1.6f, 3.5f, 3.2f, CProp);

        Solid(plats, "Banyo_Kuvet", 196f, 1.6f, 9f, 2f, CPlat);
        Solid(plats, "Banyo_Raf1", 208f, 3.4f, 4f, 0.5f, CPlat);
        Solid(plats, "Banyo_Raf2", 216f, 5.2f, 3.5f, 0.5f, CPlat);   // anahtar buraya

        Fire("Banyo_Alev1", 200f, 1f, 1.6f, 2.3f);
        Fire("Banyo_Alev2", 220f, 10f, 1.4f, 1.9f);

        Tap("Banyo_Kuvet_Su", 196f, 2.9f, WaterSource.SourceType.Infinite, 50f,
            new Color(0.25f, 0.60f, 0.95f), 7f, 0.7f);
    }

    // ==================================================================
    //  COCUK ODASI — 34x32, Karabas + tirmanis
    // ==================================================================

    static void BuildCocukOdasi()
    {
        Prop("Cocuk_DuvarKagidi", 241f, 16f, 32f, 30f, new Color(0.20f, 0.15f, 0.16f), -15);
        Prop("Cocuk_Yatak", 230f, 1f, 8f, 2f, CProp);
        Prop("Cocuk_Cizim", 236f, 9f, 2.4f, 2.8f, new Color(0.30f, 0.26f, 0.20f));
        Prop("Cocuk_BoyCizgisi", 226f, 5f, 0.5f, 10f, new Color(0.28f, 0.24f, 0.20f));

        Solid(plats, "Cocuk_Ranza", 230f, 2.6f, 8f, 0.6f, CPlat);
        Solid(plats, "Cocuk_Sandik", 238f, 0.9f, 3.5f, 1.8f, CPlat);

        // ---- TIRMANIS: yerden ust kata (16.5) ----
        Solid(plats, "Tirmanis_1", 246f,  2.2f, 5f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_2", 253f,  4.0f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_3", 246f,  5.8f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_4", 253f,  7.6f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_5", 246f,  9.4f, 3.5f, 0.5f, CPlat);  // kucuk
        Solid(plats, "Tirmanis_6", 253f, 11.2f, 4f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_7", 245f, 13.0f, 5f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_8", 237f, 14.8f, 5f, 0.5f, CPlat);
        Solid(plats, "Tirmanis_Sahanlik", 228f, 16.2f, 9f, 0.6f, CPlat);

        Fire("Cocuk_Alev1", 256f, 1f, 1.5f, 2.1f);
        Fire("Cocuk_Alev2", 226f, 21f, 1.7f, 2.3f);
    }

    // ==================================================================
    //  TAVAN ARASI
    // ==================================================================

    static void BuildAttic()
    {
        Solid(plats, "TavanB_Kiris", 205f, 19.4f, 6f, 0.5f, CPlat);
        Solid(plats, "TavanC_Kiris", 170f, 19.6f, 6f, 0.5f, CPlat);
        Solid(plats, "TavanD_Kiris", 128f, 19.4f, 6f, 0.5f, CPlat);

        Prop("Tavan_Kiris1", 205f, 30f, 46f, 1.2f, new Color(0.19f, 0.14f, 0.11f), -5);
        Prop("Tavan_Kiris2", 128f, 30f, 46f, 1.2f, new Color(0.19f, 0.14f, 0.11f), -5);

        Fire("Tavan_A1", 214f, 17.4f, 1.7f, 2.3f);
        Fire("Tavan_A2", 180f, 17.4f, 1.6f, 2.1f);
        Fire("Tavan_A3", 146f, 17.4f, 1.7f, 2.3f);
        Fire("Tavan_A4", 110f, 17.4f, 1.6f, 2.1f);
        Fire("Tavan_A5", 24f, 17.4f, 1.7f, 2.3f);

        Prop("TavanAntre_Sandik", 27f, 17.6f, 3.5f, 2.6f, CProp);

        // Olumden ONCE dolsun — kacis yolu susuz kalmasin
        Tap("TavanB_Musluk", 210f, 17.4f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1.2f, 1.8f);
        // Yalniz bolumde tek kullanimlik acil dolum
        Tap("TavanD_Bidon", 108f, 17.2f, WaterSource.SourceType.OneShot, 50f,
            new Color(0.20f, 0.70f, 0.85f), 1.4f, 1.6f);

        Beam(1,  56f, 14.0f, "Salon - ILK KARSILASMA, genis ve guvenli");
        Beam(2, 132f, 14.6f, "Mutfak");
        Beam(3, 172f,  5.6f, "Koridor - alcak tavan, gerilim yuksek");
        Beam(4, 204f, 14.6f, "Banyo");
        Beam(5, 198f, 31f, "TavanB");
        Beam(6, 138f, 31f, "TavanD");
        Beam(7,  66f, 31f, "Salon kirisleri uzerinde - yalniz bolum");

        // OLUM TAHTASI — TavanD, autoTrigger KAPALI
        var death = Sprite2(debris, "Tahta_OLUM (EndingSequence -> Death Debris)",
                            126f, 31f, 4f, 0.5f, new Color(0.5f, 0.3f, 0.16f));
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
                         x, y, 3f, 0.4f, new Color(0.45f, 0.28f, 0.15f));
        go.AddComponent<BoxCollider2D>().isTrigger = true;

        var gm = Sprite2(go.transform, "YerIsareti", x, y, 2.6f, 0.25f,
                         new Color(1f, 0.45f, 0.1f, 0.9f));
        gm.GetComponent<SpriteRenderer>().sortingOrder = 3;
        gm.SetActive(false);

        var fd = go.AddComponent<FallingDebris>();
        fd.groundLayer = LayerMask.GetMask("Ground");
        fd.groundMarker = gm;
        fd.warningDuration = 1f;
        fd.triggerHeight = 15f;
        fd.autoTrigger = true;
    }

    // ==================================================================
    //  ANAHTARLAR ve KILITLI KAPILAR
    // ==================================================================

    static void BuildLocks()
    {
        // MAVI — Salon'un tirmanis rotasinda, yerden GORUNUR. Kilidi Mutfak girisi.
        Key(KeyColor.Mavi, 62f, 7.6f);
        Lock(KeyColor.Mavi, 102f);

        // YESIL — Koridor'da rafta. Alcak tavanli odada canavarlar arasinda.
        Key(KeyColor.Yesil, 176f, 5.0f);
        Lock(KeyColor.Yesil, 188f);

        // MOR — Banyo'da ust rafta. Kuvetten iki ziplama.
        Key(KeyColor.Mor, 216f, 6.8f);
        Lock(KeyColor.Mor, 224f);
    }

    static void Key(KeyColor c, float x, float y)
    {
        Color col = KeyColors.Of(c);

        var go = Sprite2(keys, $"Anahtar_{c}", x, y, 0.9f, 1.4f, col, 6);
        go.AddComponent<BoxCollider2D>().isTrigger = true;

        var g = new GameObject("Isik");
        g.transform.SetParent(go.transform, false);
        g.transform.localScale = new Vector3(7f / 0.9f, 7f / 1.4f, 1f);
        var gsr = g.AddComponent<SpriteRenderer>();
        gsr.sprite = glow;
        gsr.color = new Color(col.r, col.g, col.b, 0.4f);
        gsr.sortingOrder = 5;

        var kp = go.AddComponent<KeyPickup>();
        kp.color = c;
        kp.glow = gsr;
    }

    static void Lock(KeyColor c, float x)
    {
        var go = Sprite2(keys, $"KilitliKapi_{c}", x, DoorH * 0.5f, 1.8f, DoorH,
                         CDoor, 1);

        // Kati engel — acilinca kapatilir
        var blocker = go.AddComponent<BoxCollider2D>();
        blocker.isTrigger = false;

        // Algilama — daha genis, oyuncu yaklasinca tepki versin
        var sense = go.AddComponent<BoxCollider2D>();
        sense.isTrigger = true;
        sense.size = new Vector2(2.2f, 1.2f);

        var ld = go.AddComponent<LockedDoor>();
        ld.requiredKey = c;
        ld.blocker = blocker;
        ld.openHeight = DoorH + 0.4f;
    }

    // ==================================================================
    //  Tetikleyiciler / isaretler
    // ==================================================================

    static void BuildTriggers()
    {
        // ⚠️ SIRADISILIK KORUMASI
        // Oyuncu beklenmedik bir yoldan kacis rotasina ulasirsa final
        // sahnesi yanlis sirada tetiklenmesin. Fiziksel engelleme (Salon
        // tirmanisinin kisaltilmasi) yeterli olmali, ama bu ikinci kat
        // guvenlik: bilmedigimiz bir kestirme bulunsa bile oyun kirilmaz.
        var death = Zone("Tetik_KopeginOlumu", 126f, 18.5f, 2f, 4f,
                         EndingTriggerZone.What.DogDeath);
        death.requiredKeyCount = 3;   // 3 anahtar = cocuk odasina ulasildi

        var boss = Zone("Tetik_BossSahnesi", 24f, 18.5f, 2f, 4f,
                        EndingTriggerZone.What.BossScene);
        boss.requires = death;        // once Karabas olmeli
    }

    static EndingTriggerZone Zone(string name, float x, float y, float w, float h,
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
        return z;
    }

    static readonly (SpawnMarker.Kind k, float x, float y, string note)[] MarkerSpots =
    {
        (SpawnMarker.Kind.PlayerStart,   6f,  1f, "Oyuncu baslangici"),
        (SpawnMarker.Kind.FlameWall,     2f,  1f, "Giris alev duvari. Karabas kurtulunca GORUNUR sekilde yukselir."),
        (SpawnMarker.Kind.Boss,          5f,  1f, "Dev alev. Oyuncu tavandan buraya duser. Cani hic azalmaz."),
        (SpawnMarker.Kind.DogSilhouette,14f,  0.8f,"Kopek mamasi — 'burada bir kopek yasiyor' ipucu."),

        (SpawnMarker.Kind.WallMonster,  21f,  1f, "Antre - ilk, ZAYIF (can 40)"),
        (SpawnMarker.Kind.WallMonster,  62f, 12f, "Salon kitapligi - YUKSEK, tirmanis rotasinda"),
        (SpawnMarker.Kind.WallMonster,  95f,  8f, "Salon ikinci kitaplik"),
        (SpawnMarker.Kind.WallMonster, 126f,  8f, "Mutfak ust dolap"),
        (SpawnMarker.Kind.WallMonster, 158f,  1f, "Koridor 1/3 - su YOK, 2'sine yeter"),
        (SpawnMarker.Kind.WallMonster, 170f,  4f, "Koridor 2/3 - YUKSEK"),
        (SpawnMarker.Kind.WallMonster, 184f,  1f, "Koridor 3/3"),
        (SpawnMarker.Kind.WallMonster, 208f,  6f, "Banyo ayna - YUKSEK"),
        (SpawnMarker.Kind.WallMonster, 192f,  1f, "Banyo"),
        (SpawnMarker.Kind.WallMonster, 200f, 20f, "TavanB"),
        (SpawnMarker.Kind.WallMonster, 118f, 20f, "TavanD - yalniz bolum"),

        (SpawnMarker.Kind.FloorFire,   202f, 0.5f, "Banyo"),
        (SpawnMarker.Kind.FloorFire,   218f, 0.5f, "Banyo"),
        (SpawnMarker.Kind.FloorFire,   136f, 0.5f, "Mutfak"),

        (SpawnMarker.Kind.FloorFire,   216f,17.0f, "KACIS Karabas'li - seyrek"),
        (SpawnMarker.Kind.FloorFire,   198f,17.0f, "KACIS Karabas'li"),
        (SpawnMarker.Kind.FloorFire,   178f,17.0f, "KACIS Karabas'li"),
        (SpawnMarker.Kind.FloorFire,   160f,17.0f, "KACIS Karabas'li"),
        (SpawnMarker.Kind.FloorFire,   140f,17.0f, "KACIS Karabas'li - olumden hemen once"),

        (SpawnMarker.Kind.FloorFire,   120f,17.0f, "YALNIZ - sik araliklar kaybi hissettirir"),
        (SpawnMarker.Kind.FloorFire,   114f,17.0f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,   106f,17.0f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,    98f,17.5f, "YALNIZ - salon kirisi uzerinde"),
        (SpawnMarker.Kind.FloorFire,    78f,17.5f, "YALNIZ - salon kirisi uzerinde"),
        (SpawnMarker.Kind.FloorFire,    46f,17.5f, "YALNIZ - salon kirisi uzerinde"),
        (SpawnMarker.Kind.FloorFire,    30f,17.0f, "YALNIZ - son"),

        (SpawnMarker.Kind.Dog,         229f, 1f, "KARABAS - sikismis halde baslar"),
        (SpawnMarker.Kind.CoveringFire,229f, 1f, "Karabas'i orten ates (can 50)."),
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
        cam.orthographicSize = 7f;
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
        if (p == null) return;

        p.transform.position = new Vector3(6f, 1f, 0f);
        if (p.GetComponent<KeyInventory>() == null) p.AddComponent<KeyInventory>();
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

    static void Prop(string name, float x, float y, float w, float h, Color c, int order = -2)
        => Sprite2(props, name, x, y, w, h, c, order);

    static void Fire(string name, float x, float y, float w, float h, float glowScale = 1f)
    {
        var go = Sprite2(fires, name, x, y, w, h, new Color(1f, 0.55f, 0.12f, 0.95f), 5);

        var g = new GameObject("Isik");
        g.transform.SetParent(go.transform, false);
        g.transform.localScale = new Vector3(9f * glowScale / Mathf.Max(w, 0.01f),
                                             9f * glowScale / Mathf.Max(h, 0.01f), 1f);
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
        col.size = new Vector2(3.5f / Mathf.Max(w, 0.01f), 3.5f / Mathf.Max(h, 0.01f));

        var src = go.AddComponent<WaterSource>();
        src.type = type;
        if (type == WaterSource.SourceType.Infinite) src.refillPerSecond = rate;
        else { src.instantAmount = rate; src.destroyAfterUse = true; }
    }

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
                px[y * size + x] = new Color(1f, 1f, 1f, a * a);
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
