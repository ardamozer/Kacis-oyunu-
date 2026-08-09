using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ITFAIYECIYI BAGLA — menuden: ALEV → Itfaiyeciyi Bagla
///
/// Sahnedeki Player'a gercek sprite'lari ve PlayerAnimator'u takar.
///
/// ⚠️ FIZIGE HIC DOKUNMUYOR — tasarim karari.
/// Player'in localScale'i (0.8, 1.6) ve BoxCollider2D AYNI objede, yani
/// carpisma kutusunun boyutu olcege gomulu. Olcegi 1'e cekmek daha temiz
/// olurdu ama uc seyi birden yeniden hesaplamayi gerektirirdi:
///   - collider.size
///   - GroundCheck local (0, -0.52) × 1.6 = ayaklarin 0.03 alti
///   - Nozzle local (0.5, 0.2) → dunyada (0.4, 0.32)
/// Ustelik transform.position'in anlami (merkez) degisirdi; checkpoint'ler
/// ve seviye yerlesimi konumlari ona gore tutuyor.
///
/// Onun yerine GORSEL ayri bir cocuk objeye ("Gorsel") aliniyor ve orada
/// ters olcekle (1/0.8, 1/1.6) parent'in ezmesi notrleniyor. Sonuc: sprite
/// dogru oranda, fizik bir bit degismemis.
///
/// Kokteki eski SpriteRenderer SILINIYOR (kapatilmiyor):
/// GetComponentInChildren kapali BILESENI de donduruyor, dursaydi
/// PlayerController ve PlayerHealth turuncu dikdortgeni bulmaya devam ederdi.
/// </summary>
public static class AlevFiremanBinder
{
    const string Folder = "Assets/Art/Imported/Fireman";
    const string VisualName = "Gorsel";

    // Sprite secimleri kasilma sirasinda gozle secildi (32x32, hepsi saga bakiyor).
    const int IdleFrame  = 31;              // yandan, dik durus, kollar yanda
    static readonly int[] WalkFrames  = { 7, 8 };    // bacaklar net donusumlu
    const int JumpFrame  = 16;              // kol yukari uzanmis
    static readonly int[] SprayFrames = { 34, 42 };  // kol ileri — hortum tutusu

    [MenuItem("ALEV/Itfaiyeciyi Bagla", false, 6)]
    public static void Bind()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("ALEV",
                "Sahnede Tag='Player' olan obje yok.\n\nMainScene acik mi?", "Tamam");
            return;
        }

        Sprite idle = Load(IdleFrame);
        if (idle == null)
        {
            EditorUtility.DisplayDialog("ALEV",
                $"{Folder}/sprite_{IdleFrame}.png bulunamadi.\n\n" +
                "Itfaiyeci paketi o klasorde mi?", "Tamam");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(player, "Itfaiyeciyi bagla");

        // ---- 1. Koktekı dikdortgen renderer'i kaldir ----
        var rootSR = player.GetComponent<SpriteRenderer>();
        int sortingOrder = rootSR != null ? rootSR.sortingOrder : 5;
        if (rootSR != null) Object.DestroyImmediate(rootSR, true);

        // ---- 2. Gorsel cocugu ----
        Transform vt = player.transform.Find(VisualName);
        if (vt == null)
        {
            var go = new GameObject(VisualName);
            go.transform.SetParent(player.transform, false);
            vt = go.transform;
        }

        Vector3 ps = player.transform.localScale;
        // Parent'in ezmesini notrle → dunyada olcek 1
        vt.localScale = new Vector3(
            Mathf.Approximately(ps.x, 0f) ? 1f : 1f / ps.x,
            Mathf.Approximately(ps.y, 0f) ? 1f : 1f / ps.y,
            1f);

        // Sprite pivotu AYAKLARDA (alt bosluk 0 olculdu). Collider'in alt
        // kenari kokten 0.8 birim asagida (olcek 1.6 × varsayilan 1 / 2),
        // yani local -0.5 → dunyada -0.8. Ayaklar tam collider tabanina oturur.
        vt.localPosition = new Vector3(0f, -0.5f, 0f);

        var sr = vt.GetComponent<SpriteRenderer>();
        if (sr == null) sr = vt.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = idle;
        sr.sortingOrder = sortingOrder;
        sr.color = Color.white;   // eski turuncu tint sprite'i boyamasin

        // ---- 3. Animasyon ----
        var anim = player.GetComponent<PlayerAnimator>();
        if (anim == null) anim = player.AddComponent<PlayerAnimator>();

        anim.idle  = Clip(new[] { IdleFrame }, 0f, false);
        anim.run   = Clip(WalkFrames,  8f,  true);   // PlayerAnimator'da alan adi 'run'
        anim.jump  = Clip(new[] { JumpFrame }, 0f, false);
        anim.spray = Clip(SprayFrames, 6f,  true);

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = player;

        Debug.Log("<b>[ALEV Itfaiyeci]</b> Baglandi.\n" +
                  $"  idle {IdleFrame} · walk {string.Join(",", WalkFrames)} · " +
                  $"jump {JumpFrame} · spray {string.Join(",", SprayFrames)}\n" +
                  $"  Gorsel cocugu olcek ({vt.localScale.x:0.##}, {vt.localScale.y:0.##}) " +
                  "— parent'in (0.8, 1.6) ezmesi notrlendi, fizik degismedi.");
    }

    static PlayerAnimator.Clip Clip(int[] numbers, float fps, bool loop)
    {
        var frames = new System.Collections.Generic.List<Sprite>();
        foreach (int n in numbers)
        {
            Sprite s = Load(n);
            if (s != null) frames.Add(s);
        }
        return new PlayerAnimator.Clip { frames = frames.ToArray(), fps = fps, loop = loop };
    }

    static Sprite Load(int n) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/sprite_{n}.png");
}
