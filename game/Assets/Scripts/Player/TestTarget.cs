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
    public int stompsToKill = 2;

    [Tooltip("Ezildikten sonra kisa sure ezilemez olsun (art arda sayilmasin).")]
    public float stompCooldown = 0.4f;

    [Header("Geri Itme (oldurucu olmayan ezmede)")]
    [Tooltip("Alev parlayip oyuncuyu yana iter. Spam ziplamayi engeller, " +
             "ezmeyi ritim gerektiren bir ise cevirir.")]
    public float knockbackForce = 9f;

    [Tooltip("Parlama sirasinda kac katina buyusun.")]
    public float flareScale = 1.5f;

    public float flareDuration = 0.18f;

    int stompsTaken;
    float lastStompTime = -99f;
    Vector3 baseScale;

    SpriteRenderer sprite;
    Color startColor;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) startColor = sprite.color;
        baseScale = transform.localScale;
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
            StartCoroutine(FlareRoutine());
            KnockPlayerBack();

            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.12f, 0.15f);
            return;
        }

        Debug.Log($"[TestTarget] {name} ezilerek olduruldu.");
        Destroy(gameObject);
    }

    /// <summary>Alev bir an parlayip sonuyor — ezmenin bedeli oldugunu gosterir.</summary>
    System.Collections.IEnumerator FlareRoutine()
    {
        float t = 0f;
        while (t < flareDuration)
        {
            t += Time.deltaTime;
            // Basta hizla buyu, sonra geri in.
            float k = Mathf.Sin(Mathf.Clamp01(t / flareDuration) * Mathf.PI);
            transform.localScale = baseScale * Mathf.Lerp(1f, flareScale, k);
            yield return null;
        }
        transform.localScale = baseScale;
    }

    /// <summary>
    /// Oyuncuyu yana iter. PlayerController ezmeden hemen sonra dikey hizi
    /// kendisi ayarliyor ama YATAY hizi koruyor — bu yuzden burada sadece
    /// yatay itme veriyoruz, ikisi birlesince guzel bir savrulma cikiyor.
    /// </summary>
    void KnockPlayerBack()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        float dir = player.transform.position.x - transform.position.x;
        // Tam tepedeyse yon belirsiz kalir; o durumda bakilan yonun tersine it.
        if (Mathf.Abs(dir) < 0.05f)
        {
            var pc = player.GetComponent<PlayerController>();
            dir = pc != null ? -pc.FacingDirection : 1f;
        }

        rb.linearVelocity = new Vector2(Mathf.Sign(dir) * knockbackForce, rb.linearVelocity.y);
    }
}
