using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// SU PUSKURTMESINI KUR — menuden: ALEV → Su Puskurtmesini Kur
///
/// WaterGun.sprayEffect sahnede BOSTU ve sahnede hic ParticleSystem yoktu:
/// oyuncu su sikiyor ama hicbir sey gormuyordu. Kod tarafi zaten hazirdi
/// (Play/Stop + nisan yonune cevirme), sadece parcacik uretilmemisti.
///
/// ⚠️ Bu "cila" degil, OKUNABILIRLIK. Gorunmeyen suyla oyuncu nereye
/// nisan aldigini, isabet edip etmedigini bilemiyor.
///
/// ⚠️ KONI +X'E BAKMALI. WaterGun her karede
/// `sprayEffect.transform.right = dir` yapiyor, yani nisan yonu LOCAL +X.
/// ParticleSystem'in koni sekli varsayilan olarak +Z'ye ates eder;
/// shape.rotation ile 90 derece cevriliyor. Bu satir olmadan su
/// ekrana dik firlar ve hicbir sey gorunmez.
///
/// ⚠️ SIMULATION SPACE = WORLD. Local kalirsa parcacik bulutu oyuncuyla
/// birlikte tasinir; su havada asili durur ve firlatilmis gibi durmaz.
/// </summary>
public static class AlevWaterSpraySetup
{
    const string MaterialPath = "Assets/Art/Generated/SuParcacik.mat";
    const string CirclePath   = "Assets/Art/Generated/WhiteCircle.png";

    [MenuItem("ALEV/Su Puskurtmesini Kur", false, 7)]
    public static void Setup()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("ALEV", "Sahnede Tag='Player' yok.", "Tamam");
            return;
        }

        var gun = player.GetComponent<WaterGun>();
        if (gun == null)
        {
            EditorUtility.DisplayDialog("ALEV", "Player'da WaterGun yok.", "Tamam");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(player, "Su puskurtmesi");

        // Namludan cikmali; yoksa oyuncunun merkezinden.
        Transform origin = player.transform.Find("Nozzle") ?? player.transform;

        // ---- Namluyu ELIN UCUNA tasi ----
        // Eski yer local (0.5, 0.2) → dunyada (0.4, 0.32), o da tam cene
        // hizasi: su agizdan cikiyor gibi duruyordu.
        // Yeni yer sprite'tan OLCULDU: sprite_34'te en sagdaki opak piksel
        // kare icinde x=28, alttan 13. satir. PPU 16, pivot alt-orta,
        // ayaklar dunyada -0.8 →  dunya (0.75, 0.01)
        // Parent olcegi (0.8, 1.6) oldugu icin local (0.94, 0.01).
        if (origin != player.transform)
            origin.localPosition = new Vector3(0.94f, 0.01f, 0f);

        // Namlu artik govdenin DISINDA. Aynalanmazsa sola bakarken su
        // sirttan cikar ve hasar kutusu yanlis tarafta olusur.
        var flip = player.GetComponent<NozzleFlip>();
        if (flip == null) flip = player.AddComponent<NozzleFlip>();
        flip.nozzle = origin != player.transform ? origin : null;

        Transform t = origin.Find("SuPuskurtme");
        if (t == null)
        {
            var go = new GameObject("SuPuskurtme");
            go.transform.SetParent(origin, false);
            t = go.transform;
        }
        t.localPosition = Vector3.zero;

        var ps = t.GetComponent<ParticleSystem>();
        if (ps == null) ps = t.gameObject.AddComponent<ParticleSystem>();

        // Ayarlarken calisiyor olmasin.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ---- Ana ----
        var main = ps.main;
        // Menzil 3.5 birim: hiz 10 × omur 0.35 = 3.5. WaterGun.range ile
        // ayni olmali, yoksa su goründügünden uzaga/yakina hasar verir.
        main.startLifetime   = 0.35f;
        main.startSpeed      = 10f;
        main.startSize       = 0.14f;
        main.startColor      = new Color(0.55f, 0.8f, 1f, 0.85f);
        main.gravityModifier = 0.35f;          // hafif yay cizsin, lazer gibi durmasin
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 250;
        main.playOnAwake     = false;          // WaterGun aciyor

        // ---- Cikis ----
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 90f;

        // ---- Sekil ----
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle  = 9f;                        // WaterGun.width 1.2 ile uyumlu dagilim
        sh.radius = 0.05f;
        sh.rotation = new Vector3(0f, 90f, 0f); // +Z degil +X'e ates etsin

        // ---- Sonuca dogru incelip solsun ----
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.35f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0f),
                    new GradientColorKey(new Color(0.4f, 0.7f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0f,   1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // ---- Cizim ----
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.material = GetOrCreateMaterial();
        r.sortingOrder = 10;                   // oyuncunun ve atesin onunde

        gun.sprayEffect = ps;

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = t.gameObject;

        Debug.Log("<b>[ALEV Su]</b> Puskurtme kuruldu ve WaterGun.sprayEffect'e baglandi.\n" +
                  $"  menzil {main.startSpeed.constant * main.startLifetime.constant:0.##} birim " +
                  $"(WaterGun.range {gun.range})  ·  koni {sh.angle}°  ·  {em.rateOverTime.constant}/sn");
    }

    /// <summary>
    /// Sprites/Default + yuvarlak doku. Materyal verilmezse parcacik
    /// macenta cikar; Sprites/Default 2D'de dogru harmanlamayi veriyor.
    /// </summary>
    static Material GetOrCreateMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");

        mat = new Material(sh);

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CirclePath);
        if (tex != null) mat.mainTexture = tex;

        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
