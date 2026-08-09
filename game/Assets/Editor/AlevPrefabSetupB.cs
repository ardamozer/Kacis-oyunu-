using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    const string HeartPath = ArtFolder + "/WhiteHeart.png";
    const string DogPath = ArtFolder + "/DogSilhouette.png";

    const string PrefabRoot = "Assets/Prefabs";
    const string EnemyPrefabFolder = PrefabRoot + "/Enemies";
    const string DogPrefabFolder = PrefabRoot + "/Dog";

    // Kisi A'nin sprite'i ile ayni olcek: 16px / 16 PPU = 1 birim.
    const int SpriteSize = 16;
    const int PixelsPerUnit = 16;

    const string AudioFolder = "Assets/Audio/Generated";

    /// <summary>
    /// Ses varsa dondurur, yoksa null. Sesler henuz uretilmemisse
    /// prefablar sessiz kurulur — kod null kontrolu yapiyor, hata olmaz.
    /// Uretmek icin: ALEV → Kisi B — Sesleri Uret
    /// </summary>
    static AudioClip Clip(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/" + name + ".wav");
    }

    // ==================================================================
    //  1) PREFABLAR
    // ==================================================================

    // Play modunda sahne kalicı degistirilemiyor (MarkSceneDirty patliyor) ve
    // zaten Play'de yapilan degisiklikler cikinca geri aliniyor. Menuyu
    // bastan kapatmak, anlasilmaz bir istisna almaktan iyi.
    [MenuItem("ALEV/Kisi B — Prefablari Kur", true)]
    static bool BuildPrefabsValidate() => !EditorApplication.isPlaying;

    [MenuItem("ALEV/Kisi B — Prefablari Kur", false, 20)]
    public static void BuildPrefabsMenu()
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

        BuildPrefabs();
    }

    /// <summary>Diyalogsuz surum — tazeleme komutu bunu cagirir.</summary>
    public static void BuildPrefabs()
    {
        if (!CheckLayersAndTags()) return;

        EnsureFolder(PrefabRoot);
        EnsureFolder(EnemyPrefabFolder);
        EnsureFolder(DogPrefabFolder);

        Sprite square = GetOrCreateSprite(SquarePath, Shape.Square);
        Sprite circle = GetOrCreateSprite(CirclePath, Shape.Circle);
        Sprite heart = GetOrCreateSprite(HeartPath, Shape.Heart);

        GameObject flamePrefab = BuildFlame(circle);
        GameObject heartPrefab = BuildHeart(heart);
        BuildWallMonster(square, flamePrefab);
        BuildFloorFire(square);
        BuildKarabas(GetOrCreateSprite(DogPath, Shape.Dog), heartPrefab);

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

        // Duz bir daire "alev" gibi okunmuyor — titretiyoruz.
        go.AddComponent<FireFlicker>();

        GameObject prefab = SavePrefab(go, EnemyPrefabFolder + "/Flame.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---- Kalp (sevme tusu geri bildirimi) ----

    static GameObject BuildHeart(Sprite heartSprite)
    {
        GameObject go = new GameObject("HeartPop");
        go.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = heartSprite;
        sr.color = new Color(1f, 0.45f, 0.6f);
        sr.sortingOrder = 10;                          // her seyin onunde gorunsun

        go.AddComponent<HeartPop>();

        GameObject prefab = SavePrefab(go, DogPrefabFolder + "/HeartPop.prefab");
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
        wm.shootClip = Clip("FlameWhoosh");
        wm.deathClip = Clip("FireExtinguish");

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
        ff.extinguishClip = Clip("FireExtinguish");

        // Titreme buyume ve sonme animasyonlarini ezmez — taban olcek
        // disaridan degisince kendini yeniden hizalar.
        go.AddComponent<FireFlicker>();

        // Surekli citirti. 3D ses: oyuncu yaklastikca yukselir, uzaklasinca
        // kesilir — sahnede 20 ates varsa hepsi ayni anda calmaz.
        AudioClip crackle = Clip("FireCrackle");
        if (crackle != null)
        {
            AudioSource loop = go.AddComponent<AudioSource>();
            loop.clip = crackle;
            loop.loop = true;
            loop.playOnAwake = true;
            loop.volume = 0.35f;
            loop.spatialBlend = 1f;                    // tamamen 3D
            loop.rolloffMode = AudioRolloffMode.Linear;
            loop.minDistance = 1.5f;
            loop.maxDistance = 9f;
        }

        SavePrefab(go, EnemyPrefabFolder + "/FloorFire.prefab");
        Object.DestroyImmediate(go);
    }

    // ---- Karabas (+ sikismis ates) ----

    static void BuildKarabas(Sprite dogSprite, GameObject heartPrefab)
    {
        GameObject go = new GameObject("Karabas");
        go.tag = "Dog";
        // Siluetin kendi orantisi var; kutu gibi ezmeyelim.
        go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = dogSprite;
        sr.color = new Color(0.35f, 0.22f, 0.15f);     // kahverengi
        sr.sortingOrder = -1;                          // atesin ARKASINDA baslasin

        AudioSource audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        AudioClip whine = Clip("DogWhine");
        AudioClip bark = Clip("DogBark");

        DogCompanion dog = go.AddComponent<DogCompanion>();
        dog.followDistance = 1.5f;
        dog.followSpeed = 8f;   // oyuncu 7 — kopek yetisebilmeli
        dog.fireDetectRange = 1f;
        dog.audioSource = audio;
        dog.whineClip = whine;
        dog.happyBarkClip = bark;

        // Ses tasarimi — PLAN.md "bastan duyulsun, yaklastikca inlemeye donsun":
        //   acilista bir havlama  -> "ileride bir kopek var"
        //   bir onceki odadan kisik inleme -> "yaklasiyorum"
        //   ayni odada net inleme -> "buradaymis"
        dog.openingBark = true;
        dog.openingBarkDelay = 1.5f;
        dog.openingBarkVolume = 0.5f;
        dog.maxHearDistance = 32f;          // oda ~20 birim, bu bir onceki odayi kapsar
        dog.minDistanceForMaxVolume = 4f;
        dog.whineInterval = 7f;             // seyrek: surekli inlerse kulak aliskanlik yapiyor
        dog.whineIntervalJitter = 2.5f;     // duzenli aralik metronom gibi duyuluyor
        // Bunlar olmadan kopek dikeyde hic hareket etmez: platforma cikan
        // oyuncunun altinda kalir, kat degisince de bir daha bulamaz.
        dog.groundLayer = LayerMask.GetMask("Ground");
        dog.warpDistance = 20f;
        // PLAN.md: sevme tusunun mekanik islevi yok ama "en cok konusulacak
        // detay" olmasi bekleniyor. Karsiliginda hicbir sey gorunmezse
        // o detay yok demektir.
        dog.heartParticlePrefab = heartPrefab;
        dog.heartCount = 3;
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
        // Ates kare kalsin; kopek silueti sadece kopege ait.
        fireSr.sprite = GetOrCreateSprite(SquarePath, Shape.Square);
        fireSr.color = new Color(1f, 0.45f, 0.1f);
        fireSr.sortingOrder = 0;

        fire.AddComponent<BoxCollider2D>();
        fire.AddComponent<FireFlicker>();

        TrappedFireCover cover = fire.AddComponent<TrappedFireCover>();
        cover.maxHealth = 18f;
        cover.delayBeforeFollow = 1.2f;
        cover.dog = dog;                               // ayni prefab icinde, referans korunur
        cover.extinguishClip = Clip("FireExtinguish");

        // Test yardimcisi: E / R / T tuslariyla elle tetikleme.
        go.AddComponent<TestDogTrigger>().interactRange = 3f;

        SavePrefab(go, DogPrefabFolder + "/Karabas.prefab");
        Object.DestroyImmediate(go);
    }

    // ==================================================================
    //  2) FINAL SAHNESI
    // ==================================================================

    [MenuItem("ALEV/Kisi B — Finali Sahneye Kur", true)]
    static bool BuildEndingValidate() => !EditorApplication.isPlaying;

    [MenuItem("ALEV/Kisi B — Finali Sahneye Kur", false, 21)]
    /// <summary>
    /// Dilimlenmis kopek sayfasindaki kareleri SAYISAL sirayla dondurur.
    /// Sayfa yoksa bos dizi — cagiran taraf eski silueti kullanir.
    ///
    /// ⚠️ Siralama sayisal olmali: alfabetikte "_10", "_2"den once gelir.
    /// </summary>
    static Sprite[] LoadDogFrames(string sheet)
    {
        string path = $"Assets/Art/Imported/Dog/{sheet}.png";
        if (!File.Exists(path)) return new Sprite[0];

        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s =>
            {
                int i = s.name.LastIndexOf('_');
                return i >= 0 && int.TryParse(s.name.Substring(i + 1), out int n)
                    ? n : int.MaxValue;
            })
            .ToArray();
    }

    public static void BuildEndingMenu()
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

        BuildEnding();
    }

    /// <summary>Diyalogsuz surum — tazeleme komutu bunu cagirir.</summary>
    public static void BuildEnding()
    {
        if (!CheckLayersAndTags()) return;

        Sprite square = GetOrCreateSprite(SquarePath, Shape.Square);
        Sprite circle = GetOrCreateSprite(CirclePath, Shape.Circle);

        RemoveIfExists("EndingSequence", "Boss", "BigDeathFlame", "Collar", "DeathDebris",
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

        // ---- Sahnenin gercek koordinatlarini bul ----
        // ⚠️ Sabit koordinat KULLANMIYORUZ. Ilk surumde y=-1/-2.5/-3 gibi
        // degerler yaziliydi; onlar duz test sahnesine gore secilmisti
        // (zemin y=-4). Gercek evin zemini y=0 oldugu icin final objeleri
        // zeminin ALTINA gomuluyor ve odalarla ust uste biniyordu.
        Vector3 anchor = FindEndingAnchor();
        Vector3 bossPos = FindBossAnchor(anchor);

        // ---- Olum sahnesi gorselleri ----
        GameObject deathPoint = new GameObject("DogDeathPoint");
        deathPoint.transform.position = anchor;

        GameObject bigFlame = MakeSprite("BigDeathFlame", circle,
            new Color(1f, 0.35f, 0.05f), new Vector3(2.5f, 2.5f, 1f),
            anchor + Vector3.up * 0.6f, 5);
        bigFlame.SetActive(false);

        GameObject collar = MakeSprite("Collar", square,
            new Color(0.55f, 0.35f, 0.15f), new Vector3(0.3f, 0.2f, 1f),
            anchor + Vector3.up * 0.1f, 4);
        collar.SetActive(false);

        // Kopegin uzerine dusecek yanan tahta (Kisi A'nin FallingDebris'i).
        // Tavandan dusecegi icin capanin epey ustunde baslar.
        GameObject debris = MakeSprite("DeathDebris", square,
            new Color(0.5f, 0.25f, 0.1f), new Vector3(2.2f, 0.35f, 1f),
            anchor + Vector3.up * 5f, 5);
        debris.AddComponent<BoxCollider2D>().isTrigger = true;

        FallingDebris fd = debris.AddComponent<FallingDebris>();
        // ⚠️ Oyuncu yaklasinca kendiliginden (ve UYARIYLA) dusmesin —
        // bu tahta sadece EndingSequence tetikleyince, uyarisiz duser.
        fd.autoTrigger = false;
        fd.groundLayer = LayerMask.GetMask("Ground");
        fd.lifetimeAfterLanding = 4f;

        // ---- Boss ----
        // PLAN.md: duvar canavarinin sprite'i 4 kat buyutulmus + koyu kirmizi.
        GameObject boss = MakeSprite("Boss", square,
            new Color(0.45f, 0.05f, 0.05f), new Vector3(4f, 5.6f, 1f),
            bossPos + Vector3.up * 2.4f, 1);   // govde merkezi, ayagi zeminde
        boss.layer = LayerMask.NameToLayer("Enemy");   // yoksa WaterGun bossu goremez
        boss.tag = "Enemy";
        boss.AddComponent<BoxCollider2D>().isTrigger = true;
        boss.AddComponent<BossCore>();

        // Dev alev de yasasin. Buyuk kutle daha agir titrer.
        FireFlicker bossFlicker = boss.AddComponent<FireFlicker>();
        bossFlicker.scaleSpeed = 4f;
        bossFlicker.scaleAmount = 0.035f;

        GameObject bossFirePoint = new GameObject("BossFirePoint");
        bossFirePoint.transform.SetParent(boss.transform, false);
        bossFirePoint.transform.localPosition = new Vector3(-0.6f, 0f, 0f);

        boss.SetActive(false);

        // ---- Hayalet ----
        // Boss'un arkasindan gelip onun uzerinden gecer. Konumlar bossa gore
        // hesaplaniyor, yoksa hayalet evin baska bir odasindan geciyor.
        Vector3 ghostFrom = bossPos + Vector3.right * 9f + Vector3.up * 0.8f;
        Vector3 ghostTo = bossPos + Vector3.left * 4f + Vector3.up * 0.8f;

        // PLAN.md: "kopek sprite'i mavi/beyaz tint + %50 saydam".
        // Hayaletin Karabas olarak TANINMASI sart — oyunun tum duygusal
        // yuku o tanimanin uzerinde. Artik gercek sprite'lar var, kodla
        // uretilen siluet yerine onlari kullaniyoruz.
        // Kosma karesi secildi: hayalet bossun uzerinden gecerek ilerliyor,
        // duran bir poz "sureklenen bir sey" gibi okunmuyor.
        Sprite[] ghostFrames = LoadDogFrames("Saint-Bernard-run");
        Sprite ghostSprite = ghostFrames.Length > 0
            ? ghostFrames[0]
            : GetOrCreateSprite(DogPath, Shape.Dog);   // sprite yoksa eski siluet

        GameObject ghost = MakeSprite("DogGhost", ghostSprite,
            new Color(0.6f, 0.85f, 1f, 0.5f), new Vector3(1.1f, 1.1f, 1f),
            ghostFrom, 6);

        // Sagdan sola gidiyor; sprite'lar varsayilan olarak saga bakiyor.
        var ghostSR = ghost.GetComponent<SpriteRenderer>();
        if (ghostSR != null && ghostFrames.Length > 0) ghostSR.flipX = true;

        if (ghostFrames.Length > 1)
        {
            var ga = ghost.AddComponent<SimpleSpriteAnimator>();
            ga.frames = ghostFrames;
            ga.fps = 10f;      // canli kopekten biraz yavas — agir, ruya gibi
            ga.loop = true;
        }

        ghost.SetActive(false);

        GameObject ghostStart = new GameObject("GhostStart");
        ghostStart.transform.position = ghostFrom;

        GameObject ghostEnd = new GameObject("GhostEnd");
        ghostEnd.transform.position = ghostTo;

        // ---- Yonetici ----
        GameObject manager = new GameObject("EndingSequence");
        EndingSequence ending = manager.AddComponent<EndingSequence>();

        ending.dogDeathPoint = deathPoint.transform;
        ending.bigFlameVisual = bigFlame;      // yedek yol (tahta yoksa)
        ending.deathDebris = fd;               // tercih edilen yol
        ending.debrisImpactDelay = 0.55f;
        ending.crashClip = Clip("DebrisCrash");
        ending.ghostClip = Clip("GhostWhoosh");
        ending.pushDistance = 2f;
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
    /// Olum sahnesinin gececegi yeri bulur.
    ///
    /// Oncelik sirasi:
    ///   1. Sahnedeki Karabas — olum onun bulundugu yerde gecer
    ///   2. Dog isaretinin konumu
    ///   3. Oyuncunun konumu
    ///   4. Son care: dunya merkezi
    ///
    /// Sabit koordinat yazmak duz test sahnesinde calisiyordu ama gercek
    /// evde her seyi zeminin altina gomuyordu.
    /// </summary>
    static Vector3 FindEndingAnchor()
    {
        DogCompanion dog = Object.FindFirstObjectByType<DogCompanion>(FindObjectsInactive.Include);
        if (dog != null) return dog.transform.position;

        foreach (SpawnMarker m in Object.FindObjectsByType<SpawnMarker>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (m.kind == SpawnMarker.Kind.Dog) return m.transform.position;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player.transform.position;

        return Vector3.zero;
    }

    /// <summary>
    /// Boss'un duracagi yer. Seviyede Boss isareti varsa oradan, yoksa
    /// olum capasinin biraz ilerisinden.
    /// </summary>
    static Vector3 FindBossAnchor(Vector3 fallbackNear)
    {
        foreach (SpawnMarker m in Object.FindObjectsByType<SpawnMarker>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (m.kind == SpawnMarker.Kind.Boss) return m.transform.position;
        }

        return fallbackNear + Vector3.right * 10f;
    }

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

    /// <summary>
    /// Verilen isimli objeleri sahneden siler.
    ///
    /// ⚠️ GameObject.Find KULLANMIYORUZ — o DEVRE DISI objeleri bulmuyor.
    /// Final objelerinin cogu (Boss, BigDeathFlame, Collar, DogGhost,
    /// DeathDebris) kurulur kurulmaz SetActive(false) oluyor. Find ile
    /// arayinca "yok" saniliyor ve her calistirmada bir yenisi ekleniyordu
    /// — F5 ile arka arkaya calistirinca sahne kopyalarla doluyordu.
    ///
    /// Bu yuzden sahne kokunden elle tarayip inaktifleri de yakaliyoruz.
    /// </summary>
    static void RemoveIfExists(params string[] names)
    {
        var wanted = new HashSet<string>(names);
        var doomed = new List<GameObject>();

        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            CollectByName(root.transform, wanted, doomed);

        foreach (GameObject go in doomed)
            if (go != null) Object.DestroyImmediate(go);
    }

    static void CollectByName(Transform t, HashSet<string> names, List<GameObject> into)
    {
        if (names.Contains(t.gameObject.name))
        {
            into.Add(t.gameObject);
            return;   // cocuklarina inme — ebeveynle birlikte gidecekler
        }

        for (int i = 0; i < t.childCount; i++)
            CollectByName(t.GetChild(i), names, into);
    }

    // ---- Sprite uretimi ----

    enum Shape { Square, Circle, Heart, Dog }

    /// <summary>
    /// Karabas'in silueti — 16x16 piksel, yandan, saga bakiyor.
    ///
    /// Neden elle cizilmis harita: kalp bir formulle uretilebiliyordu ama
    /// kopek uretilemiyor. PLAN.md kopegi oyunun duygusal merkezi ilan
    /// ediyor; kare bir kopegin olumu ile silueti belli bir kopeginki ayni
    /// sey degil. Cizilmis asset gelene kadar bu bosluğu kapatiyor.
    ///
    /// Ayaklar en alt satirda: DogCompanion zemine otururken sprite'in
    /// yariyuksekligini kullaniyor, altta bosluk birakirsak kopek havada durur.
    /// </summary>
    static readonly string[] DogPixels =
    {
        "................",
        "...#........##..",   // kuyruk ucu · kulak
        "...##......####.",
        "....#.....######",   // bas
        "....#....#######",
        "..##############",   // govde + burun
        "..#############.",
        "..#############.",
        "..############..",
        "..############..",
        "..############..",
        "..##..##.##..#..",   // bacaklar
        "..##..##.##..#..",
        "..##..##.##..#..",
        "..##..##.##..#..",
        "..##..##.##..#..",
    };

    static Sprite GetOrCreateSprite(string path, Shape shape)
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
                bool on;

                switch (shape)
                {
                    case Shape.Circle:
                    {
                        float dx = x + 0.5f - r;
                        float dy = y + 0.5f - r;
                        on = dx * dx + dy * dy <= r * r;
                        break;
                    }

                    case Shape.Heart:
                    {
                        // Klasik kalp esitsizligi: (x²+y²-1)³ - x²y³ <= 0
                        // -1..1 araligina tasiyip y'yi ceviriyoruz (doku
                        // asagidan yukari taranir, kalbin sivri ucu altta olmali).
                        float hx = (x + 0.5f - r) / r * 1.35f;
                        float hy = -(y + 0.5f - r) / r * 1.35f;

                        float a = hx * hx + hy * hy - 1f;
                        on = a * a * a - hx * hx * hy * hy * hy <= 0f;
                        break;
                    }

                    case Shape.Dog:
                    {
                        // Doku tabandan yukari taranir, harita ise yukaridan
                        // asagi yazildi — satiri ters cevirmek gerekiyor.
                        on = DogPixels[SpriteSize - 1 - y][x] == '#';
                        break;
                    }

                    default:
                        on = true;
                        break;
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
