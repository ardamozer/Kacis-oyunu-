using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// YAPI TASI PALETI — menuden: ALEV → Yapi Taslarini Uret
///
/// Assets/Prefabs/Level/ altina elle seviye tasarimi icin hazir parcalar
/// uretir. Project penceresinden sahneye surukleyip Ctrl+D ile cogaltirsiniz.
///
/// Neden: script'le uretilen seviye blockout'tur, elle tasarlanmis gibi
/// hissettirmez. Tasarim "yerlestir → bak → kaydir → tekrar bak" dongusudur
/// ve bunu insan yapar. Palet o donguyu hizlandirir.
///
/// Bir kere calistirin, sonra Evi Kur'u BIR DAHA CALISTIRMAYIN — elle
/// yaptiginiz her seyi siler.
/// </summary>
public static class AlevPrefabPalette
{
    const string Folder = "Assets/Prefabs/Level";

    static Sprite square, glow;

    [MenuItem("ALEV/Yapi Taslarini Uret", false, 3)]
    public static void Build()
    {
        square = AlevSceneSetup.GetOrCreateSquareSprite();
        glow = LoadOrNull("Assets/Art/Generated/Glow.png");

        EnsureFolder();

        // ---- Zemin ve platformlar (uzerine basilabilir) ----
        Plat("Platform_Dar",   3f,  0.5f);
        Plat("Platform_Orta",  5f,  0.5f);
        Plat("Platform_Genis", 8f,  0.5f);
        Plat("Zemin_Parca",   10f,  1f);
        Plat("Duvar_Parca",    1f,  6f);
        Plat("Blok_Buyuk",    10f,  6f);

        // ---- Mobilya (collider YOK, sadece gorsel) ----
        Prop("Mobilya_Kucuk",  3f, 2f);
        Prop("Mobilya_Uzun",   3f, 8f);
        Prop("Mobilya_Genis", 10f, 2f);
        Prop("Tablo",          2f, 2.6f);
        Prop("Hali",          12f, 0.3f);

        // ---- Atmosfer ----
        FirePrefab("Alev_Kucuk", 1.4f, 2f, 1f);
        FirePrefab("Alev_Buyuk", 3f, 3.5f, 1.6f);

        // ---- Su ----
        WaterPrefab("Musluk", WaterSource.SourceType.Infinite, 30f,
                    new Color(0.25f, 0.60f, 0.95f), 1f, 1.8f);
        WaterPrefab("PatlakBoru_Yavas", WaterSource.SourceType.Infinite, 15f,
                    new Color(0.22f, 0.52f, 0.85f), 0.8f, 2f);
        WaterPrefab("Kuvet", WaterSource.SourceType.Infinite, 50f,
                    new Color(0.25f, 0.60f, 0.95f), 7f, 0.7f);
        WaterPrefab("Bidon", WaterSource.SourceType.OneShot, 50f,
                    new Color(0.20f, 0.70f, 0.85f), 1.2f, 1.4f);
        WaterPrefab("Akvaryum", WaterSource.SourceType.OneShot, 100f,
                    new Color(0.15f, 0.85f, 0.75f), 2.2f, 1.4f);

        // ---- Anahtarlar ve kilitler ----
        foreach (KeyColor c in new[] { KeyColor.Mavi, KeyColor.Yesil, KeyColor.Mor })
        {
            KeyPrefab(c);
            LockPrefab(c);
        }

        // ---- Dusen tahta ----
        BeamPrefab();

        // ---- Oda cercevesi ----
        RoomPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(Folder);

        Debug.Log("<b>[ALEV]</b> Yapi taslari hazir: <b>Assets/Prefabs/Level/</b>\n" +
                  "Project penceresinden sahneye surukleyin, Ctrl+D ile cogaltin.\n" +
                  "⚠️ Artik 'Evi Kur'u CALISTIRMAYIN — elle yaptiklarinizi siler.\n" +
                  "Degisiklikten sonra 'Seviyeyi Denetle' ile kontrol edin.");
    }

    // ------------------------------------------------------------------

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Level");
    }

    static Sprite LoadOrNull(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static GameObject New(string name, float w, float h, Color c, int order = 0)
    {
        var go = new GameObject(name);
        go.transform.localScale = new Vector3(w, h, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = c;
        sr.sortingOrder = order;
        return go;
    }

    static void Save(GameObject go)
    {
        string path = $"{Folder}/{go.name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------

    static void Plat(string name, float w, float h)
    {
        var go = New(name, w, h, new Color(0.31f, 0.23f, 0.17f));
        go.layer = LayerMask.NameToLayer("Ground");
        go.AddComponent<BoxCollider2D>();
        Save(go);
    }

    static void Prop(string name, float w, float h)
    {
        var go = New(name, w, h, new Color(0.22f, 0.16f, 0.14f), -2);
        Save(go);
    }

    static void FirePrefab(string name, float w, float h, float glowScale)
    {
        var go = New(name, w, h, new Color(1f, 0.55f, 0.12f, 0.95f), 5);

        SpriteRenderer gsr = null;
        if (glow != null)
        {
            var g = new GameObject("Isik");
            g.transform.SetParent(go.transform, false);
            g.transform.localScale = new Vector3(9f * glowScale / w, 9f * glowScale / h, 1f);
            gsr = g.AddComponent<SpriteRenderer>();
            gsr.sprite = glow;
            gsr.color = new Color(1f, 0.5f, 0.15f, 0.35f);
            gsr.sortingOrder = 4;
        }

        var af = go.AddComponent<AmbientFire>();
        af.glow = gsr;
        Save(go);
    }

    static void WaterPrefab(string name, WaterSource.SourceType type, float rate,
                            Color c, float w, float h)
    {
        var go = New(name, w, h, c, 2);
        go.tag = "WaterSource";

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3.5f / w, 3.5f / h);

        var src = go.AddComponent<WaterSource>();
        src.type = type;
        if (type == WaterSource.SourceType.Infinite) src.refillPerSecond = rate;
        else { src.instantAmount = rate; src.destroyAfterUse = true; }

        Save(go);
    }

    static void KeyPrefab(KeyColor c)
    {
        Color col = KeyColors.Of(c);
        var go = New($"Anahtar_{c}", 0.9f, 1.4f, col, 6);
        go.AddComponent<BoxCollider2D>().isTrigger = true;

        SpriteRenderer gsr = null;
        if (glow != null)
        {
            var g = new GameObject("Isik");
            g.transform.SetParent(go.transform, false);
            g.transform.localScale = new Vector3(7f / 0.9f, 7f / 1.4f, 1f);
            gsr = g.AddComponent<SpriteRenderer>();
            gsr.sprite = glow;
            gsr.color = new Color(col.r, col.g, col.b, 0.4f);
            gsr.sortingOrder = 5;
        }

        var kp = go.AddComponent<KeyPickup>();
        kp.color = c;
        kp.glow = gsr;
        Save(go);
    }

    static void LockPrefab(KeyColor c)
    {
        var go = New($"KilitliKapi_{c}", 1.8f, 3.4f, new Color(0.36f, 0.22f, 0.14f), 1);

        var blocker = go.AddComponent<BoxCollider2D>();
        blocker.isTrigger = false;

        var sense = go.AddComponent<BoxCollider2D>();
        sense.isTrigger = true;
        sense.size = new Vector2(2.2f, 1.2f);

        var ld = go.AddComponent<LockedDoor>();
        ld.requiredKey = c;
        ld.blocker = blocker;
        ld.openHeight = 3.8f;
        Save(go);
    }

    static void BeamPrefab()
    {
        var go = New("DusenTahta", 3f, 0.4f, new Color(0.45f, 0.28f, 0.15f));
        go.AddComponent<BoxCollider2D>().isTrigger = true;

        var gm = New("YerIsareti", 2.6f, 0.25f, new Color(1f, 0.45f, 0.1f, 0.9f), 3);
        gm.transform.SetParent(go.transform, false);
        gm.SetActive(false);

        var fd = go.AddComponent<FallingDebris>();
        fd.groundLayer = LayerMask.GetMask("Ground");
        fd.groundMarker = gm;
        fd.warningDuration = 1f;
        fd.triggerHeight = 15f;
        fd.autoTrigger = true;

        Save(go);
    }

    static void RoomPrefab()
    {
        var go = new GameObject("Oda");
        var r = go.AddComponent<Room>();
        r.roomName = "YeniOda";
        r.size = new Vector2(34f, 16f);
        Save(go);
    }
}
