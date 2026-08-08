using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// KISI B'NIN PREFABLARINI TEK TIKLA KURAR.
///
/// Menu:
///   ALEV → Kisi B — Prefablari Kur      (Flame, WallMonster, FloorFire, Karabas)
///   ALEV → Kisi B — Finali Sahneye Kur  (EndingSequence + Boss + hayalet + tasma)
///
/// NEDEN VAR: KURULUM_B.md Bolum 2-8'de ~60 adet Inspector alani elle
/// baglaniyor. En sik jam hatasi "Is Trigger'i unutmak" / "prefab yerine
/// Hierarchy objesini surukLEMEK" gibi seyler. Burada hepsi kod, unutulamaz.
///
/// Tekrar tekrar calistirilabilir — bir sey bozulursa yeniden basin.
/// Prefablar ustune yazilir, sahnedeki objeler silinip yeniden kurulur.
///
/// Bu bir EDITOR script'i (Assets/Editor altinda), oyun build'ine girmez.
/// Kisi A'nin AlevSceneSetup.cs dosyasina DOKUNULMADI, bu ayri bir dosya.
/// </summary>
public static class AlevPrefabSetupB
{
    const string ArtFolder = "Assets/Art/Generated";
    const string SquarePath = ArtFolder + "/WhiteSquare.png";
    const string CirclePath = ArtFolder + "/WhiteCircle.png";

    const string PrefabRoot = "Assets/Prefabs";
    const string EnemyPrefabFolder = PrefabRoot + "/Enemies";
    const string DogPrefabFolder = PrefabRoot + "/Dog";

    // Kisi A'nin sprite'i ile ayni olcek: 16px / 16 PPU = 1 birim.
    const int SpriteSize = 16;
    const int PixelsPerUnit = 16;

    // ==================================================================
    //  1) PREFABLAR
    // ==================================================================

    [MenuItem("ALEV/Kisi B — Prefablari Kur", false, 20)]
    public static void BuildPrefabs()
    {
        if (!EditorUtility.DisplayDialog(
                "Kisi B prefablarini kur",
                "Su prefablar olusturulacak (varsa uzerine yazilir):\n\n" +
                "  Prefabs/Enemies/Flame\n" +
                "  Prefabs/Enemies/WallMonster\n" +
                "  Prefabs/Enemies/FloorFire\n" +
                "  Prefabs/Dog/Karabas\n\n" +
                "Sahnendeki objelere dokunulmaz.",
                "Kur", "Vazgec"))
        {
            return;
        }

        if (!CheckLayersAndTags()) return;

        EnsureFolder(PrefabRoot);
        EnsureFolder(EnemyPrefabFolder);
        EnsureFolder(DogPrefabFolder);

        Sprite square = GetOrCreateSprite(SquarePath, false);
        Sprite circle = GetOrCreateSprite(CirclePath, true);

        GameObject flamePrefab = BuildFlame(circle);
        BuildWallMonster(square, flamePrefab);
        BuildFloorFire(square);
        BuildKarabas(square);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<b>[ALEV-B]</b> Prefablar kuruldu: Flame, WallMonster, FloorFire, Karabas.\n" +
                  "Sirada: menuden <b>ALEV → Kisi B — Finali Sahneye Kur</b>, " +
                  "ya da prefablari Hierarchy'ye surukleyip test et.");
    }

    // ---- Flame ----

    static GameObject BuildFlame(Sprite circle)
    {
        GameObject go = new GameObject("Flame");
        go.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = new Color(1f, 0.53f, 0f);          // FF8800
        sr.sortingOrder = 2;

        // Is Trigger: alev oyuncuyu itmemeli, icinden gecip hasar vermeli.
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        // Kinematic Rigidbody: iki collider'dan biri Rigidbody tasimazsa
        // Unity trigger olayini tetiklemez. Zemin statik oldugu icin buraya koyuyoruz.
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        FlameProjectile fp = go.AddComponent<FlameProjectile>();
        fp.blockingLayers = LayerMask.GetMask("Ground");

        GameObject prefab = SavePrefab(go, EnemyPrefabFolder + "/Flame.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---- WallMonster ----

    static void BuildWallMonster(Sprite square, GameObject flamePrefab)
    {
        GameObject go = new GameObject("WallMonster");
        go.tag = "Enemy";
        go.layer = LayerMask.NameToLayer("Enemy");
        go.transform.localScale = new Vector3(1f, 1.4f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.85f, 0.25f, 0.1f);
        sr.sortingOrder = -1;                          // duvarin ARKASINDA gizlensin

        // Trigger: oyuncu icinden gecebilmeli — PLAN.md "kacarak gecilebilir".
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // Ates noktasi govdenin DISINDA olmali, yoksa alev kendi collider'ina takilir.
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(go.transform, false);
        firePoint.transform.localPosition = new Vector3(-0.8f, 0.2f, 0f);

        WallMonster wm = go.AddComponent<WallMonster>();
        wm.facing = -1;                                // sola bakar (sag duvardaki canavar)
        wm.detectRange = 7f;
        wm.emergeDistance = 0.9f;
        wm.emergeSpeed = 5f;
        wm.fireInterval = 2f;
        wm.maxHealth = 40f;
        wm.flamePrefab = flamePrefab;                  // ASSET prefabi, sahne objesi degil
        wm.firePoint = firePoint.transform;

        SavePrefab(go, EnemyPrefabFolder + "/WallMonster.prefab");
        Object.DestroyImmediate(go);
    }

    // ---- FloorFire ----

    static void BuildFloorFire(Sprite square)
    {
        GameObject go = new GameObject("FloorFire");
        go.tag = "Enemy";
        go.layer = LayerMask.NameToLayer("Enemy");
        go.transform.localScale = new Vector3(0.9f, 0.5f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(1f, 0.75f, 0.3f);
        sr.sortingOrder = 1;

        // ⚠️ Is Trigger YOK: ezme (stomp) gercek carpisma gerektirir.
        go.AddComponent<BoxCollider2D>();

        // Bar soldan saga dolsun diye capa + ofsetli cocuk.
        // Sprite dogrudan olceklenirse ORTADAN buyur, kotu gorunur.
        GameObject fillBar = new GameObject("FillBar");
        fillBar.transform.SetParent(go.transform, false);
        fillBar.transform.localPosition = new Vector3(-0.5f, 0.75f, 0f);

        GameObject barFill = new GameObject("BarFill");
        barFill.transform.SetParent(fillBar.transform, false);
        barFill.transform.localPosition = new Vector3(0.5f, 0f, 0f);   // bu ofset sart
        barFill.transform.localScale = new Vector3(1f, 0.15f, 1f);

        SpriteRenderer barSr = barFill.AddComponent<SpriteRenderer>();
        barSr.sprite = square;
        barSr.color = Color.white;
        barSr.sortingOrder = 3;
        // BarFill'e COLLIDER EKLENMEZ — yoksa oyuncu barin ustunde durur.

        FloorFire ff = go.AddComponent<FloorFire>();
        ff.fillTime = 4f;
        ff.grownScale = 1.7f;
        ff.contactDamage = 1;
        ff.damageInterval = 1f;
        ff.maxHealth = 25f;
        ff.fillBar = fillBar.transform;                // BarFill degil, FillBar capasi

        SavePrefab(go, EnemyPrefabFolder + "/FloorFire.prefab");
        Object.DestroyImmediate(go);
    }

    // ---- Karabas (+ sikismis ates) ----

    static void BuildKarabas(Sprite square)
    {
        GameObject go = new GameObject("Karabas");
        go.tag = "Dog";
        go.transform.localScale = new Vector3(0.9f, 0.7f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.35f, 0.22f, 0.15f);     // kahverengi
        sr.sortingOrder = -1;                          // atesin ARKASINDA baslasin

        AudioSource audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        DogCompanion dog = go.AddComponent<DogCompanion>();
        dog.followDistance = 1.5f;
        dog.followSpeed = 4f;
        dog.fireDetectRange = 1f;
        dog.audioSource = audio;
        // whineClip / happyBarkClip bilerek bos — ses dosyasi henuz yok,
        // kod null kontrolu yapiyor, sessiz calisir.

        // ---- Sikismis kopegi orten ates ----
        GameObject fire = new GameObject("TrappedFire");
        fire.transform.SetParent(go.transform, false);
        fire.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        fire.transform.localScale = new Vector3(0.78f, 1f, 1f);   // Karabas'in olcegini telafi eder

        // ⚠️ EN KRITIK SATIR: WaterGun sadece hitLayers(=Enemy) tarar.
        // Default layer'da kalirsa su hic degmez ve Karabas ASLA kurtarilamaz.
        fire.layer = LayerMask.NameToLayer("Enemy");

        SpriteRenderer fireSr = fire.AddComponent<SpriteRenderer>();
        fireSr.sprite = square;
        fireSr.color = new Color(1f, 0.45f, 0.1f);
        fireSr.sortingOrder = 0;

        fire.AddComponent<BoxCollider2D>();

        TrappedFireCover cover = fire.AddComponent<TrappedFireCover>();
        cover.maxHealth = 18f;
        cover.delayBeforeFollow = 1.2f;
        cover.dog = dog;                               // ayni prefab icinde, referans korunur

        // Test yardimcisi: E / R / T tuslariyla elle tetikleme.
        go.AddComponent<TestDogTrigger>().interactRange = 3f;

        SavePrefab(go, DogPrefabFolder + "/Karabas.prefab");
        Object.DestroyImmediate(go);
    }

    // ==================================================================
    //  2) FINAL SAHNESI
    // ==================================================================

    [MenuItem("ALEV/Kisi B — Finali Sahneye Kur", false, 21)]
    public static void BuildEnding()
    {
        if (!EditorUtility.DisplayDialog(
                "Finali sahneye kur",
                "Sahnende su objeler olusturulacak (ayni isimliler once silinir):\n\n" +
                "  EndingSequence · Boss · BigDeathFlame\n" +
                "  Collar · DogGhost · GhostStart/End · DogDeathPoint\n\n" +
                "Karabas sahnede varsa olum sahnesine otomatik baglanir.",
                "Kur", "Vazgec"))
        {
            return;
        }

        if (!CheckLayersAndTags()) return;

        Sprite square = GetOrCreateSprite(SquarePath, false);
        Sprite circle = GetOrCreateSprite(CirclePath, true);

        RemoveIfExists("EndingSequence", "Boss", "BigDeathFlame", "Collar",
                       "DogGhost", "GhostStart", "GhostEnd", "DogDeathPoint");

        GameObject flamePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            EnemyPrefabFolder + "/Flame.prefab");

        if (flamePrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Once prefablari kur",
                "Flame prefabi bulunamadi. Boss ates atamaz.\n\n" +
                "Once: ALEV → Kisi B — Prefablari Kur",
                "Tamam");
            return;
        }

        // ---- Olum sahnesi gorselleri ----
        GameObject deathPoint = new GameObject("DogDeathPoint");
        deathPoint.transform.position = new Vector3(4f, -3f, 0f);

        GameObject bigFlame = MakeSprite("BigDeathFlame", circle,
            new Color(1f, 0.35f, 0.05f), new Vector3(2.5f, 2.5f, 1f),
            new Vector3(4f, -2.5f, 0f), 5);
        bigFlame.SetActive(false);

        GameObject collar = MakeSprite("Collar", square,
            new Color(0.55f, 0.35f, 0.15f), new Vector3(0.3f, 0.2f, 1f),
            new Vector3(4f, -3.3f, 0f), 4);
        collar.SetActive(false);

        // ---- Boss ----
        // PLAN.md: duvar canavarinin sprite'i 4 kat buyutulmus + koyu kirmizi.
        GameObject boss = MakeSprite("Boss", square,
            new Color(0.45f, 0.05f, 0.05f), new Vector3(4f, 5.6f, 1f),
            new Vector3(14f, -1f, 0f), 1);
        boss.layer = LayerMask.NameToLayer("Enemy");   // yoksa WaterGun bossu goremez
        boss.tag = "Enemy";
        boss.AddComponent<BoxCollider2D>().isTrigger = true;
        boss.AddComponent<BossCore>();

        GameObject bossFirePoint = new GameObject("BossFirePoint");
        bossFirePoint.transform.SetParent(boss.transform, false);
        bossFirePoint.transform.localPosition = new Vector3(-0.6f, 0f, 0f);

        boss.SetActive(false);

        // ---- Hayalet ----
        GameObject ghost = MakeSprite("DogGhost", square,
            new Color(0.6f, 0.85f, 1f, 0.5f), new Vector3(0.9f, 0.7f, 1f),
            new Vector3(20f, -3f, 0f), 6);
        ghost.SetActive(false);

        GameObject ghostStart = new GameObject("GhostStart");
        ghostStart.transform.position = new Vector3(20f, -3f, 0f);

        GameObject ghostEnd = new GameObject("GhostEnd");
        ghostEnd.transform.position = new Vector3(8f, -3f, 0f);

        // ---- Yonetici ----
        GameObject manager = new GameObject("EndingSequence");
        EndingSequence ending = manager.AddComponent<EndingSequence>();

        ending.dogDeathPoint = deathPoint.transform;
        ending.bigFlameVisual = bigFlame;
        ending.collarPickup = collar;
        ending.silenceDuration = 3f;

        ending.bossObject = boss;
        ending.bossFirePoint = bossFirePoint.transform;
        ending.flamePrefab = flamePrefab;
        ending.bossFireInterval = 2.5f;

        ending.ghostVisual = ghost;
        ending.ghostStart = ghostStart.transform;
        ending.ghostEnd = ghostEnd.transform;
        ending.ghostCrossDuration = 2.5f;

        // Sahnede Karabas varsa olum sahnesine bagla.
        DogCompanion sceneDog = Object.FindFirstObjectByType<DogCompanion>();
        if (sceneDog != null) ending.dog = sceneDog;

        manager.AddComponent<TestEndingTrigger>().ending = ending;

        Selection.activeGameObject = manager;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string dogNote = sceneDog != null
            ? "Karabas bulundu ve baglandi."
            : "UYARI: sahnede Karabas yok, olum sahnesinde kopek gizlenmeyecek. " +
              "Karabas prefabini sahneye surukleyip bu komutu tekrar calistir.";

        Debug.Log("<b>[ALEV-B]</b> Final sahnesi kuruldu. " + dogNote + "\n" +
                  "Test: <b>1</b> = kopegin olumu · <b>2</b> = boss sahnesi. " +
                  "Boss sahnesinde canin 1 kalbe dusunce hayalet otomatik gelir.");
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================

    /// <summary>
    /// Layer/tag eksikse kod sessizce yanlis calisir (layer 0 = Default).
    /// Bastan durdurup ne yapilacagini soylemek daha iyi.
    /// </summary>
    static bool CheckLayersAndTags()
    {
        string[] layers = { "Ground", "Player", "Enemy" };
        foreach (string l in layers)
        {
            if (LayerMask.NameToLayer(l) < 0)
            {
                EditorUtility.DisplayDialog("Layer eksik",
                    "'" + l + "' layer'i tanimli degil.\n\n" +
                    "Project Settings → Tags and Layers → Layers altina ekleyin, " +
                    "sonra tekrar deneyin.", "Tamam");
                return false;
            }
        }
        return true;
    }

    static GameObject MakeSprite(string name, Sprite sprite, Color color,
                                 Vector3 scale, Vector3 position, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = scale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;

        return go;
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        return PrefabUtility.SaveAsPrefabAsset(go, path);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static void RemoveIfExists(params string[] names)
    {
        foreach (string n in names)
        {
            GameObject go;
            while ((go = GameObject.Find(n)) != null) Object.DestroyImmediate(go);
        }
    }

    // ---- Sprite uretimi ----

    static Sprite GetOrCreateSprite(string path, bool circle)
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        EnsureFolder(ArtFolder);

        var tex = new Texture2D(SpriteSize, SpriteSize);
        var pixels = new Color[SpriteSize * SpriteSize];

        float r = SpriteSize * 0.5f;
        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                bool on = true;
                if (circle)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    on = dx * dx + dy * dy <= r * r;
                }
                pixels[y * SpriteSize + x] = on ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
