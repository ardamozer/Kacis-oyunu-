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

    // ── RENK DILI ──
    // En onemli kural: BASILABILIR seyler ACIK ve SICAK, dekor KOYU ve SONUK.
    // Ilk surumde hepsi ayni kahverengiydi ve oyuncu neye basabilecegini
    // anlayamiyordu. Bu bir estetik degil OYNANABILIRLIK meselesi.
    static readonly Color CWall    = new Color(0.11f, 0.09f, 0.09f);
    static readonly Color CFloor   = new Color(0.19f, 0.15f, 0.13f);
    static readonly Color CPlat    = new Color(0.44f, 0.32f, 0.21f);   // ACIK — basilir
    static readonly Color CPlatTop = new Color(0.68f, 0.50f, 0.30f);   // ust kenar parlak
    static readonly Color CProp    = new Color(0.135f, 0.110f, 0.105f);  // KOYU — dekor
    static readonly Color CProp2   = new Color(0.165f, 0.130f, 0.120f);  // dekor, biraz acik
    static readonly Color CDoor    = new Color(0.36f, 0.22f, 0.14f);
    static readonly Color CBack    = new Color(0.135f, 0.105f, 0.10f);
    static readonly Color CTrim    = new Color(0.19f, 0.145f, 0.125f);   // supurgelik
    static readonly Color CGlass   = new Color(0.105f, 0.125f, 0.155f);  // pencere cami

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

        // Her odanin kendi tavani + duvar detayi. Tintler farkli: odalar
        // birbirinin ayni gorunuyordu, hafif renk kimligi ayirt ettiriyor.
        var tints = new[]
        {
            new Color(0.150f, 0.115f, 0.100f),  // Antre  — sicak kahve
            new Color(0.165f, 0.120f, 0.110f),  // Salon  — daha zengin
            new Color(0.130f, 0.135f, 0.125f),  // Mutfak — soguk, fayans
            new Color(0.120f, 0.100f, 0.100f),  // Koridor— koyu, bogucu
            new Color(0.165f, 0.130f, 0.140f),  // Cocuk  — pembemsi
            new Color(0.125f, 0.115f, 0.110f),  // Arka   — servis alani
            new Color(0.130f, 0.110f, 0.105f),  // ArkaKapi
        };

        for (int i = 0; i < RoomDefs.Length; i++)
        {
            var (name, x0, x1, h) = RoomDefs[i];
            Solid(geo, $"Tavan_{name}", (x0 + x1) * 0.5f, h + 0.5f, x1 - x0, 1f, CWall);
            WallDetail(name, x0, x1, h, tints[i]);
        }

        // Koridor'un ALCAK TAVANI — oynanabilir yukseklik 6.5, bogucu
        Solid(geo, "Koridor_AlcakTavan", 170f, 8.75f, 36f, 4.5f, CWall);

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
        // Giris holu: portmanto, ayna, pencere. Her sey zemine ya da
        // duvara BAGLI — havada duran esya yok.
        Prop("Antre_Portmanto", 3f, 3f, 1f, 6f, CProp2);
        Prop("Antre_Palto1", 2.2f, 4.6f, 1.4f, 2.8f, new Color(0.24f, 0.14f, 0.12f), -1);
        Prop("Antre_Palto2", 3.9f, 4.4f, 1.3f, 2.6f, new Color(0.18f, 0.16f, 0.20f), -1);

        Frame("Antre_Ayna", 17f, 5.4f, 2.2f, 3f, new Color(0.16f, 0.18f, 0.21f));
        Window("Antre_Pencere", 28f, 6f, 3.4f, 3.4f);

        Prop("Antre_Hali", 17f, 0.2f, 13f, 0.4f, new Color(0.27f, 0.14f, 0.12f), 1);

        Plat("Antre_Ayakkabilik", 9f, 0.8f, 4f, 1.6f);
        Plat("Antre_Konsol", 24f, 1.9f, 5f);
        Prop("Antre_KonsolAyak1", 22f, 1f, 0.35f, 2.2f, CProp2, -1);
        Prop("Antre_KonsolAyak2", 26f, 1f, 0.35f, 2.2f, CProp2, -1);

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
        // ---- Somine: ocak + davlumbaz + rafi ----
        Prop("Salon_SomineGovde", 40f, 3.2f, 9f, 6.4f, CProp2, -6);
        Prop("Salon_SomineIc", 40f, 2.2f, 6f, 4f, new Color(0.09f, 0.06f, 0.06f), -5);
        Prop("Salon_SomineRaf", 40f, 6.6f, 10f, 0.6f, CTrim, -4);
        FireCluster("Salon_Somine", 40f, 2.6f, 1.9f);

        // ---- Kitapliklar: raflari GORUNEN ----
        Shelf("Salon_Kitaplik1", 66f, 1f, 4.5f, 5);
        Shelf("Salon_Kitaplik2", 95f, 1f, 4f, 4);

        // ---- Buyuk pencere ve perdeler ----
        Window("Salon_Pencere", 80f, 9f, 7f, 7f);
        Prop("Salon_Perde1", 75.5f, 9f, 2f, 9f, new Color(0.26f, 0.13f, 0.13f), -2);
        Prop("Salon_Perde2", 84.5f, 9f, 2f, 9f, new Color(0.26f, 0.13f, 0.13f), -2);

        // ---- Avize: tavana ZINCIRLE bagli ----
        Prop("Salon_AvizeZincir", 58f, 18.4f, 0.25f, 3.2f, CTrim, -3);
        Prop("Salon_Avize", 58f, 16.4f, 5f, 1f, new Color(0.38f, 0.30f, 0.15f), -2);
        Fire("Salon_AvizeAtes", 58f, 15.6f, 1.2f, 1.9f, 0.8f);

        Prop("Salon_Hali", 62f, 0.2f, 24f, 0.4f, new Color(0.27f, 0.14f, 0.12f), 1);
        Frame("Salon_Tablo", 50f, 12f, 3f, 3.6f, new Color(0.20f, 0.15f, 0.14f));

        // ---- Basilabilir mobilya ----
        Plat("Salon_Koltuk", 50f, 1.3f, 8f, 1.8f);
        Prop("Salon_KoltukSirt", 50f, 2.8f, 8f, 1.6f, CProp2, -1);
        Plat("Salon_Piyano", 88f, 1.3f, 9f, 2.4f);
        Plat("Salon_PiyanoTabure", 81f, 0.7f, 3f, 1.4f);

        // ---- Tirmanis: her platform duvara DIREKLE bagli, havada durmuyor ----
        ClimbStep("Salon_T1", 46f, 1.9f, 6f);
        ClimbStep("Salon_T2", 54f, 4.2f, 5f);
        ClimbStep("Salon_T3", 62f, 6.0f, 5f);
        ClimbStep("Salon_T4", 54f, 7.8f, 4f);
        ClimbStep("Salon_T5", 62f, 9.6f, 3.5f);
        ClimbStep("Salon_T6", 53f, 11.4f, 5f);
        ClimbStep("Salon_T7", 62f, 13.2f, 4f);
        ClimbStep("Salon_T8", 54f, 15.0f, 5f);
        ClimbStep("Salon_T9", 62f, 16.8f, 5f);

        Fire("Salon_Alev1", 78f, 1.5f, 2.7f, 4.2f);
        Fire("Salon_Alev3", 99f, 11f, 2.3f, 3.5f);

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
        // Fayans bandi — mutfak kimligi
        Prop("Mutfak_Fayans", 127f, 5.5f, 46f, 5f, new Color(0.155f, 0.16f, 0.155f), -16);
        for (int i = 0; i < 10; i++)
            Prop($"Mutfak_FayansCizgi{i}", 105f + i * 4.6f, 5.5f, 0.14f, 5f,
                 new Color(0.155f, 0.163f, 0.155f), -15);

        Prop("Mutfak_Buzdolabi", 106f, 3.4f, 4.2f, 6.8f, CProp2, -6);
        Prop("Mutfak_BuzdolabiKapi", 106f, 3.4f, 0.2f, 6.6f, CProp, -5);

        // Ust dolaplar — tek blok yerine ayri kapaklar
        for (int i = 0; i < 4; i++)
            Prop($"Mutfak_UstDolap{i}", 116f + i * 4.4f, 7.4f, 4f, 2.6f, CProp2, -6);

        Window("Mutfak_Pencere", 134f, 7.6f, 4f, 3f);

        // Asili tencereler
        for (int i = 0; i < 3; i++)
            Prop($"Mutfak_Tencere{i}", 124f + i * 2.2f, 5.6f, 1.2f, 1.4f, CTrim, -2);

        Plat("Mutfak_Tezgah", 116f, 0.9f, 14f, 1.8f);
        Prop("Mutfak_TezgahAlti", 116f, 0.4f, 13f, 0.9f, CProp, -1);
        Plat("Mutfak_Masa", 132f, 2.0f, 8f);
        Prop("Mutfak_MasaAyak1", 129f, 1f, 0.3f, 2f, CProp2, -1);
        Prop("Mutfak_MasaAyak2", 135f, 1f, 0.3f, 2f, CProp2, -1);
        Plat("Mutfak_Dolap1", 140f, 3.8f, 5f);
        Plat("Mutfak_Dolap2", 133f, 5.6f, 4f);

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
        // Koridor = duvarinda SIRA SIRA fotograf olan yer. Kimligi bu.
        Frame("Koridor_Foto0", 158f, 4.8f, 1.6f, 2.0f, new Color(0.145f, 0.125f, 0.115f));
        Frame("Koridor_Foto1", 164f, 4.4f, 2.2f, 1.6f, new Color(0.145f, 0.125f, 0.115f));
        Frame("Koridor_Foto2", 181f, 4.7f, 1.4f, 1.8f, new Color(0.145f, 0.125f, 0.115f));

        // Duvar aplikleri
        for (int i = 0; i < 3; i++)
        {
            Prop($"Koridor_Aplik{i}", 160f + i * 9f, 5.6f, 0.7f, 1f, CTrim, -2);
            Fire($"Koridor_AplikAtes{i}", 160f + i * 9f, 6.2f, 0.8f, 1.2f, 0.5f);
        }

        Prop("Koridor_Hali", 170f, 0.2f, 32f, 0.4f, new Color(0.25f, 0.13f, 0.12f), 1);

        Plat("Koridor_DevrikDolap", 166f, 1.5f, 4.5f, 0.6f);
        Plat("Koridor_Basamak1", 170.5f, 1.5f, 3f);
        Plat("Koridor_Basamak2", 174f, 2.9f, 3f);
        Plat("Koridor_Raf", 177.5f, 4.2f, 3.5f);
        Prop("Koridor_RafDirek", 177.5f, 3.2f, 0.3f, 1.6f, CProp2, -1);

        Fire("Koridor_Alev1", 156f, 1.3f, 2.1f, 3.3f);
        Fire("Koridor_Alev2", 185f, 1.3f, 2.1f, 3.3f);

        Key(KeyColor.Mor, 177.5f, 5.6f);
        Lock(KeyColor.Mor, 188f);
    }

    // ==================================================================
    //  5. COCUK ODASI — 38x22. Karabas. Sol yarisi NEFES bolgesi.
    // ==================================================================

    static void CocukOdasi()
    {
        // Cocuk odasi: cizimler, oyuncaklar, boy cizgileri. Bu odanin
        // isini duygu yapiyor — oyuncu burada birinin yasadigini anlamali.
        Frame("Cocuk_Cizim0", 202f, 8.6f, 1.8f, 2.2f, new Color(0.24f, 0.21f, 0.155f));
        Frame("Cocuk_Cizim1", 207f, 8.0f, 2.4f, 1.8f, new Color(0.24f, 0.21f, 0.155f));
        Frame("Cocuk_Cizim2", 213f, 8.8f, 1.5f, 1.9f, new Color(0.24f, 0.21f, 0.155f));

        Window("Cocuk_Pencere", 220f, 8f, 3.6f, 3.4f);

        Prop("Cocuk_BoyCizgisi", 190f, 5f, 0.4f, 9f, new Color(0.30f, 0.26f, 0.22f), -2);
        for (int i = 0; i < 5; i++)
            Prop($"Cocuk_BoyIsaret{i}", 190.6f, 2f + i * 1.7f, 1f, 0.14f,
                 new Color(0.34f, 0.30f, 0.24f), -1);

        // Ranza: iki katli, alt yatak dekor, ust kat basilabilir
        Prop("Cocuk_RanzaGovde", 197f, 1.6f, 8.4f, 3.2f, CProp2, -5);
        Prop("Cocuk_Yatak", 197f, 0.9f, 7.6f, 1.2f, new Color(0.26f, 0.18f, 0.20f), -4);
        Plat("Cocuk_Ranza", 197f, 2.2f, 8f, 0.6f);
        Plat("Cocuk_Tabure", 202.5f, 0.7f, 2.4f, 1.4f);

        Plat("Cocuk_Sandik", 208f, 0.9f, 3.5f, 1.8f);
        Plat("Cocuk_Raf", 213f, 3.2f, 4f);
        Prop("Cocuk_RafDirek", 213f, 2.2f, 0.3f, 1.6f, CProp2, -1);

        // Yerde oyuncaklar
        for (int i = 0; i < 4; i++)
            Prop($"Cocuk_Oyuncak{i}", 204f + i * 3.1f, 0.5f, 0.9f, 0.9f,
                 new Color(0.30f + i * 0.06f, 0.20f, 0.16f), 1);

        Fire("Cocuk_Alev1", 223f, 1.3f, 2.2f, 3.4f);
        Fire("Cocuk_Alev2", 190f, 11f, 2.3f, 3.5f);
    }

    // ==================================================================
    //  6. ARKA KORIDOR — 68x16. Kacis. Karabas yaninizda. OLUM burada.
    // ==================================================================

    static void ArkaKoridor()
    {
        // Servis koridoru: borular, sayaclar, kablolar, sandiklar.
        // Evin "arka tarafi" hissi — sussuz, islevsel.
        Prop("Arka_Boru1", 244f, 8.8f, 34f, 0.9f, new Color(0.26f, 0.22f, 0.19f), -4);
        Prop("Arka_Boru2", 272f, 7.4f, 40f, 0.7f, new Color(0.22f, 0.19f, 0.17f), -4);
        for (int i = 0; i < 7; i++)
            Prop($"Arka_BoruKelepce{i}", 232f + i * 9f, 8.8f, 0.6f, 1.5f, CTrim, -3);

        for (int i = 0; i < 3; i++)
            Prop($"Arka_Sayac{i}", 236f + i * 20f, 5.4f, 2.4f, 2.8f, CProp2, -3);

        Plat("Arka_Kasa1", 238f, 1.6f, 5f, 0.6f);
        Plat("Arka_Kasa2", 245f, 3.2f, 4f);
        Plat("Arka_Kasa3", 262f, 1.8f, 5f, 0.6f);
        Plat("Arka_Kiris", 270f, 3.6f, 6f);
        Plat("Arka_Kasa4", 288f, 1.8f, 4f, 0.6f);

        Prop("Arka_KasaGovde1", 238f, 0.8f, 4.6f, 1.6f, CProp2, -1);
        Prop("Arka_KasaGovde3", 262f, 0.9f, 4.6f, 1.8f, CProp2, -1);
        Prop("Arka_KasaGovde4", 288f, 0.9f, 3.6f, 1.8f, CProp2, -1);

        Fire("Arka_Alev1", 232f, 1.4f, 2.3f, 3.5f);
        Fire("Arka_Alev2", 256f, 7.8f, 2.1f, 3.2f);
        Fire("Arka_Alev3", 284f, 1.4f, 2.4f, 3.6f);

        Tap("Arka_Musluk", 240f, 0.8f, WaterSource.SourceType.Infinite, 30f,
            new Color(0.25f, 0.60f, 0.95f), 1.2f, 1.6f);

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
        // Cikis: kapinin camindan disarisi gorunuyor — kurtulus goze
        // gorunur olsun, oyuncu neye dogru kostugunu bilsin.
        Prop("ArkaKapi_Kasa", 333f, 2.6f, 4.2f, 5.6f, new Color(0.30f, 0.21f, 0.13f), -4);
        Prop("ArkaKapi_Kapi", 333f, 2.4f, 3.2f, 4.8f, new Color(0.36f, 0.24f, 0.14f), -3);
        Prop("ArkaKapi_Cam", 333f, 3.6f, 1.8f, 1.6f, new Color(0.22f, 0.26f, 0.30f), -2);
        Prop("ArkaKapi_Kol", 331.8f, 2.2f, 0.5f, 0.4f, CTrim, -1);

        Prop("ArkaKapi_Sigorta", 300f, 6.2f, 2.6f, 3.2f, CProp2, -3);
        for (int i = 0; i < 4; i++)
            Prop($"ArkaKapi_Kablo{i}", 302f + i * 7f, 9.4f, 6f, 0.3f,
                 new Color(0.20f, 0.17f, 0.15f), -3);

        Plat("ArkaKapi_Kasa1", 302f, 1.8f, 5f, 0.6f);
        Plat("ArkaKapi_Kasa2", 308f, 3.4f, 4f);
        Plat("ArkaKapi_Kasa3", 321f, 1.8f, 5f, 0.6f);
        Prop("ArkaKapi_KasaGovde1", 302f, 0.9f, 4.6f, 1.8f, CProp2, -1);
        Prop("ArkaKapi_KasaGovde3", 321f, 0.9f, 4.6f, 1.8f, CProp2, -1);

        Fire("ArkaKapi_Alev1", 298f, 1.5f, 2.5f, 3.9f);
        Fire("ArkaKapi_Alev2", 318f, 9.2f, 2.2f, 3.4f);

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

    // ==================================================================
    //  GORSEL YARDIMCILAR
    // ==================================================================

    /// <summary>
    /// Basilabilir platform + UST KENARINDA parlak serit.
    ///
    /// O serit dekoratif degil, ISLEVSEL: oyuncuya "burasi basilir" diyor.
    /// Ust kenari vurgulanmamis platform, ayni renkteki mobilyadan ayirt
    /// edilemiyordu.
    /// </summary>
    static void Plat(string name, float x, float y, float w, float h = 0.5f)
    {
        Solid(plats, name, x, y, w, h, CPlat);
        Sprite2(plats, name + "_Kenar", x, y + h * 0.5f - 0.06f, w, 0.14f, CPlatTop, 1);
    }

    /// <summary>
    /// Tirmanis basamagi: platform + altinda DIREK.
    /// Direksiz platformlar boslukta yuzen tahtalar gibi duruyordu;
    /// destegi gorunen bir raf "buraya konmus" hissi veriyor.
    /// </summary>
    static void ClimbStep(string name, float x, float y, float w)
    {
        Plat(name, x, y, w);
        Prop(name + "_Direk1", x - w * 0.35f, y - 0.9f, 0.3f, 1.8f, CProp2, -1);
        Prop(name + "_Direk2", x + w * 0.35f, y - 0.9f, 0.3f, 1.8f, CProp2, -1);
    }

    /// <summary>Duvara asili tablo/ayna — cercevesiyle, bosluga asilmasin diye.</summary>
    static void Frame(string name, float x, float y, float w, float h, Color inner)
    {
        Prop(name + "_Cerceve", x, y, w + 0.3f, h + 0.3f, new Color(0.21f, 0.155f, 0.11f), -3);
        Prop(name, x, y, w, h, inner, -2);
    }

    /// <summary>Pencere — cam + cercevesi + hac seklinde bolme.</summary>
    static void Window(string name, float x, float y, float w, float h)
    {
        Prop(name + "_Cerceve", x, y, w + 0.5f, h + 0.5f, new Color(0.20f, 0.155f, 0.115f), -4);
        Prop(name + "_Cam", x, y, w, h, CGlass, -3);
        Prop(name + "_Dikey", x, y, 0.18f, h, new Color(0.20f, 0.155f, 0.115f), -2);
        Prop(name + "_Yatay", x, y, w, 0.18f, new Color(0.20f, 0.155f, 0.115f), -2);
    }

    /// <summary>Raflari GORUNEN kitaplik — duz kahverengi dikdortgen yerine.</summary>
    static void Shelf(string name, float x, float yBottom, float w, int shelves)
    {
        float step = 1.7f;
        Prop(name + "_Govde", x, yBottom + shelves * step * 0.5f, w, shelves * step, CProp2, -4);
        for (int i = 0; i <= shelves; i++)
            Prop($"{name}_Raf{i}", x, yBottom + i * step, w, 0.22f, CTrim, -2);
        // Kitaplar
        for (int i = 0; i < shelves; i++)
            for (int j = 0; j < 3; j++)
                Prop($"{name}_Kitap{i}{j}", x - w * 0.3f + j * w * 0.3f,
                     yBottom + i * step + 0.75f, w * 0.22f, 1.1f,
                     new Color(0.20f + j * 0.035f, 0.115f, 0.10f), -1);
    }

    /// <summary>
    /// Odanin duvar detayi: supurgelik, duvar kagidi cizgileri, tavan silmesi.
    /// Duz siyah duvar "ic mekan" hissi vermiyordu.
    /// </summary>
    static void WallDetail(string name, float x0, float x1, float h, Color tint)
    {
        float w = x1 - x0, cx = (x0 + x1) * 0.5f;

        Prop($"Duvar_{name}", cx, h * 0.5f, w - 1f, h - 1f, tint, -19);
        Prop($"Supurgelik_{name}", cx, 0.55f, w - 1f, 0.5f, CTrim, -8);
        Prop($"TavanSilme_{name}", cx, h - 1.2f, w - 1f, 0.4f, CTrim, -8);

        // Dikey duvar kagidi cizgileri — yuzey duz kalmasin
        int stripes = Mathf.Max(3, Mathf.RoundToInt(w / 5.5f));
        for (int i = 0; i < stripes; i++)
        {
            float sx = x0 + (i + 0.5f) * w / stripes;
            Prop($"Cizgi_{name}_{i}", sx, (h + 1f) * 0.5f, 0.16f, h - 2f,
                 tint * 1.12f, -18);
        }
    }

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
