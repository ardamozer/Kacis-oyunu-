using System.Collections;
using UnityEngine;

/// <summary>
/// ALEV PERDESI — gecidi kapatan, SADECE suyla sonen dikey alev.
///
/// ⚠️ NEDEN VAR:
/// Oyunun ana mekanigi su tabancasiydi ama oyuncu onu hic kullanmadan
/// oyunu bitirebiliyordu: duvar canavarlari kacilarak geciliyor,
/// yer atesleri kucukken ezilerek sonuyor, 336 birimde 6 su kaynagi
/// oldugu icin su hic bitmiyordu. "Sinirli su" fantezisi hic
/// hissedilmiyordu cunku hicbir sey suyu harcamaya ZORLAMIYORDU.
///
/// Bu perde zinciri kuruyor:
///   su zorunlu → su biter → musluk aramak anlamli → canavarin
///   caldigi can ve su acitir → butun ekonomi canlanir.
///
/// Ezilemez, atlanamaz, kacilamaz. Tek cozum su.
///
/// ⚠️ SEVIYE TASARIMI SARTI — bunu okumadan perde koymayin:
/// Her perdenin ONUNDE, oyuncunun ULASABILECEGI bir su kaynagi olmali.
/// Ozellikle x=188'deki alev duvari geri donusu kapattiktan sonra:
/// o segmentte su yoksa susuz gelen oyuncu COZULEMEZ duruma duser.
/// Yerlestirme komutu bunu otomatik dogruluyor (AlevCurtainSetup).
/// </summary>
public class FlameCurtain : MonoBehaviour, IWaterDamageable
{
    [Header("Dayaniklilik")]
    [Tooltip("WaterGun saniyede 40 hasar veriyor, depo 100 ve harcama 15/sn.\n" +
             "80 can = 2 saniye sikma = 30 su. Dolu depoyla 3 perde gecilir.")]
    public float maxHealth = 80f;

    [Header("Gorsel")]
    public Color hitFlashColor = Color.white;
    [Tooltip("Sonerken asagi dogru cokme suresi.")]
    public float extinguishDuration = 0.45f;

    [Header("Ses")]
    public AudioClip extinguishClip;
    [Range(0f, 1f)] public float extinguishVolume = 0.85f;

    /// <summary>Sondu mu? Kapi acildi mi diye soranlar okuyabilir.</summary>
    public bool IsExtinguished { get; private set; }

    float health;
    float flashUntil;

    SpriteRenderer sprite;
    Color originalColor;
    Collider2D blocker;

    void Awake()
    {
        health = maxHealth;

        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;

        blocker = GetComponent<Collider2D>();
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
        if (IsExtinguished) return;

        health -= amount;

        if (sprite != null)
        {
            // "Isliyor" geri bildirimi. Bu olmadan oyuncu 2 saniye boyunca
            // hicbir sey olmuyor sanip vazgeciyor.
            sprite.color = Color.Lerp(originalColor, hitFlashColor, 0.55f);
            flashUntil = Time.time + 0.08f;
        }

        if (health <= 0f) StartCoroutine(ExtinguishRoutine());
    }

    IEnumerator ExtinguishRoutine()
    {
        IsExtinguished = true;

        // Gecis HEMEN acilsin — sonme animasyonunu beklerken oyuncu
        // duvara toslamaya devam ederse "sondurdum ama gecemiyorum" hissi olur.
        if (blocker != null) blocker.enabled = false;

        if (extinguishClip != null)
            AudioSource.PlayClipAtPoint(extinguishClip, transform.position, extinguishVolume);

        // Titreme varsa sonme animasyonuyla kavga etmesin.
        FireFlicker flicker = GetComponent<FireFlicker>();
        if (flicker != null) flicker.enabled = false;

        Vector3 startScale = transform.localScale;
        Color startColor = sprite != null ? sprite.color : Color.white;
        float t = 0f;

        while (t < extinguishDuration)
        {
            t += Time.deltaTime;
            float k = t / extinguishDuration;

            // Yatayda kalir, dikeyde coker — sonen bir alev perdesi boyle durur.
            transform.localScale = new Vector3(
                startScale.x,
                Mathf.Lerp(startScale.y, startScale.y * 0.05f, k),
                startScale.z);

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
}
