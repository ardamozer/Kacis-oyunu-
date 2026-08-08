using UnityEngine;

/// <summary>
/// FINAL BOSS'UN GOVDESI — devasa alev.
///
/// PLAN.md "Boss = dovus DEGIL, sahne": oyuncu suyu sikar, gorsel geri
/// bildirim alir (flash), AMA CANI HIC AZALMAZ. "Olduremiyorum" hissi
/// oyunun kasitli tasarim karari — bu yuzden IWaterDamageable'i uyguluyor
/// ama TakeWater icinde hasar mantigi yok.
///
/// Sprite: WallMonster'in sprite'i 4 kat buyutulmus + koyu kirmizi renk
/// (PLAN.md — yeni asset gerekmiyor).
/// </summary>
public class BossCore : MonoBehaviour, IWaterDamageable
{
    [Tooltip("Su degince yanip sonecek renk.")]
    public Color hitFlashColor = Color.white;
    public float flashDuration = 0.08f;

    SpriteRenderer sprite;
    Color originalColor;
    float flashUntil;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;
    }

    void Update()
    {
        if (sprite != null && flashUntil > 0f && Time.time >= flashUntil)
        {
            sprite.color = originalColor;
            flashUntil = 0f;
        }
    }

    /// <summary>KASITLI: can azaltmiyor, sadece "su degdi" geri bildirimi verir.</summary>
    public void TakeWater(float amount)
    {
        if (sprite == null) return;
        sprite.color = Color.Lerp(originalColor, hitFlashColor, 0.6f);
        flashUntil = Time.time + flashDuration;
    }
}
