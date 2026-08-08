using UnityEngine;

/// <summary>
/// Test hedefi. Hem suyla sondurulebilir hem ziplayarak ezilebilir —
/// yani iki mekanigi de tek objede test edersiniz.
///
/// Kisi B'nin gercek dusmanlari (WallMonster, FloorFire) prefab olarak
/// gelince bunlari sahneden silin.
/// </summary>
public class TestTarget : MonoBehaviour, IWaterDamageable, IStompable
{
    [Tooltip("Kac su hasari dayanacak. WaterGun saniyede 20 veriyor.")]
    public float health = 60f;

    [Tooltip("Kac kere ezilince olsun. 1 = tek ezmede olur.")]
    public int stompsToKill = 3;

    [Tooltip("Ezildikten sonra kisa sure ezilemez olsun (art arda sayilmasin).")]
    public float stompCooldown = 0.4f;

    int stompsTaken;
    float lastStompTime = -99f;

    SpriteRenderer sprite;
    Color startColor;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) startColor = sprite.color;
    }

    public void TakeWater(float amount)
    {
        health -= amount;

        // Isabet geri bildirimi: hasar aldikca beyazlasir.
        if (sprite != null)
        {
            sprite.color = Color.Lerp(Color.white, startColor, Mathf.Clamp01(health / 60f));
        }

        if (health <= 0f)
        {
            Debug.Log($"[TestTarget] {name} suyla sonduruldu.");
            Destroy(gameObject);
        }
    }

    public void OnStomped()
    {
        // Tek ziplamada carpisma birden fazla kez tetiklenebilir; bekleme suresi
        // bunu engelliyor, yoksa 3 ezme tek ziplamada biterdi.
        if (Time.time - lastStompTime < stompCooldown) return;
        lastStompTime = Time.time;

        stompsTaken++;
        int remaining = stompsToKill - stompsTaken;

        if (remaining > 0)
        {
            Debug.Log($"[TestTarget] {name} ezildi — {remaining} ezme daha lazim.");
            // Hasar geri bildirimi: her ezmede biraz yassilasir.
            transform.localScale = new Vector3(
                transform.localScale.x,
                transform.localScale.y * 0.8f,
                transform.localScale.z);
            return;
        }

        Debug.Log($"[TestTarget] {name} ezilerek olduruldu.");
        Destroy(gameObject);
    }
}
