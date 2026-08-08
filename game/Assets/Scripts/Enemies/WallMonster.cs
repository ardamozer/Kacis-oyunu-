using System.Collections;
using UnityEngine;

/// <summary>
/// DUVAR CANAVARI — yanan tablo / kitaplik.
///
/// Davranis:
///   1. Duvarin icinde gizli bekler (gorunmez degil, duvara gomulu durur).
///   2. Oyuncu menzile girince duvardan disari kayarak cikar.
///   3. Tam cikinca belirli araliklarla alev firlatir.
///   4. Oyuncu menzilden cikarsa geri duvara girer ve atesi keser.
///   5. Su degince cani azalir, sifirlaninca soner ve yok olur.
///
/// TASARIM NOTU: Bu canavar OLDURULMEK ZORUNDA DEGIL (PLAN.md kilitlenme kurali 4).
/// Oyuncu kacarak da gecebilmeli — bu yuzden menzil disina cikinca ates etmeyi birakir.
/// </summary>
public class WallMonster : MonoBehaviour, IWaterDamageable
{
    [Header("Algilama")]
    [Tooltip("Oyuncu bu mesafeye girince duvardan cikar.")]
    public float detectRange = 7f;
    [Tooltip("Bos birakin — sahnedeki Tag='Player' objesini kendi bulur.")]
    public Transform player;

    [Header("Duvardan Cikma")]
    [Tooltip("+1 = saga bakar (sol duvardaki canavar), -1 = sola bakar (sag duvardaki).")]
    public int facing = -1;
    [Tooltip("Duvardan ne kadar disari cikacagi (Unity birimi).")]
    public float emergeDistance = 0.9f;
    [Tooltip("Cikma/girme hizi. Buyuk = daha ani, korkutucu.")]
    public float emergeSpeed = 5f;

    [Header("Ates")]
    public GameObject flamePrefab;
    [Tooltip("Alevin dogacagi nokta. Canavarin agzi hizasinda, govdenin BIRAZ disinda olmali.")]
    public Transform firePoint;
    [Tooltip("Iki alev arasindaki sure.")]
    public float fireInterval = 2f;
    [Tooltip("Tam cikinca ilk alevi atmadan onceki bekleme. Oyuncuya tepki verme sansi tanir.")]
    public float firstShotDelay = 0.7f;
    [Tooltip("ACIK: alevi oyuncunun bulundugu yone atar, iki yone de ates edebilir.\n" +
             "KAPALI: her zaman 'facing' yonune atar (duvara gomulu, tek yone bakan canavar).")]
    public bool aimAtPlayer = true;

    [Header("Can")]
    [Tooltip("WaterGun saniyede ~40 su verir. 40 can = ~1 saniye sikma. Daha uzun dayansin istersen buyut.")]
    public float maxHealth = 40f;

    [Header("Gorsel")]
    [Tooltip("Su degince yanip sonecek renk.")]
    public Color hitFlashColor = Color.white;
    public float deathDuration = 0.35f;

    [Header("Ses (hepsi opsiyonel)")]
    [Tooltip("Alev firlatirken calar.")]
    public AudioClip shootClip;
    [Range(0f, 1f)] public float shootVolume = 0.5f;
    [Tooltip("Sonup olurken calar.")]
    public AudioClip deathClip;
    [Range(0f, 1f)] public float deathVolume = 0.7f;

    // ---- Ic durum ----
    Vector3 hiddenPos;      // duvarin icindeki baslangic konumu
    Vector3 shownPos;       // tamamen cikmis konum
    float health;
    float fireTimer;
    float flashUntil;
    bool isDead;

    SpriteRenderer sprite;
    Color originalColor;
    Collider2D bodyCollider;

    void Awake()
    {
        // Sahneye koydugunuz konum = GIZLI konum. Cikis konumu ondan hesaplanir.
        hiddenPos = transform.position;
        shownPos = hiddenPos + Vector3.right * facing * emergeDistance;

        health = maxHealth;
        fireTimer = firstShotDelay;

        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            originalColor = sprite.color;
            sprite.flipX = facing < 0;
        }

        bodyCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        // Inspector'da elle baglanmadiysa oyuncuyu Tag ile bul.
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        // Su hasari flash'ini geri al.
        if (sprite != null && flashUntil > 0f && Time.time >= flashUntil)
        {
            sprite.color = originalColor;
            flashUntil = 0f;
        }

        bool playerNear = IsPlayerInRange();

        // Sprite ates edecegi yone baksin — yoksa sola bakip saga ates eder.
        if (sprite != null && aimAtPlayer && player != null)
            sprite.flipX = ShootDirection() < 0;

        // Cikma / girme: tek satirda, durum makinesi olmadan.
        Vector3 target = playerNear ? shownPos : hiddenPos;
        transform.position = Vector3.MoveTowards(transform.position, target, emergeSpeed * Time.deltaTime);

        // Sadece TAMAMEN cikmisken ates eder. Yarim cikmisken atmaz.
        bool fullyOut = (transform.position - shownPos).sqrMagnitude < 0.0025f;

        if (playerNear && fullyOut)
        {
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                Shoot();
                fireTimer = fireInterval;
            }
        }
        else
        {
            // Duvara girince sayaci sifirla — tekrar ciktiginda hemen atmasin.
            fireTimer = firstShotDelay;
        }
    }

    bool IsPlayerInRange()
    {
        if (player == null) return false;
        return (player.position - shownPos).sqrMagnitude <= detectRange * detectRange;
    }

    /// <summary>Alevin gidecegi yon: +1 sag, -1 sol.</summary>
    int ShootDirection()
    {
        if (!aimAtPlayer || player == null) return facing;
        return player.position.x >= transform.position.x ? 1 : -1;
    }

    void Shoot()
    {
        if (flamePrefab == null || firePoint == null) return;

        int dir = ShootDirection();

        // FirePoint govdenin bir yaninda duruyor. Ters yone ates ederken onu
        // aynalamazsak alev canavarin govdesinin ICINDEN cikiyor gibi gorunur.
        // firePoint.position (dunya konumu) kullaniyoruz ki olcek de hesaba katilsin.
        Vector3 offset = firePoint.position - transform.position;
        if (offset.x != 0f && Mathf.Sign(offset.x) != dir) offset.x = -offset.x;

        GameObject flame = Instantiate(flamePrefab, transform.position + offset, Quaternion.identity);

        if (shootClip != null)
            AudioSource.PlayClipAtPoint(shootClip, transform.position, shootVolume);

        // Alev kendi yonunu bilmiyor — biz veriyoruz.
        FlameProjectile projectile = flame.GetComponent<FlameProjectile>();
        if (projectile != null) projectile.Launch(new Vector2(dir, 0f));
    }

    // ---- IWaterDamageable ----

    /// <summary>Kisi A'nin WaterGun'i her karede bunu cagirir.</summary>
    public void TakeWater(float amount)
    {
        if (isDead) return;

        health -= amount;

        if (sprite != null)
        {
            // "Su degiyor" geri bildirimi. TakeWater her karede cagrildigi icin
            // burada Invoke/Coroutine kullanmiyoruz — sadece bir zaman damgasi
            // birakiyoruz, rengi Update geri aliyor.
            sprite.color = Color.Lerp(originalColor, hitFlashColor, 0.6f);
            flashUntil = Time.time + 0.08f;
        }

        if (health <= 0f) StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        isDead = true;
        if (bodyCollider != null) bodyCollider.enabled = false;

        // Obje Destroy edilecek — kendi AudioSource'u sesi keserdi.
        if (deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, transform.position, deathVolume);

        // Sonme animasyonu: kucul + saydamlas. Yeni asset gerektirmez.
        Vector3 startScale = transform.localScale;
        float t = 0f;

        while (t < deathDuration)
        {
            t += Time.deltaTime;
            float k = t / deathDuration;

            transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, k);

            if (sprite != null)
            {
                Color c = originalColor;
                c.a = Mathf.Lerp(originalColor.a, 0f, k);
                sprite.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // Sahne penceresinde menzili ve cikis konumunu gosterir — ayarlamayi kolaylastirir.
    void OnDrawGizmosSelected()
    {
        // Editorde (Play degilken) shownPos henuz hesaplanmamis olur, burada hesapla.
        Vector3 hidden = Application.isPlaying ? hiddenPos : transform.position;
        Vector3 shown = Application.isPlaying ? shownPos : hidden + Vector3.right * facing * emergeDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(shown, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(hidden, shown);
        Gizmos.DrawWireCube(shown, Vector3.one * 0.3f);
    }
}
