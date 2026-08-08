using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Itfaiyecinin cani. 3 kalp.
///
/// ⚠️ SOZLESME — Kisi B'nin dusmanlari bunu cagiriyor:
///     TakeDamage(int amount)
///     CurrentHealth
/// Imzalari degistirmeyin, arkadasinizin kodu kirilir.
///
/// Dusmanlar SendMessageUpwards("TakeDamage", int) ile cagiriyor
/// (PlayerHealth henuz yokken derleme bagimliligi kurmamak icin).
/// Bu yuzden metod adi ve int parametresi kritik.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Can")]
    public int maxHealth = 3;

    [Tooltip("Hasar aldiktan sonra ne kadar sure daha hasar almaz. " +
             "Bu olmadan atesin icinde 3 kalbi aninda kaybedersiniz.")]
    public float invulnerabilityTime = 1.2f;

    [Header("Hasar Geri Bildirimi")]
    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 0.35f;

    [Tooltip("Dokunulmazlik sirasinda saniyede kac kez yanip sonsun.")]
    public float flashesPerSecond = 8f;

    [Header("Olum")]
    [Tooltip("Oldukten kac saniye sonra sahne yeniden yuklensin.")]
    public float respawnDelay = 1.5f;

    [Header("Canli Durum (sadece izlemek icin)")]
    [SerializeField] int currentHealth;
    [SerializeField] bool invulnerable;

    /// <summary>Kalan can. Final sahnesi bunu okuyup boss sahnesini tetikliyor.</summary>
    public int CurrentHealth => currentHealth;

    /// <summary>0-1 arasi can orani. HUD icin.</summary>
    public bool IsDead => currentHealth <= 0;

    /// <summary>Hasar aninda 1'e cikip sonen deger. Ekran kenari alev efekti bunu kullanir.</summary>
    public float DamageFlash { get; private set; }

    float invulnerableUntil;
    SpriteRenderer sprite;
    PlayerController controller;
    bool dead;

    void Awake()
    {
        currentHealth = maxHealth;
        sprite = GetComponentInChildren<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        invulnerable = Time.time < invulnerableUntil;

        // Dokunulmazlik gorseli: yanip sonme
        if (sprite != null)
        {
            bool visible = !invulnerable || Mathf.Sin(Time.time * flashesPerSecond * Mathf.PI * 2f) > 0f;
            Color c = sprite.color;
            c.a = visible ? 1f : 0.25f;
            sprite.color = c;
        }

        // Hasar parlamasi sonuyor
        DamageFlash = Mathf.MoveTowards(DamageFlash, 0f, Time.deltaTime * 1.5f);
    }

    /// <summary>
    /// Hasar verir. Dusmanlar bunu cagirir.
    /// Dokunulmazlik suresi icindeyse hicbir sey olmaz.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0) return;
        if (Time.time < invulnerableUntil) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        invulnerableUntil = Time.time + invulnerabilityTime;

        DamageFlash = 1f;

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);

        Debug.Log($"[PlayerHealth] Hasar {amount}. Kalan can: {currentHealth}");

        if (currentHealth <= 0) Die();
    }

    /// <summary>Can doldurur (jam'de kullanilmayabilir ama dursun).</summary>
    public void Heal(int amount)
    {
        if (dead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    void Die()
    {
        dead = true;

        // Kontrolu kes — final sahnesinde de ayni bayrak kullaniliyor.
        if (controller != null) controller.CanControl = false;

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.6f, 0.6f);

        Debug.Log("[PlayerHealth] Oyuncu oldu.");
        Invoke(nameof(ReloadScene), respawnDelay);
    }

    void ReloadScene()
    {
        // Checkpoint alinmissa sahneyi YENIDEN YUKLEMIYORUZ — oyuncuyu
        // son checkpoint'e tasiyoruz. Yeniden yukleseydik alinan anahtarlar
        // ve sondurulmus atesler de sifirlanir, oyuncu kilitli kapinin
        // onunde anahtarsiz kalabilirdi.
        //
        // (Kisi B ekledi. Checkpoint bileseni sahnede yoksa bu satir
        //  false doner ve eski davranis aynen calisir.)
        if (CheckpointManager.TryRespawn(this)) return;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Checkpoint sistemi cagirir: oyuncuyu yeniden oynanabilir hale getirir.
    /// Kisa bir dokunulmazlik veriyoruz, yoksa oyuncu doğduğu anda
    /// ustundeki atesten tekrar olebilir.
    /// </summary>
    public void Revive()
    {
        dead = false;
        currentHealth = maxHealth;
        invulnerableUntil = Time.time + invulnerabilityTime;
        DamageFlash = 0f;

        if (controller != null) controller.CanControl = true;
    }
}
