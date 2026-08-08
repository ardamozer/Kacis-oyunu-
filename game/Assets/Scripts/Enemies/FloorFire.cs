using System.Collections;
using UnityEngine;

/// <summary>
/// YER ATESI — yerde duran, uzerinde dolan barli ates.
///
/// Iki faz:
///   FAZ 1 (KUCUK) — bar doluyor. Zararsiz. Ustune ziplayip ezersen soner.
///   FAZ 2 (BUYUK) — bar doldu. Buyur, kizarir, DOKUNUNCA HASAR VERIR.
///                   Varsayilan olarak ARTIK EZILEMEZ, sadece su sondurur.
///
/// ⚠️ SEVIYE TASARIMI SARTI — bunu okumadan seviye kurmayin.
/// PLAN.md kural 3 "yer atesleri susuz oldurulebilir kalmali" diyordu; bu
/// artik SADECE kucuk fazda gecerli. Buyumus ates su gerektiriyor, yani
/// kilitlenme riski seviye tasarimina kaydi:
///
///   • Her odada oyuncunun ULASABILECEGI sonsuz su kaynagi (musluk/kuvet) olsun.
///   • Buyumus bir ates TEK gecis yolunu kapatmasin — ya alternatif yol,
///     ya da atesin arkasinda degil onunde bir musluk olsun.
///
/// Playtest'te oyuncu susuz + onunde buyumus ates ile sikisirsa,
/// `grownCanBeStomped` alanini acin — tek tikta eski davranisa doner.
///
/// FireMonster'a donusme ozelligi KESILDI (PLAN.md kesilecekler #3).
/// Dolunca canavar dogmuyor, sadece buyuyup hasar veriyor.
///
/// Karabas bu atesin ustune atlayip sondurur → DogCompanion Extinguish() cagirir.
/// </summary>
public class FloorFire : MonoBehaviour, IStompable, IWaterDamageable
{
    [Header("Dolma")]
    [Tooltip("Barin dolma suresi. Kucuk = daha baskici.")]
    public float fillTime = 4f;
    [Tooltip("Barin gorseli — cocuk obje. Bos birakilabilir.")]
    public Transform fillBar;

    [Header("Faz 2 (dolmus hali)")]
    [Tooltip("Dolunca kac kat buyusun.")]
    public float grownScale = 1.7f;
    [Tooltip("Dolmus atese dokununca alinan hasar.")]
    public int contactDamage = 1;
    [Tooltip("Ustunde durursan kac saniyede bir hasar alirsin.")]
    public float damageInterval = 1f;
    public Color grownColor = new Color(1f, 0.35f, 0.1f);
    [Tooltip("Buyumus ates de ezilerek sondurulebilsin mi?\n" +
             "KAPALI (varsayilan) = buyudukten sonra SADECE su sondurur.\n" +
             "Playtest'te oyuncu susuz kalip sikisirsa bunu acin.")]
    public bool grownCanBeStomped = false;

    [Header("Su")]
    [Tooltip("WaterGun saniyede ~40 verir. 25 can = ~0.6 saniye.")]
    public float maxHealth = 25f;

    [Header("Sonme")]
    public float extinguishDuration = 0.25f;

    /// <summary>Bar doldu mu? Karabas ve UI okuyabilir.</summary>
    public bool IsGrown { get; private set; }

    /// <summary>Sonmus mu? Karabas ayni atesi iki kez sondurmeye calismasin diye.</summary>
    public bool IsExtinguished { get; private set; }

    float fillTimer;
    float health;
    float nextDamageTime;

    SpriteRenderer sprite;
    Color originalColor;
    Vector3 baseScale;
    Collider2D bodyCollider;

    void Awake()
    {
        health = maxHealth;
        baseScale = transform.localScale;

        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;

        bodyCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (IsExtinguished || IsGrown) return;

        // ---- Bar doluyor ----
        fillTimer += Time.deltaTime;

        if (fillBar != null)
        {
            float ratio = Mathf.Clamp01(fillTimer / fillTime);
            Vector3 s = fillBar.localScale;
            fillBar.localScale = new Vector3(ratio, s.y, s.z);
        }

        if (fillTimer >= fillTime) Grow();
    }

    void Grow()
    {
        IsGrown = true;

        transform.localScale = baseScale * grownScale;
        if (sprite != null) sprite.color = grownColor;

        // Bar isini bitirdi, gizle.
        if (fillBar != null) fillBar.gameObject.SetActive(false);
    }

    // ---- IStompable ----

    /// <summary>Ezme su an bu atesi sondurur mu?</summary>
    public bool CanBeStomped => !IsGrown || grownCanBeStomped;

    /// <summary>
    /// Kisi A'nin PlayerController'i, ustune dustugumuzu algilayinca cagirir.
    /// Kucuk fazda soner; buyumus faz varsayilan olarak sonmez (sadece su).
    /// </summary>
    public void OnStomped()
    {
        if (!CanBeStomped) return;
        Extinguish();
    }

    // ---- IWaterDamageable ----

    public void TakeWater(float amount)
    {
        if (IsExtinguished) return;

        health -= amount;
        if (health <= 0f) Extinguish();
    }

    // ---- Karabas / disaridan sondurme ----

    /// <summary>Karabas ustune atlayinca bunu cagirir. Kisi A da tetikleyebilir.</summary>
    public void Extinguish()
    {
        if (IsExtinguished) return;
        StartCoroutine(ExtinguishRoutine());
    }

    IEnumerator ExtinguishRoutine()
    {
        IsExtinguished = true;
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (fillBar != null) fillBar.gameObject.SetActive(false);

        Vector3 startScale = transform.localScale;
        Color startColor = sprite != null ? sprite.color : Color.white;
        float t = 0f;

        while (t < extinguishDuration)
        {
            t += Time.deltaTime;
            float k = t / extinguishDuration;

            transform.localScale = Vector3.Lerp(startScale, startScale * 0.1f, k);

            if (sprite != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, k);
                sprite.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // ---- Temas hasari (sadece faz 2) ----

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsGrown || IsExtinguished) return;
        if (!collision.collider.CompareTag("Player")) return;

        // Kopek asla oyuncunun hatasiyla olmez — atese dokunmasi zararsiz.
        // (Karabas zaten atesi sondurmek icin ustune atliyor.)

        // Ustunden ezerek geldiyse hasar verme — o ezme, PlayerController
        // OnStomped'i zaten cagirdi ve ates sondu.
        //
        // AMA ates ezilemez haldeyse bu muafiyet gecersiz: dev alevin ustune
        // atlayan oyuncu yanmali, yoksa sondurulemeyen atesin ustunde bedava
        // ziplayan bir platform gibi durur.
        if (CanBeStomped)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y >= 0.5f) return;
            }
        }

        if (Time.time < nextDamageTime) return;
        nextDamageTime = Time.time + damageInterval;

        // SendMessage kullanma sebebi FlameProjectile.cs'te aciklandi:
        // Kisi A'nin PlayerHealth'i henuz yok, derleme bagimliligi kurmuyoruz.
        collision.collider.SendMessageUpwards("TakeDamage", contactDamage,
            SendMessageOptions.DontRequireReceiver);
    }
}
