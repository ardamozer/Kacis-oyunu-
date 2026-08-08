using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Test sahnesini tek tikla kurar. Menuden: ALEV → Test Sahnesini Kur
///
/// Elle 20 alan baglamak yerine bunu calistirin. Tekrar calistirilabilir —
/// bir sey bozulursa yeniden basip sifirdan kurarsiniz.
///
/// Bu bir EDITOR script'i (Assets/Editor klasorunde), oyuna dahil edilmez.
/// </summary>
public static class AlevSceneSetup
{
    const string SpriteFolder = "Assets/Art/Generated";
    const string SpritePath = SpriteFolder + "/WhiteSquare.png";

    // Sprite 16x16 px, Pixels Per Unit 16 → 1 kare = 1 dunya birimi.
    // Boylece Inspector'daki Scale degerleri dogrudan birim karsiligi olur.
    const int SpriteSize = 16;
    const int PixelsPerUnit = 16;

    [MenuItem("ALEV/Test Sahnesini Kur", false, 0)]
    public static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog(
                "Test sahnesini kur",
                "Mevcut sahnedeki Ground / Player / Musluk / TestTarget objeleri " +
                "silinip yeniden olusturulacak.\n\nDevam edilsin mi?",
                "Kur", "Vazgec"))
        {
            return;
        }

        Sprite square = GetOrCreateSquareSprite();

        RemoveIfExists("Ground", "Player", "Musluk", "Akvaryum",
                       "TestTarget_Su", "TestTarget_Ezme", "HUD");

        SetupCamera();

        CreateGround(square);
        GameObject player = CreatePlayer(square);
        CreateMusluk(square);
        CreateAkvaryum(square);
        CreateTestTargets(square);
        CreateHUD(square, player);

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<b>[ALEV]</b> Sahne kuruldu. Play'e basin.\n" +
                  "A/D veya ok tuslari: yurume · Space: zipla · " +
                  "Sol fare (veya Ctrl): su sik · Yukari ok + su: yukari nisan");
    }

    // ------------------------------------------------------------------
    //  Sprite
    // ------------------------------------------------------------------

    static Sprite GetOrCreateSquareSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(SpriteFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Generated");

        var tex = new Texture2D(SpriteSize, SpriteSize);
        var pixels = new Color[SpriteSize * SpriteSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);

        // Pixel art import ayarlari — ASSETS.md'de anlatilan ayarlarin aynisi.
        // Indireceginiz gercek asset'lere de bu ayarlari uygulayacaksiniz.
        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;          // bulaniklik olmasin
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    // ------------------------------------------------------------------
    //  Objeler
    // ------------------------------------------------------------------

    static void CreateGround(Sprite square)
    {
        GameObject ground = NewSprite("Ground", square, new Color(0.18f, 0.15f, 0.14f));
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.transform.position = new Vector3(0f, -4f, 0f);
        ground.transform.localScale = new Vector3(30f, 1f, 1f);
        ground.AddComponent<BoxCollider2D>();

        // Ziplayip inebileceginiz birkac platform
        CreatePlatform(square, new Vector3(-6f, -1.5f, 0f), new Vector3(4f, 0.5f, 1f));
        CreatePlatform(square, new Vector3(0f, 0.8f, 0f), new Vector3(4f, 0.5f, 1f));
        CreatePlatform(square, new Vector3(6f, -1.5f, 0f), new Vector3(4f, 0.5f, 1f));
    }

    static void CreatePlatform(Sprite square, Vector3 pos, Vector3 scale)
    {
        GameObject p = NewSprite("Platform", square, new Color(0.24f, 0.20f, 0.18f));
        p.layer = LayerMask.NameToLayer("Ground");
        p.transform.position = pos;
        p.transform.localScale = scale;
        p.AddComponent<BoxCollider2D>();
    }

    static GameObject CreatePlayer(Sprite square)
    {
        GameObject player = NewSprite("Player", square, new Color(0.90f, 0.35f, 0.15f));
        player.layer = LayerMask.NameToLayer("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, -2f, 0f);
        player.transform.localScale = new Vector3(0.8f, 1.6f, 1f);

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;   // titremeyi onler

        // ⚠️ Hareketsiz kalan Rigidbody2D "uyur" ve uyuyan cisim icin
        // OnTriggerStay2D cagrilmaz — musluk onunde DURUNCA su dolmamasinin
        // sebebi buydu. Oyuncu asla uyumasin.
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        player.AddComponent<BoxCollider2D>();

        // Zemin kontrol noktasi. Local -0.52, parent scale 1.6 →
        // dunyada ayaklarin ~0.03 birim altina denk gelir.
        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform, false);
        groundCheck.localPosition = new Vector3(0f, -0.52f, 0f);

        // Su tabancasinin namlusu
        var nozzle = new GameObject("Nozzle").transform;
        nozzle.SetParent(player.transform, false);
        nozzle.localPosition = new Vector3(0.5f, 0.2f, 0f);

        var controller = player.AddComponent<PlayerController>();
        controller.groundCheck = groundCheck;
        controller.groundLayer = LayerMask.GetMask("Ground");

        player.AddComponent<WaterTank>();

        var gun = player.AddComponent<WaterGun>();
        gun.nozzle = nozzle;
        gun.hitLayers = LayerMask.GetMask("Enemy");

        player.AddComponent<PlayerHealth>();
        player.AddComponent<PetInteraction>();   // E tusu — Karabas'i sevme

        return player;
    }

    static void CreateMusluk(Sprite square)
    {
        GameObject tap = NewSprite("Musluk", square, new Color(0.25f, 0.6f, 0.95f));
        tap.tag = "WaterSource";
        tap.transform.position = new Vector3(-10f, -3f, 0f);
        tap.transform.localScale = new Vector3(1f, 1.5f, 1f);

        var col = tap.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.5f, 2f);   // yaklasinca dolsun, ustune basmaya gerek yok

        var src = tap.AddComponent<WaterSource>();
        src.type = WaterSource.SourceType.Infinite;
        src.refillPerSecond = 30f;
    }

    static void CreateAkvaryum(Sprite square)
    {
        GameObject aq = NewSprite("Akvaryum", square, new Color(0.15f, 0.85f, 0.75f));
        aq.tag = "WaterSource";
        aq.transform.position = new Vector3(10f, -3f, 0f);
        aq.transform.localScale = new Vector3(1.6f, 1f, 1f);

        var col = aq.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var src = aq.AddComponent<WaterSource>();
        src.type = WaterSource.SourceType.OneShot;
        src.instantAmount = 100f;
        src.destroyAfterUse = true;
    }

    static void CreateTestTargets(Sprite square)
    {
        // Suyla sondurulecek hedef — havada, ates edilerek olduruleck
        GameObject a = NewSprite("TestTarget_Su", square, new Color(0.85f, 0.20f, 0.20f));
        a.layer = LayerMask.NameToLayer("Enemy");
        a.tag = "Enemy";
        a.transform.position = new Vector3(4f, -2.5f, 0f);
        a.AddComponent<BoxCollider2D>();
        a.AddComponent<TestTarget>().health = 60f;

        // Ezilecek hedef — ustune ziplanacak
        GameObject b = NewSprite("TestTarget_Ezme", square, new Color(0.95f, 0.55f, 0.10f));
        b.layer = LayerMask.NameToLayer("Enemy");
        b.tag = "Enemy";
        b.transform.position = new Vector3(-3f, -3.2f, 0f);
        b.transform.localScale = new Vector3(1f, 0.5f, 1f);
        b.AddComponent<BoxCollider2D>();
        b.AddComponent<TestTarget>().health = 60f;
    }

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.07f, 0.05f, 0.06f);

        // Rig yapisi:
        //   CameraRig    ← CameraFollow (takip eder)
        //     └ Camera   ← CameraShake  (sarsar)
        // Ikisi ayni objede olursa birbirinin pozisyonunu ezer.
        Transform rig = cam.transform.parent;
        if (rig == null || rig.name != "CameraRig")
        {
            var rigGO = new GameObject("CameraRig");
            rig = rigGO.transform;
            rig.position = Vector3.zero;
            cam.transform.SetParent(rig, false);
            Undo.RegisterCreatedObjectUndo(rigGO, "ALEV sahne kurulumu");
        }

        cam.transform.localPosition = new Vector3(0f, 0f, -10f);

        if (rig.GetComponent<CameraFollow>() == null) rig.gameObject.AddComponent<CameraFollow>();
        if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
    }

    // ------------------------------------------------------------------
    //  HUD
    // ------------------------------------------------------------------

    static void CreateHUD(Sprite square, GameObject player)
    {
        var canvasGO = new GameObject("HUD");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "ALEV sahne kurulumu");

        var hud = canvasGO.AddComponent<GameHUD>();
        hud.tank = player.GetComponent<WaterTank>();
        hud.health = player.GetComponent<PlayerHealth>();

        // ---- Su bari ----
        RectTransform barBg = NewUIImage("WaterBarBg", canvasGO.transform, square,
                                         new Color(0f, 0f, 0f, 0.55f));
        Anchor(barBg, new Vector2(0f, 1f), new Vector2(40f, -110f), new Vector2(420f, 34f));

        RectTransform barFill = NewUIImage("WaterBarFill", barBg, square, Color.white);
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = Vector2.one;
        barFill.offsetMin = new Vector2(4f, 4f);
        barFill.offsetMax = new Vector2(-4f, -4f);

        var fillImage = barFill.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        hud.waterFill = fillImage;

        // ---- Kalpler ----
        var hearts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            RectTransform h = NewUIImage($"Heart{i}", canvasGO.transform, square, Color.white);
            Anchor(h, new Vector2(0f, 1f), new Vector2(46f + i * 58f, -48f), new Vector2(42f, 42f));
            hearts[i] = h.GetComponent<Image>();
        }
        hud.hearts = hearts;

        // ---- Ekran kenari alev (vinyet) ----
        Sprite vignette = GetOrCreateVignetteSprite();
        RectTransform v = NewUIImage("DamageVignette", canvasGO.transform, vignette,
                                     new Color(1f, 0.35f, 0.05f, 0f));
        v.anchorMin = Vector2.zero;
        v.anchorMax = Vector2.one;
        v.offsetMin = Vector2.zero;
        v.offsetMax = Vector2.zero;
        var vImage = v.GetComponent<Image>();
        vImage.raycastTarget = false;
        hud.damageVignette = vImage;
    }

    static RectTransform NewUIImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return go.GetComponent<RectTransform>();
    }

    /// <summary>Sol-ust koseye gore konumlandirir.</summary>
    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// <summary>
    /// Ortasi saydam, kenarlari opak bir vinyet dokusu uretir.
    /// Hasar alinca kenarlardan alev giriyormus hissi verir.
    /// </summary>
    static Sprite GetOrCreateVignetteSprite()
    {
        const string path = SpriteFolder + "/Vignette.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int size = 256;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        var center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // Merkezde tamamen saydam, kenara dogru hizla opaklasir.
                float a = Mathf.Clamp01(Mathf.Pow(Mathf.Clamp01(d), 3f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
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

    // ------------------------------------------------------------------
    //  Yardimcilar
    // ------------------------------------------------------------------

    static GameObject NewSprite(string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        // drawMode Simple kalsin: boyutlandirmayi transform.localScale ile yapiyoruz.
        Undo.RegisterCreatedObjectUndo(go, "ALEV sahne kurulumu");
        return go;
    }

    static void RemoveIfExists(params string[] names)
    {
        foreach (string n in names)
        {
            GameObject go;
            while ((go = GameObject.Find(n)) != null) Object.DestroyImmediate(go);
        }
        // Platformlar ayni isimde birden fazla oldugu icin ayri temizleniyor.
        GameObject plat;
        while ((plat = GameObject.Find("Platform")) != null) Object.DestroyImmediate(plat);
    }
}
