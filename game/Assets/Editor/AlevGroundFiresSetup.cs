using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// YERDEKI DEKOR ATESLERINI SONDURULEBILIR YAP —
/// menuden: ALEV → Yerdeki Atesleri Sondurulebilir Yap
///
/// Atesler tek sanata birlestirilince yeni bir sorun cikti: dekoratif ates
/// ile gercek FloorFire ARTIK AYNI GORUNUYOR. Oyuncu birine su sikiyor
/// sonuyor, otekine sikiyor hicbir sey olmuyor. Ayni goruntunun iki farkli
/// kurala uymasi en kotu turden kafa karisikligi.
///
/// Bu komut YERDEKI dekor ateslerine FloorFire takar.
///
/// ⚠️ KORIDOR'DAKI IKI ATES BILEREK DISARIDA.
/// PLAN kurali: "Koridor kasitli susuz ama orada yer atesi YOK."
/// Susuz bir bolgeye suyla sonen, temas hasari veren ates koyarsak
/// oyuncu kilitlenebilir. Koridor_Alev1 (x=156) ve Koridor_Alev2
/// (x=182.5) dekor kalir.
///
/// ⚠️ YUKSEKTEKILER DE DISARIDA.
/// Duvar aplikleri ve avize atesi (y >= 6.5) zaten ulasilamaz; onlari
/// sondurulebilir yapmak "sondurmeye calisip ulasamama" hayal kirikligi
/// uretirdi.
///
/// ⚠️ AYARLAR OZELLIKLE MUHAFAZAKAR:
///   startGrown = true   → gorunum degismiyor, zaten buyuk yaniyorlar
///   grownScale = 1      → daha fazla BUYUMUYORLAR; seviye bu boyutlara
///                         gore tasarlandi, buyuselerdi gecisleri kapatirdi
///   trigger collider    → fiziksel carpisma yok, oyuncu atese takilmaz
///                         (ezme de calismaz ama buyumus atesler zaten
///                          suyla sonuyor, kural tutarli)
/// </summary>
public static class AlevGroundFiresSetup
{
    /// <summary>Yerdeki dekor atesleri. Koridor'dakiler bilerek yok.</summary>
    static readonly string[] Targets =
    {
        "Antre_Alev1",     // x=30
        "Salon_Alev1",     // x=78
        "Mutfak_Alev1",    // x=148
        "Cocuk_Alev1",     // x=223
        "Arka_Alev1",      // x=232   (su x=240'ta)
        "Arka_Alev3",      // x=284
        "ArkaKapi_Alev1",  // x=298   (su x=306'da)
    };

    [MenuItem("ALEV/Yerdeki Atesleri Sondurulebilir Yap", false, 9)]
    public static void Setup()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0)
        {
            EditorUtility.DisplayDialog("ALEV",
                "'Enemy' layer'i yok. WaterGun sadece o layer'i tariyor.", "Tamam");
            return;
        }

        // Ayarlari prefabtan al ki mevcut ateslerle birebir ayni davransin.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Enemies/FloorFire.prefab");
        FloorFire model = prefab != null ? prefab.GetComponent<FloorFire>() : null;

        int done = 0, missing = 0;
        var log = new System.Text.StringBuilder();

        foreach (string name in Targets)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                missing++;
                log.Append($"\n  ✖ {name} — sahnede yok");
                continue;
            }

            Undo.RegisterFullObjectHierarchyUndo(go, "Yerdeki atesler");

            go.layer = enemyLayer;
            go.tag = "Enemy";

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();

            // ⚠️ TRIGGER OLMAMALI. FloorFire temas hasarini
            // OnCollisionStay2D ile veriyor; trigger'da carpisma olayi hic
            // dogmaz, ates sondurulebilir ama uzerinden gecerken hasar
            // vermez. Ilk surumde trigger'di ve tam bu yuzden hasarsizdi.
            col.isTrigger = false;

            // Gorsel karenin tamami degil, ALT KISMI kati olsun.
            // Alev sprite'i uzun; tamami kati olsaydi 3.7 birimlik bir
            // duvar olurdu ve oyuncu ziplayarak (max 2.87) asamazdi.
            // Alt yarisi hem "uzerine basinca yanarsin" hissini verir hem
            // de gecisi tikamaz.
            col.size   = new Vector2(0.8f, 0.45f);
            col.offset = new Vector2(0f, -0.25f);

            var ff = go.GetComponent<FloorFire>();
            if (ff == null) ff = go.AddComponent<FloorFire>();

            if (model != null)
            {
                ff.maxHealth          = model.maxHealth;
                ff.contactDamage      = model.contactDamage;
                ff.damageInterval     = model.damageInterval;
                ff.extinguishDuration = model.extinguishDuration;
                ff.extinguishVolume   = model.extinguishVolume;
            }

            ff.startGrown = true;   // zaten buyuk yaniyorlar
            ff.grownScale = 1f;     // daha fazla buyumesinler

            // ⚠️ RENK AYRIMI — okunabilirlik kurali.
            // Duvar canavarlari da ates sanatini kullaniyor artik. Ikisi
            // ayni kirmizida olursa oyuncu yoldaki sondurulebilir atesi
            // dusman sanip kaciyor, ya da tersi. Yoldaki atesler SICAK
            // SARI-TURUNCU kaliyor; dusmanlar kendi kirmizisinda.
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.72f, 0.30f, 1f);

            EditorUtility.SetDirty(go);
            done++;
            log.Append($"\n  ✔ {name} (x={go.transform.position.x:0.#})");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<b>[ALEV Yer Atesleri]</b> {done} ates sondurulebilir oldu" +
                  (missing > 0 ? $", {missing} bulunamadi" : "") + "." +
                  "\n  Koridor'daki iki ates BILEREK disarida — orasi kasitli susuz." +
                  log);
    }
}
