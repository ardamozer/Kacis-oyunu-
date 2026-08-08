using System.Collections;
using UnityEngine;

/// <summary>
/// SIKISMIS KOPEGI ORTEN ATES — PLAN.md "Karabas'la bag kurma" madde 3.
///
/// Karabas'in ustune koydugunuz kucuk bir ates objesi. Oyuncu suyu sikip
/// bunu sondurunce Karabas serbest kalir. Bulma ani PASIF bir tetikleyici
/// degil, OYNANAN bir an olmali — bu yuzden gercek IWaterDamageable, gercek
/// can barı mantigi kullaniliyor (FloorFire'la ayni desen, ama daha kisa).
///
/// ⚠️ Bu script Kisi A'nin hicbir sinifina bagli DEGIL. Tetikleme tamamen
/// Karabas'in kendi prefabinin icinde oluyor (DogCompanion.OnRescued /
/// StartFollowing'i dogrudan cagirir) — boylece Kisi A entegre etmeden
/// once bile TestScene_B'de bastan sona test edilebilir.
/// </summary>
public class TrappedFireCover : MonoBehaviour, IWaterDamageable
{
    [Header("Baglanti")]
    [Tooltip("Bu atesin ustunu ortttugu Karabas. Ayni ebeveyn altinda olmasi onerilir.")]
    public DogCompanion dog;

    [Header("Sondurme")]
    [Tooltip("WaterGun saniyede ~40 verir. 18 can = ~0.45 sn. Kucuk tutun — bu bir bulmaca degil, bir an.")]
    public float maxHealth = 18f;
    public Color hitFlashColor = Color.white;
    public float extinguishDuration = 0.3f;

    [Header("Kurtarma sirasi")]
    [Tooltip("Ates sonup OnRescued cagrildiktan sonra, StartFollowing'e kadar bekleme.")]
    public float delayBeforeFollow = 1.2f;

    bool isExtinguished;
    float health;
    float flashUntil;

    SpriteRenderer sprite;
    Color originalColor;
    Collider2D bodyCollider;

    void Awake()
    {
        health = maxHealth;

        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;

        bodyCollider = GetComponent<Collider2D>();

        if (dog == null) dog = GetComponentInParent<DogCompanion>();
    }

    void Update()
    {
        if (sprite != null && flashUntil > 0f && Time.time >= flashUntil)
        {
            sprite.color = originalColor;
            flashUntil = 0f;
        }
    }

    // ---- IWaterDamageable ----

    public void TakeWater(float amount)
    {
        if (isExtinguished) return;

        health -= amount;

        if (sprite != null)
        {
            sprite.color = Color.Lerp(originalColor, hitFlashColor, 0.6f);
            flashUntil = Time.time + 0.08f;
        }

        if (health <= 0f) StartCoroutine(ExtinguishAndRescue());
    }

    IEnumerator ExtinguishAndRescue()
    {
        isExtinguished = true;
        if (bodyCollider != null) bodyCollider.enabled = false;

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

        gameObject.SetActive(false);

        // Karabas serbest kaldi. Kisi A'nin kamera odaklanma efekti (PLAN.md
        // madde 6, "ilk kez ates sondurdugunde kamera bir an ona odaklansin")
        // bu OnRescued cagrisini disaridan da dinleyebilir — burada sadece
        // kopegin kendi durum gecisini garanti ediyoruz.
        if (dog != null)
        {
            dog.OnRescued();
            yield return new WaitForSeconds(delayBeforeFollow);
            dog.StartFollowing();
        }
    }
}
