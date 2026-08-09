using System.Collections;
using UnityEngine;

/// <summary>
/// FINAL BOSS'UN GOVDESI — devasa alev.
///
/// TASARIM: boss OLDURULEMEZ, ama oyuncu bunu denemeden anlamamali.
///
/// Eski hali "su siksan da hicbir sey olmaz" idi; oyuncu bir sey yaptigini
/// hissetmiyordu. Simdi su GERCEKTEN isliyor: alev gozle gorulur sekilde
/// kuculuyor, oyuncu "oldurebilirim" diye inaniyor — ve tam bitecekken alev
/// kendini yeniden buyutuyor. Bir kez umut verip geri almak, hic umut
/// vermemekten cok daha agir bir caresizlik birakiyor.
///
/// Ve caresizligin dibindeyken Karabas'in hayaleti geliyor. Sahnenin anlami
/// bu sirayla kuruluyor: denedim -> yetmedi -> o kurtardi.
///
/// PLAN.md "Boss = dovus DEGIL, sahne" karari korunuyor: oyuncunun kazanma
/// yolu YOK. Degisen tek sey, yenilginin gorunur olmasi.
///
/// Sprite: WallMonster'in sprite'i 4 kat buyutulmus + koyu kirmizi renk
/// (PLAN.md — yeni asset gerekmiyor).
/// </summary>
public class BossCore : MonoBehaviour, IWaterDamageable
{
    [Header("Can (oldurulemez — sifira inmeden yenilenir)")]
    [Tooltip("Su tabancasi saniyede 40 hasar veriyor. 160 = ~4 saniye kesintisiz " +
             "sikma, yani neredeyse tam bir depo. 'Her seyimi verdim' hissi icin bu.")]
    public float maxHealth = 160f;

    [Tooltip("Can bu orana DUSUNCE alev kendini yeniler. 0.15 = %15.\n" +
             "Cok dusuk yaparsaniz oyuncu 'olmustu ama' der ve haksizlik hisseder; " +
             "cok yuksek yaparsaniz umut hic dogmaz.")]
    [Range(0.05f, 0.4f)] public float regenThreshold = 0.15f;

    [Header("Yenilenme")]
    [Tooltip("Alev en kucukken bu kadar bekler — 'basardim mi?' anini yasatan bosluk.")]
    public float regenPauseDuration = 0.4f;
    [Tooltip("Yeniden buyume suresi. Cok hizli olursa fark edilmez.")]
    public float regenGrowDuration = 0.9f;
    public Color regenFlashColor = new Color(1f, 0.85f, 0.3f);
    public float regenShakeDuration = 0.5f;
    public float regenShakeMagnitude = 0.4f;

    [Header("Gorsel")]
    [Tooltip("Cani tukenmisken olcegi. Kuculme okunakli olmali, yoksa oyuncu " +
             "hasar verdigini anlamaz — can bari yok, tek geri bildirim bu.")]
    [Range(0.2f, 0.9f)] public float minScale = 0.5f;

    [Tooltip("Su degince yanip sonecek renk.")]
    public Color hitFlashColor = Color.white;
    public float flashDuration = 0.08f;

    /// <summary>0-1. Disaridan okunabilsin diye.</summary>
    public float HealthFraction => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

    /// <summary>Kac kez yenilendi? Playtest'te dengeyi ayarlamak icin.</summary>
    public int RegenCount { get; private set; }

    SpriteRenderer sprite;
    Color originalColor;
    float flashUntil;

    float currentHealth;
    Vector3 baseScale;
    bool regenerating;
    bool defeated;
    Coroutine regenRoutine;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;
        baseScale = transform.localScale;
    }

    /// <summary>
    /// Boss sahnesi her acildiginda sifirlanir — test tuslariyla defalarca
    /// denenebilsin diye. (Obje TriggerBossScene'de SetActive(true) oluyor.)
    /// </summary>
    void OnEnable()
    {
        currentHealth = maxHealth;
        regenerating = false;
        defeated = false;
        RegenCount = 0;
        transform.localScale = baseScale;
        if (sprite != null) sprite.color = originalColor;
    }

    void Update()
    {
        if (sprite != null && flashUntil > 0f && Time.time >= flashUntil)
        {
            sprite.color = originalColor;
            flashUntil = 0f;
        }
    }

    /// <summary>
    /// Su degdi. Can azalir ve alev kuculur — AMA sifira inmez:
    /// esige varinca kendini yeniler. Oldurme yolu yok, bu kasitli.
    /// </summary>
    public void TakeWater(float amount)
    {
        if (defeated || regenerating) return;

        currentHealth = Mathf.Max(currentHealth - amount, maxHealth * regenThreshold);

        if (sprite != null)
        {
            sprite.color = Color.Lerp(originalColor, hitFlashColor, 0.6f);
            flashUntil = Time.time + flashDuration;
        }

        ApplyScale();

        if (currentHealth <= maxHealth * regenThreshold + 0.01f)
            regenRoutine = StartCoroutine(RegenerateRoutine());
    }

    /// <summary>Cana gore olcek. Tek gorsel geri bildirim bu — can bari yok.</summary>
    void ApplyScale()
    {
        float k = Mathf.InverseLerp(maxHealth * regenThreshold, maxHealth, currentHealth);
        transform.localScale = baseScale * Mathf.Lerp(minScale, 1f, k);
    }

    /// <summary>
    /// "Neredeyse olduruyordun" ani. Once bir bosluk (oyuncu nefesini tutar),
    /// sonra alev kukreyerek geri doner. Umut tam burada aliniyor.
    /// </summary>
    IEnumerator RegenerateRoutine()
    {
        regenerating = true;
        RegenCount++;

        Debug.Log($"[Final] Boss yenilendi ({RegenCount}. kez) — oyuncu esige indirdi " +
                  "ama olduremedi.");

        yield return new WaitForSeconds(regenPauseDuration);

        if (defeated) { regenerating = false; yield break; }

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(regenShakeDuration, regenShakeMagnitude);

        if (sprite != null)
        {
            sprite.color = regenFlashColor;
            flashUntil = Time.time + regenGrowDuration;
        }

        Vector3 from = transform.localScale;
        float e = 0f;
        while (e < regenGrowDuration)
        {
            if (defeated) { regenerating = false; yield break; }

            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / regenGrowDuration);
            k = k * k * (3f - 2f * k);                 // SmoothStep — kukreme hissi
            transform.localScale = Vector3.Lerp(from, baseScale, k);
            yield return null;
        }

        transform.localScale = baseScale;
        currentHealth = maxHealth;
        regenerating = false;
    }

    /// <summary>
    /// Hayalet geldi — boss artik yenilenmiyor ve olcegi birakiyor.
    /// EndingSequence sonmeyi (FadeOutBoss) kendi oynatiyor; burasi sadece
    /// cekismeyi kesiyor, yoksa iki kod ayni localScale'i oynatir.
    /// </summary>
    public void Defeat()
    {
        defeated = true;

        if (regenRoutine != null)
        {
            StopCoroutine(regenRoutine);
            regenRoutine = null;
        }

        regenerating = false;
        transform.localScale = baseScale;
        if (sprite != null) sprite.color = originalColor;
    }
}
