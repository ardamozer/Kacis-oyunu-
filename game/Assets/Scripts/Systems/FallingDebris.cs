using System.Collections;
using UnityEngine;

/// <summary>
/// Tavandan dusen yanan tahta / kiris.
///
/// Ucuncu tehdit tipi: suyla SONMEZ, ezilerek OLMEZ — sadece kacilir.
/// Her tehdidin bir cozumu oldugu oyun tekduze olur; "bunu cozemezsin,
/// sadece kac" ceşitlilik katar. Ayrica tek dikey tehdit bu.
///
/// Oyuncu yaklasinca once UYARI cikar, sonra duser. Telegrafli tehlike
/// adildir; telegrafsizi sinir bozar.
///
/// ⚠️ TEK ISTISNA: Karabas'in olum sahnesi. Orada uyari GOSTERILMEZ
///    (DropNow(false) ile cagrilir). Oyun boyunca "unlem gorursen kac"
///    ogretildigi icin, uyarisiz dusen tahta "bunu kimse goremezdi"
///    demektir — oyuncu kendini suclamaz, kopegin fedakarligi anlam kazanir.
/// </summary>
public class FallingDebris : MonoBehaviour
{
    [Header("Tetikleme")]
    [Tooltip("Oyuncu yatayda bu kadar yaklasinca uyari baslar.")]
    public float triggerDistanceX = 3f;

    [Tooltip("Kapaliysa sadece DropNow() ile duser — final sahnesi icin.")]
    public bool autoTrigger = true;

    [Header("Uyari")]
    [Tooltip("Uyari ne kadar surer. 0.6-1.0 arasi adil. Kisaltirsaniz haksiz olur.")]
    public float warningDuration = 0.8f;

    [Tooltip("Dusecegi yere konan unlem gorseli. Sahnede devre disi baslamali.")]
    public GameObject warningIndicator;

    [Tooltip("Uyari saniyede kac kez yanip sonsun.")]
    public float warningBlinksPerSecond = 6f;

    [Header("Dusme")]
    public float fallSpeed = 18f;
    public int damage = 1;

    [Tooltip("Yere degdikten sonra kac saniye durup yok olsun. " +
             "0 = hic kalmasin. Kalirsa gecici bir engel/platform olur.")]
    public float lifetimeAfterLanding = 4f;

    [Header("Carpma Hissi")]
    public float landShakeDuration = 0.25f;
    public float landShakeMagnitude = 0.3f;
    public ParticleSystem dustEffect;
    public AudioSource crashSound;

    [Header("Zemin")]
    [Tooltip("Neye carpinca duracak. Ground layer'ini secin.")]
    public LayerMask groundLayer;

    Vector3 startPosition;
    Transform player;
    Collider2D myCollider;
    SpriteRenderer sprite;
    bool triggered;
    bool falling;
    bool landed;

    void Awake()
    {
        startPosition = transform.position;
        myCollider = GetComponent<Collider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

        if (warningIndicator != null) warningIndicator.SetActive(false);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (triggered || !autoTrigger || player == null) return;

        // Sadece yatay mesafeye bakiyoruz — oyuncu asagidan geciyorsa
        // hangi yukseklikte oldugu onemli degil.
        if (Mathf.Abs(player.position.x - transform.position.x) <= triggerDistanceX)
        {
            DropNow(true);
        }
    }

    /// <summary>
    /// Tahtayi dusurur.
    /// </summary>
    /// <param name="showWarning">
    /// false verilirse uyari CIKMAZ — kacilmasi imkansiz olur.
    /// Sadece Karabas'in olum sahnesinde false kullanin.
    /// </param>
    public void DropNow(bool showWarning = true)
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(DropRoutine(showWarning));
    }

    IEnumerator DropRoutine(bool showWarning)
    {
        if (showWarning && warningIndicator != null)
        {
            warningIndicator.SetActive(true);

            float t = 0f;
            var indicatorSprite = warningIndicator.GetComponent<SpriteRenderer>();

            while (t < warningDuration)
            {
                t += Time.deltaTime;

                if (indicatorSprite != null)
                {
                    bool visible = Mathf.Sin(t * warningBlinksPerSecond * Mathf.PI * 2f) > 0f;
                    Color c = indicatorSprite.color;
                    c.a = visible ? 1f : 0.25f;
                    indicatorSprite.color = c;
                }
                yield return null;
            }

            warningIndicator.SetActive(false);
        }
        else if (showWarning)
        {
            // Gorsel atanmamissa bile bekleme suresi korunsun, oyun adil kalsin.
            yield return new WaitForSeconds(warningDuration);
        }

        falling = true;

        while (falling)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            // Zemine degdi mi?
            if (Physics2D.OverlapCircle(transform.position, 0.4f, groundLayer))
            {
                Land();
            }
            yield return null;
        }
    }

    void Land()
    {
        falling = false;
        landed = true;

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(landShakeDuration, landShakeMagnitude);

        if (dustEffect != null) dustEffect.Play();
        if (crashSound != null) crashSound.Play();

        if (lifetimeAfterLanding <= 0f) Destroy(gameObject);
        else StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(lifetimeAfterLanding);

        // Sonerek kaybolur — aniden yok olmasi ucuz durur.
        float t = 0f;
        const float fade = 0.6f;
        Color start = sprite != null ? sprite.color : Color.white;

        while (t < fade)
        {
            t += Time.deltaTime;
            if (sprite != null)
            {
                Color c = start;
                c.a = Mathf.Lerp(start.a, 0f, t / fade);
                sprite.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);
    void OnTriggerEnter2D(Collider2D other) => TryDamage(other);

    void TryDamage(Collider2D other)
    {
        // Sadece DUSERKEN hasar verir. Yere indikten sonra uzerine
        // basilabilen zararsiz bir enkaz olur.
        if (!falling || landed) return;
        if (!other.CompareTag("Player")) return;

        // PlayerHealth'e dogrudan bagimlilik kurmuyoruz — Kisi B'nin
        // dusmanlariyla ayni yontem, tutarli kalsin.
        other.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>Sahneyi yeniden kurmadan tekrar denemek icin.</summary>
    public void ResetDebris()
    {
        StopAllCoroutines();
        transform.position = startPosition;
        triggered = falling = landed = false;
        if (warningIndicator != null) warningIndicator.SetActive(false);
        if (sprite != null)
        {
            Color c = sprite.color;
            c.a = 1f;
            sprite.color = c;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Tetikleme mesafesi
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawLine(transform.position + Vector3.left * triggerDistanceX,
                        transform.position + Vector3.right * triggerDistanceX);

        // Dusus hatti
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 12f);
    }
}
