using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// EVI KURAR — menuden: ALEV → Evi Kur
///
/// ── DUZ DIZILIM (tek hat, kat yok) ──
/// Odalar yan yana. Ne dikey saft var ne de ust kat. Kazanclari:
///   - Navigasyon karismiyor: hep ileri gidiyorsunuz
///   - Katlar arasi bug sinifi yok (dusman/tahta yanlis kattan tetiklenmez)
///   - Odalar birbirinin YUKSEKLIGINI KISITLAMIYOR: Salon 26 birim yuksek
///     olabilir, Koridor 6.5 birim alcak olabilir, hicbir cakisma olmadan.
///     Yani olcek cesitliligini kaybetmiyoruz, kazaniyoruz.
///
/// AKIS (hep ileri, geri donus yok):
///   Antre → Salon → [MAVI] → Mutfak → [YESIL] → Koridor → [MOR] →
///   Cocuk odasi (KARABAS) → giris alevle kapanir →
///   Arka koridor (OLUM) → Arka kapi (BOSS) → cikis
///
/// ── ZIPLAMA MATEMATIGI ──
/// jumpForce 15, gravityScale 4 → maksimum yukselme 2.87.
///   yukselme 2.0 → yatay bosluk max 3.5
///   yukselme 1.0 → max 4.5   /   duz → max 5.0
/// </summary>
public static class AlevLevelBuilder
{
    const float DoorH = 3.4f;

    static Sprite square, glow, flameSprite;
    static Transform rooms, geo, plats, props, water, debris, triggers, markers, fires, keys;

    static readonly Color CWall  = new Color(0.11f, 0.09f, 0.09f);
    static readonly Color CFloor = new Color(0.17f, 0.13f, 0.12f);
    static readonly Color CPlat  = new Color(0.31f, 0.23f, 0.17f);
    static readonly Color CProp  = new Color(0.22f, 0.16f, 0.14f);
    static readonly Color CDoor  = new Color(0.36f, 0.22f, 0.14f);
    static readonly Color CBack  = new Color(0.135f, 0.105f, 0.10f);

    // ---- ODALAR: hepsi yan yana, y=0 zemininde. Yukseklikler serbest. ----
    //
    // ⚠️ YUKSEKLIKLER KUCULTULDU. Eskiden odalar 16-26 birim yuksekti ama
    // butun icerik en alttaki 4 birimde toplaniyordu — odalarin ust ucte
    // ikisi bombos siyah bosluktu. Artik yukseklik oynanan alana yakin.
    //
    // Standart oda 11 = kameranin gorus yuksekligi (ortho 5.5 x 2), yani
    // tam oturuyor, dikey kayma yok. Salon ve Cocuk odasi bilerek daha
    // yuksek — oralarda kamera kayiyor ve buyukluk hissediliyor.
    static readonly (string name, float x0, float x1, float h)[] RoomDefs =
    {
        ("Antre",         0f,   34f, 11f),
        ("Salon",        34f,  102f, 20f),   // dev oda, dikey tirmanis
        ("Mutfak",      102f,  152f, 11f),   // su bol, nefes odasi
        ("Koridor",     152f,  188f, 11f),   // oynanabilir yukseklik 6.5
        ("CocukOdasi",  188f,  226f, 15f),   // KARABAS
        ("ArkaKoridor", 226f,  294f, 11f),   // kacis + OLUM
        ("ArkaKapi",    294f,  336f, 13f),   // BOSS + cikis
    };

    const float LevelW = 336f;

    [MenuItem("ALEV/Evi Kur", false, 1)]
    public static void BuildHouse()
    {
        if (!EditorUtility.DisplayDialog("Evi kur",
                "'Level' silinip DUZ DIZILIMLE yeniden kurulacak.\n\n" +
                "7 oda yan yana, kat yok. Player ve HUD etkilenmez.\n\n" +
                "⚠️ Bu son otomatik kurulum olsun — sonrasinda elle tasarlayin, " +
                "yoksa yaptiklariniz silinir.\n\nDevam?", "Kur", "Vazgec"))
            return;

        square = AlevSceneSetup.GetOrCreateSquareSprite();
        glow = GetOrCreateGlowSprite();
        flameSprite = GetOrCreateFlameSprite();

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
        Antre();
        Salon();
        Mutfak();
        Koridor();
        CocukOdasi();
        ArkaKoridor();
        ArkaKapi();
        BuildTriggers();
        BuildMarkers();
        SetupCamera();
        MovePlayerToStart();

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<b>[ALEV]</b> Ev kuruldu — 7 oda, DUZ dizilim, 336 birim.\n" +
                  "Antre → Salon → [MAVI] → Mutfak → [YESIL] → Koridor → [MOR] → " +
                  "Cocuk odasi → Arka koridor (OLUM) → Arka kapi (BOSS) → cikis\n" +
                  "⚠️ Simdi 'Seviyeyi Denetle' calistirin, sonra elle tasarima gecin.");
    }

    // ==================================================================

    static void BuildRooms()
    {
        foreach (var (name, x0, x1, h) in RoomDefs)
        {
            var go = new GameObject("Oda_" + name);
            go.transform.SetParent(rooms, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, h * 0.5f, 0f);
            var r = go.AddComponent<Room>();
            r.roomName = name;
            r.size = new Vector2(x1 - x0, h);
        }
    }

    static void BuildShell()
    {
        Solid(geo, "Zemin", LevelW * 0.5f, -0.5f, LevelW, 1f, CFloor);
        Solid(geo, "SolDuvar", -0.5f, 10f, 1f, 22f, CWall);
        Solid(geo, "SagDuvar", LevelW + 0.5f, 10f, 1f, 22f, CWall);

        foreach (var (name, x0, x1, h) in RoomDefs)
        {
            // Her odanin kendi tavani — yukseklikler farkli olabildigi icin
            Solid(geo, $"Tavan_{name}", (x0 + x1) * 0.5f, h + 0.5f, x1 - x0, 1f, CWall);
            Prop($"Arka_{name}", (x0 + x1) * 0.5f, h * 0.5f, x1 - x0 - 1f, h - 1f, CBack, -20);
        }

        // Koridor'un ALCAK TAVANI — oynanabilir yukseklik 6.5, bogucu
        Solid(geo, "Koridor_AlcakTavan", 170f, 8.75f, 36f, 4.5f, CWall);

        // ---- TAVAN DETAYLARI ----
        // Odalarin ustu bombos siyah bosluktu. Kirisler, borular ve
        // tavana yakin alevler o boslugu dolduruyor: bakis her yerde bir
        // seye takiliyor, oda "kutu" olmaktan cikip "mekan" oluyor.
        foreach (var (name, x0, x1, h) in RoomDefs)
        {
            float w = x1 - x0;
            float cx = (x0 + x1) * 0.5f;

            // Tavan kirisleri
            Prop($"TavanKiris_{name}_1", cx - w * 0.25f, h - 0.9f, w * 0.42f, 0.7f,
                 new Color(0.20f, 0.15f, 0.12f), -4);
            Prop($"TavanKiris_{name}_2", cx + w * 0.25f, h - 0.9f, w * 0.42f, 0.7f,
                 new Color(0.20f, 0.15f, 0.12f), -4);

            // Duvar dibinde koyu golge — zemin ile duvar arasinda gecis
            Prop($"Golge_{name}", cx, 0.6f, w, 1.2f, new Color(0.10f, 0.08f, 0.08f), -14);
        }

        // Odalar arasi duvarlar + kapi bosluklari
        for (int i = 1; i < RoomDefs.Length; i++)
        {
            float bx = RoomDefs[i].x0;
            float top = Mathf.Max(RoomDefs[i - 1].h, RoomDefs[i].h);
            Solid(geo, $"Duvar_{bx}", bx, (DoorH + top) * 0.5f, 0.8f, top - DoorH, CWall);
            Prop($"Kapi_{bx}", bx, DoorH * 0.5f, 1.8f, DoorH, CDoor, -1);
        }
    }

    // ==================================================================
    //  1. ANTRE — giris, ogretici, sonda BOSS degil: sonda ALEV DUVARI
    // ==================================================================

    static void Antre()
    {
        Prop("Antre_Portmanto", 3f, 2.6f, 0.9f, 5.2f, CProp);
        Prop("Antre_Tablo", 17f, 8f, 2.6f, 3.2f, CProp);
        Prop("Antre_Hali", 17f, 0.15f, 12f, 0.3f, new Color(0.26f, 0.14f, 0.12f), 1);
        Prop("Antre_GirisKapisi", 1.5f, 2f, 2.4f, 4f, new Color(0.30f, 0.19f, 0.12f), -1);

        Solid(plats, "Antre_Ayakkabilik", 9f, 0.8f, 4f, 1.6f, CPlat);
        Solid(plats, "Antre_Konsol", 24f, 2.2f, 5f, 0.6f, CPlat);

        Fire("Antre_Alev1", 30f, 1.4f, 2.4f, 3.8f);
        Fire("Antre_Alev2", 2f, 6.5f, 1.9f, 3f);

        Tap("Antre_Kalorifer", 31f, 1.2f, WaterSource.SourceType.Infinite, 25f,
            new Color(0.25f, 0.60f, 0.95f), 1f, 2.2f);
    }

    // ==================================================================
    //  2. SALON — 68x26. Evin gosterisli odasi, MAVI anahtar tepede.
    // ==================================================================

    static void Salon()
    {
        Prop("Salon_Somine", 40f, 3.4f, 8f, 6.8f, new Color(0.20f, 0.15f, 0.14f));
        Prop("Salon_Kitaplik", 68f, 6f, 4f, 12f, CProp);
        Prop("Salon_Avize", 68f, 17.4f, 6f, 2.6f, new Color(0.35f, 0.28f, 0.14f));
        Prop("Salon_AvizeZincir", 68f, 19.2f, 0.3f, 2f, new Color(0.28f, 0.24f, 0.16f));
        Prop("Salon_Perde", 96f, 12f, 3.5f, 12f, new Color(0.24f, 0.13f, 0.13f));
        Prop("Salon_Hali", 62f, 0.15f, 22f, 0.3f, new Color(0.26f, 0.14f, 0.12f), 1);

        Solid(plats, "Salon_Koltuk", 50f, 1.3f, 8f, 1.8f, CPlat);
        Solid(plats, "Salon_Piyano", 88f, 1.8f, 9f, 2.8f, CPlat);

        // Somine: tek dev kare yerine uc ayri alev. Cok daha ikna edici.
        FireCluster("Salon_Somine", 40f, 2.6f, 1.9f);
        Fire("Salon_Alev1", 78f, 1.5f, 2.7f, 4.2f);
        Fire("Salon_Alev2", 36f, 15f, 2.1f, 3.3f);
        Fire("Salon_Alev3", 99f, 11f, 2.3f, 3.5f);

        // Dikey tirmanis — odanin tepesine kadar. MAVI anahtar en ustte:
        // buyuk odanin buyuk odulu. Yukselmeler 1.8, bosluklar 3-4.
        Solid(plats, "Salon_T1", 46f,  2.4f, 6f, 0.5f, CPlat);
        Solid(plats, "Salon_T2", 54f,  4.2f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T3", 62f,  6.0f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T4", 54f,  7.8f, 4f, 0.5f, CPlat);
        Solid(plats, "Salon_T5", 62f,  9.6f, 3.5f, 0.5f, CPlat);   // dar, hassas
        Solid(plats, "Salon_T6", 53f, 11.4f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T7", 62f, 13.2f, 4f, 0.5f, CPlat);
        Solid(plats, "Salon_T8", 54f, 15.0f, 5f, 0.5f, CPlat);
        Solid(plats, "Salon_T9", 62f, 16.8f, 5f, 0.5f, CPlat);

        Tap("Salon_Akvaryum", 37f, 0.7f, WaterSource.SourceType.OneShot, 100f,
            new Color(0.15f, 0.85f, 0.75f), 2.2f, 1.4f);

        Key(KeyColor.Mavi, 62f, 18.4f);
        Lock(KeyColor.Mavi, 102f);
    }

    // ==================================================================
    //  3. MUTFAK — 50x16. Su bol (kuvet de burada), YESIL anahtar.
    // ==================================================================

    static void Mutfak()
    {
        Prop("Mutfak_Buzdolabi", 107f, 3.4f, 4f, 6.8f, CProp);
        Prop("Mutfak_Fayans", 127f, 5f, 44f, 8f, new Color(0.17f, 0.16f, 0.15f), -10);

        Solid(plats, "Mutfak_Tezgah", 116f, 1.8f, 12f, 1.8f, CPlat);
        Solid(plats, "Mutfak_Masa", 132f, 2.0f, 7f, 0.6f, CPlat);
        Solid(plats, "Mutfak_Dolap1", 140f, 3.8f, 5f, 0.5f, CPlat);
        Solid(plats, "Mutfak_Dolap2", 133f, 5.6f, 4f, 0.5f, CPlat);

        Fire("Mutfak_Alev1", 148f, 1.4f, 2.4f, 3.7f);
        Fire("Mutfak_Alev2", 112f, 7.8f, 2.1f, 3.2f);

        Tap("Mutfak_Musluk", 120f, 3.4f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1.2f, 1.8f);
        Tap("Mutfak_Kuvet", 146f, 0.7f, WaterSource.SourceType.Infinite, 50f,
            new Color(0.25f, 0.60f, 0.95f), 6f, 0.8f);

        Key(KeyColor.Yesil, 133f, 7.2f);
        Lock(KeyColor.Yesil, 152f);
    }

    // ==================================================================
    //  4. KORIDOR — alcak tavan (6.5), SU YOK. Kitlik odasi. MOR anahtar.
    // ==================================================================

    static void Koridor()
    {
        Prop("Koridor_Tablo1", 160f, 4.5f, 2.2f, 2.6f, CProp);
        Prop("Koridor_Tablo2", 184f, 4.5f, 2.2f, 2.6f, CProp);
        Prop("Koridor_Hali", 170f, 0.15f, 30f, 0.3f, new Color(0.24f, 0.13f, 0.12f), 1);

        Solid(plats, "Koridor_DevrikDolap", 166f, 1.6f, 4.5f, 0.6f, CPlat);
        Solid(plats, "Koridor_Raf", 176f, 3.4f, 3.5f, 0.5f, CPlat);

        Fire("Koridor_Alev1", 156f, 1.3f, 2.1f, 3.3f);
        Fire("Koridor_Alev2", 185f, 1.3f, 2.1f, 3.3f);
        // Su YOK, yer atesi de YOK — kitlik hissi var ama kilitlenme riski yok.

        Key(KeyColor.Mor, 176f, 5.0f);
        Lock(KeyColor.Mor, 188f);
    }

    // ==================================================================
    //  5. COCUK ODASI — 38x22. Karabas. Sol yarisi NEFES bolgesi.
    // ==================================================================

    static void CocukOdasi()
    {
        Prop("Cocuk_DuvarKagidi", 207f, 7f, 36f, 13f, new Color(0.20f, 0.15f, 0.16f), -15);
        Prop("Cocuk_Yatak", 197f, 1f, 8f, 2f, CProp);
        Prop("Cocuk_Cizim", 203f, 9f, 2.4f, 2.8f, new Color(0.30f, 0.26f, 0.20f));
        Prop("Cocuk_BoyCizgisi", 191f, 5f, 0.5f, 10f, new Color(0.28f, 0.24f, 0.20f));

        Solid(plats, "Cocuk_Ranza", 197f, 2.6f, 8f, 0.6f, CPlat);
        Solid(plats, "Cocuk_Sandik", 208f, 0.9f, 3.5f, 1.8f, CPlat);
        Solid(plats, "Cocuk_Raf", 216f, 3.4f, 4f, 0.5f, CPlat);

        Fire("Cocuk_Alev1", 223f, 1.3f, 2.2f, 3.4f);
        Fire("Cocuk_Alev2", 190f, 11f, 2.3f, 3.5f);
    }

    // ==================================================================
    //  6. ARKA KORIDOR — 68x16. Kacis. Karabas yaninizda. OLUM burada.
    // ==================================================================

    static void ArkaKoridor()
    {
        Prop("Arka_Tugla", 260f, 5.5f, 62f, 10f, new Color(0.16f, 0.13f, 0.12f), -12);
        Prop("Arka_Boru", 244f, 8.6f, 30f, 0.8f, new Color(0.24f, 0.20f, 0.18f), -3);

        Solid(plats, "Arka_Kasa1", 238f, 1.6f, 5f, 0.6f, CPlat);
        Solid(plats, "Arka_Kasa2", 250f, 3.2f, 4f, 0.5f, CPlat);
        Solid(plats, "Arka_Kasa3", 262f, 1.8f, 5f, 0.6f, CPlat);
        Solid(plats, "Arka_Kiris", 276f, 3.6f, 6f, 0.5f, CPlat);
        Solid(plats, "Arka_Kasa4", 288f, 1.8f, 4f, 0.6f, CPlat);

        Fire("Arka_Alev1", 232f, 1.4f, 2.3f, 3.5f);
        Fire("Arka_Alev2", 256f, 7.8f, 2.1f, 3.2f);
        Fire("Arka_Alev3", 284f, 1.4f, 2.4f, 3.6f);

        // Olumden ONCE dolsun
        Tap("Arka_Musluk", 240f, 0.8f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1.2f, 1.6f);

        // OLUM ANI — odanin ~%55'i. Sonrasinda 74 birim yalnizsiniz.
        var death = Sprite2(debris, "Tahta_OLUM (EndingSequence -> Death Debris)",
                            264f, 9.4f, 4f, 0.5f, new Color(0.5f, 0.3f, 0.16f));
        death.AddComponent<BoxCollider2D>().isTrigger = true;
        var dfd = death.AddComponent<FallingDebris>();
        dfd.groundLayer = LayerMask.GetMask("Ground");
        dfd.autoTrigger = false;
        dfd.groundMarker = null;
        dfd.lifetimeAfterLanding = 0f;

        Beam(3, 246f, 9.4f, "Arka koridor");
        Beam(4, 282f, 9.4f, "Arka koridor - yalniz bolum");
    }

    // ==================================================================
    //  7. ARKA KAPI — 42x20. Yalniz, BOSS, cikis.
    // ==================================================================

    static void ArkaKapi()
    {
        Prop("ArkaKapi_Tugla", 315f, 6.5f, 40f, 12f, new Color(0.16f, 0.13f, 0.12f), -12);
        Prop("ArkaKapi_Kapi", 333f, 2.2f, 3f, 4.4f, new Color(0.34f, 0.24f, 0.14f), -1);

        Solid(plats, "ArkaKapi_Kasa1", 302f, 1.8f, 5f, 0.6f, CPlat);
        Solid(plats, "ArkaKapi_Kasa2", 312f, 3.4f, 4f, 0.5f, CPlat);
        Solid(plats, "ArkaKapi_Kasa3", 321f, 1.8f, 5f, 0.6f, CPlat);

        Fire("ArkaKapi_Alev1", 298f, 1.5f, 2.5f, 3.9f);
        Fire("ArkaKapi_Alev2", 318f, 9.2f, 2.2f, 3.4f);

        // Yalniz bolumun sigortasi: sonsuz ama YAVAS. Beklemek zorundasiniz,
        // o sirada atesler buyuyor. Kilitlenme yok, baski var.
        Tap("ArkaKapi_PatlakBoru", 306f, 1.2f, WaterSource.SourceType.Infinite, 15f,
            new Color(0.22f, 0.52f, 0.85f), 0.8f, 2f);

        Beam(5, 310f, 11.4f, "Arka kapi - yalniz bolum");
    }

    // ==================================================================

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
        fd.triggerHeight = 20f;
        fd.autoTrigger = true;
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
        var go = Sprite2(keys, $"KilitliKapi_{c}", x, DoorH * 0.5f, 1.8f, DoorH, CDoor, 1);

        var blocker = go.AddComponent<BoxCollider2D>();
        blocker.isTrigger = false;

        var sense = go.AddComponent<BoxCollider2D>();
        sense.isTrigger = true;
        sense.size = new Vector2(2.2f, 1.2f);

        var ld = go.AddComponent<LockedDoor>();
        ld.requiredKey = c;
        ld.blocker = blocker;
        ld.openHeight = DoorH + 0.4f;
    }

    // ==================================================================

    static void BuildTriggers()
    {
        var death = Zone("Tetik_KopeginOlumu", 264f, 2f, 2f, 4f,
                         EndingTriggerZone.What.DogDeath);
        death.requiredKeyCount = 3;

        var boss = Zone("Tetik_BossSahnesi", 300f, 2f, 2f, 4f,
                        EndingTriggerZone.What.BossScene);
        boss.requires = death;

        var exit = new GameObject("Tetik_EvdenCikis");
        exit.transform.SetParent(triggers, false);
        exit.transform.position = new Vector3(334f, 2f, 0f);
        var ecol = exit.AddComponent<BoxCollider2D>();
        ecol.isTrigger = true;
        ecol.size = new Vector2(3f, 5f);
        exit.AddComponent<EscapeTrigger>();

        var ge = new GameObject("GameEnding");
        ge.transform.SetParent(triggers, false);
        ge.AddComponent<GameEnding>();
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
        (SpawnMarker.Kind.PlayerStart,   6f, 1f, "Oyuncu baslangici"),
        (SpawnMarker.Kind.DogSilhouette,14f, 0.8f, "Kopek mamasi - 'burada bir kopek yasiyor' ipucu"),
        (SpawnMarker.Kind.FlameWall,   188f, 2f, "GERI DONUS KAPANIR. Karabas kurtulunca burada alev duvari yukselir, oyuncu ileri gitmek zorunda kalir. GORUNUR sekilde olsun."),
        (SpawnMarker.Kind.Boss,        328f, 1f, "Dev alev, arka kapiyi kapatir. Cani hic azalmaz."),

        (SpawnMarker.Kind.WallMonster,  21f, 1f, "Antre - ilk, ZAYIF (can 40)"),
        (SpawnMarker.Kind.WallMonster,  68f,12f, "Salon kitapligi - YUKSEK, tirmanis rotasinda"),
        (SpawnMarker.Kind.WallMonster,  92f, 4f, "Salon"),
        (SpawnMarker.Kind.WallMonster, 126f, 8f, "Mutfak"),
        (SpawnMarker.Kind.WallMonster, 158f, 1f, "Koridor 1/3 - su YOK, 2'sine yeter"),
        (SpawnMarker.Kind.WallMonster, 170f, 4f, "Koridor 2/3 - YUKSEK"),
        (SpawnMarker.Kind.WallMonster, 184f, 1f, "Koridor 3/3"),
        (SpawnMarker.Kind.WallMonster, 250f, 6f, "Arka koridor - Karabas'li"),
        (SpawnMarker.Kind.WallMonster, 316f, 6f, "Arka kapi - YALNIZ"),

        (SpawnMarker.Kind.FloorFire,   124f, 0.5f, "Mutfak"),
        (SpawnMarker.Kind.FloorFire,   142f, 0.5f, "Mutfak"),
        (SpawnMarker.Kind.FloorFire,   212f, 0.5f, "Cocuk odasi"),

        (SpawnMarker.Kind.FloorFire,   234f, 0.5f, "KACIS Karabas'li - SEYREK (8 birim ara)"),
        (SpawnMarker.Kind.FloorFire,   244f, 0.5f, "KACIS Karabas'li"),
        (SpawnMarker.Kind.FloorFire,   254f, 0.5f, "KACIS Karabas'li - olumden hemen once"),

        (SpawnMarker.Kind.FloorFire,   270f, 0.5f, "YALNIZ - 4 birim ara. Yari mesafe, sifir yardim."),
        (SpawnMarker.Kind.FloorFire,   274f, 0.5f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,   279f, 0.5f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,   286f, 0.5f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,   296f, 0.5f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,   304f, 0.5f, "YALNIZ"),
        (SpawnMarker.Kind.FloorFire,   314f, 0.5f, "YALNIZ - son"),

        (SpawnMarker.Kind.Dog,         196f, 1f, "KARABAS - sikismis halde baslar"),
        (SpawnMarker.Kind.CoveringFire,196f, 1f, "Karabas'i orten ates (can 50). Sondurulunce kurtulur."),
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

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[ALEV] Main Camera yok."); return; }

        cam.orthographic = true;
        // ⚠️ 7 -> 5.5. Eskiden oyuncu ekran yuksekliginin sadece %11'iydi,
        // her sey uzaktan bakilan minik noktalar gibi duruyordu. 5.5'te
        // oyuncu %15 — platformcularda normal aralik bu.
        cam.orthographicSize = 5.5f;
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

    /// <summary>
    /// Dekoratif alev.
    ///
    /// ⚠️ ONEMLI DERSLER (ilk hali cok kotu duruyordu):
    ///   1. Kare sprite ASLA ates gibi gorunmez. Ates bir SILUET meselesi.
    ///      Artik proseduel uretilen alev dili sekli kullaniliyor.
    ///   2. Buyuk ates = turuncu blok. Kucuk ve INCE alevler ikna edicidir.
    ///      Buyuk ates isteniyorsa tek buyuk yerine BIRKAC KUCUK kullanin.
    ///   3. sortingOrder POZITIF olmamali — dekoratif ates oyuncunun onune
    ///      gecerse ekrani kirletir. Artik arkada (-3), isik daha da arkada.
    /// </summary>
    static void Fire(string name, float x, float y, float w, float h, float glowScale = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(fires, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(w, h, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = flameSprite;              // alev dili sekli, renkleri icinde
        sr.color = Color.white;               // tint YOK, dokudaki renkler gecerli
        sr.sortingOrder = -3;                 // oyuncunun ARKASINDA

        var g = new GameObject("Isik");
        g.transform.SetParent(go.transform, false);
        // Isik halkasi daha kucuk ve daha soluk — eskiden dev turuncu
        // lekeler olusturuyordu.
        g.transform.localScale = new Vector3(5.5f * glowScale / Mathf.Max(w, 0.01f),
                                             5.5f * glowScale / Mathf.Max(h, 0.01f), 1f);
        var gsr = g.AddComponent<SpriteRenderer>();
        gsr.sprite = glow;
        gsr.color = new Color(1f, 0.45f, 0.12f, 0.18f);
        gsr.sortingOrder = -4;

        go.AddComponent<AmbientFire>().glow = gsr;
    }

    /// <summary>Buyuk ates: tek dev kare yerine farkli boyda birkac alev.</summary>
    static void FireCluster(string name, float x, float y, float scale = 1f)
    {
        Fire($"{name}_a", x - 1.1f * scale, y, 1.0f * scale, 1.9f * scale, 0.7f);
        Fire($"{name}_b", x, y + 0.25f * scale, 1.4f * scale, 2.7f * scale, 1.1f);
        Fire($"{name}_c", x + 1.1f * scale, y, 1.0f * scale, 2.0f * scale, 0.7f);
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

    /// <summary>
    /// Alev dili sekli uretir — kare sprite ates gibi gorunmuyordu.
    ///
    /// Profil: dipte dar, %30 yukseklikte en genis, tepede sivri.
    /// Renk dokunun ICINE bakiliyor: cekirdek sari-beyaz, ortasi turuncu,
    /// kenari koyu kirmizi. Boylece tint gerekmiyor ve ates "sicak"
    /// gorunuyor — tek renk turuncu bir sekil hep yassi durur.
    /// </summary>
    public static Sprite GetOrCreateFlameSprite()
    {
        const string path = "Assets/Art/Generated/Flame.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder("Assets/Art/Generated"))
            AssetDatabase.CreateFolder("Assets/Art", "Generated");

        const int W = 64, H = 96;
        var tex = new Texture2D(W, H);
        var px = new Color[W * H];

        var core = new Color(1.00f, 0.95f, 0.62f);   // sari-beyaz cekirdek
        var mid  = new Color(1.00f, 0.55f, 0.10f);   // turuncu
        var edge = new Color(0.85f, 0.20f, 0.04f);   // koyu kirmizi kenar

        for (int yi = 0; yi < H; yi++)
        {
            float y = yi / (float)(H - 1);           // 0 = dip, 1 = tepe

            // Alev profili: dipte dar, %30'da en genis, tepede sivri
            float bulge = Mathf.Sin(Mathf.Clamp01(y / 0.3f) * Mathf.PI * 0.5f);
            float halfW = 0.5f * (1f - y) * (0.55f + 0.45f * bulge);

            for (int xi = 0; xi < W; xi++)
            {
                float x = xi / (float)(W - 1) - 0.5f;   // -0.5 .. 0.5
                float ax = Mathf.Abs(x);

                if (halfW <= 0.001f || ax > halfW) { px[yi * W + xi] = Color.clear; continue; }

                float d = ax / halfW;                   // 0 = eksen, 1 = kenar

                // Renk: cekirdekten kenara. Tepeye dogru cekirdek soner.
                float coreStrength = Mathf.Clamp01(1f - y * 1.3f);
                Color c = d < 0.45f
                    ? Color.Lerp(core, mid, d / 0.45f)
                    : Color.Lerp(mid, edge, (d - 0.45f) / 0.55f);
                c = Color.Lerp(mid, c, 0.35f + 0.65f * coreStrength);

                // Kenarlarda ve tepede yumusak sonme
                float a = Mathf.Clamp01((1f - d) * 3f) * Mathf.Clamp01((1f - y) * 2.2f + 0.15f);
                c.a = a;

                px[yi * W + xi] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var fimp = (TextureImporter)AssetImporter.GetAtPath(path);
        fimp.textureType = TextureImporterType.Sprite;
        fimp.alphaIsTransparency = true;
        fimp.textureCompression = TextureImporterCompression.Uncompressed;
        fimp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
