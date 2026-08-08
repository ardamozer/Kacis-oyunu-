using System.Collections;
using UnityEngine;

/// <summary>
/// KARABAS — oyuncunun kurtaracagi, sonra pesinden gelen kopek.
///
/// Uc durum:
///   Stuck    — baslangic. Sikismis, inliyor. Henuz takip etmiyor.
///   Freed    — OnRescued() cagrildi (alev sondu). Mutlu havlar, henuz durgun.
///   Following— StartFollowing() cagrildi. Oyuncuyu takip eder, yoldaki
///              buyumus yer ateslerinin ustune atlayip sondurur.
///
/// PLAN.md kurallari:
///   - "Bastan duyulsun": Stuck halinde bile, oyuncu yaklastikca inleme sesi
///     yukselir. Oyuncu kopegi GORMEDEN once ONUN SESINI duymali.
///   - Sevme tusu (E, Kisi A cagirir) her zaman calisir, hicbir mekanik
///     etkisi yok — sadece kalp + kisa havlama.
///   - Kopek asla oyuncunun hatasiyla olmez; bu script hicbir hasar/olum
///     mantigi icermez (o EndingSequence'ta, scripted).
/// </summary>
public class DogCompanion : MonoBehaviour
{
    enum State { Stuck, Freed, Following }

    [Header("Takip")]
    [Tooltip("Oyuncudan bu kadar uzaga kadar yaklasir, sonra durur.")]
    public float followDistance = 1.5f;
    public float followSpeed = 4f;
    [Tooltip("Bos birakin — sahnedeki Tag='Player' objesini kendi bulur.")]
    public Transform player;

    [Header("Yoldaki atesi sondurme (Following sirasinda)")]
    [Tooltip("Bu yaricapta buyumus bir FloorFire varsa ustune atlayip sondurur.")]
    public float fireDetectRange = 1.0f;
    public float stompHopHeight = 0.3f;
    public float stompHopDuration = 0.3f;

    [Header("Ses — inleme (Stuck halindeyken)")]
    public AudioSource audioSource;
    public AudioClip whineClip;
    [Tooltip("Bu mesafeden yakinken ses tam seviyede calar.")]
    public float minDistanceForMaxVolume = 2f;
    [Tooltip("Bu mesafeden sonra hic duyulmaz.")]
    public float maxHearDistance = 15f;
    public float whineInterval = 3f;

    [Header("Ses — mutlu havlama (Freed / Petted)")]
    public AudioClip happyBarkClip;

    [Header("Sevme (OnPetted)")]
    [Tooltip("Opsiyonel. Bos birakilirsa sadece ses calar, particle olmaz.")]
    public GameObject heartParticlePrefab;
    public Transform heartSpawnPoint;

    State state = State.Stuck;
    float whineTimer;
    bool isStomping;

    SpriteRenderer sprite;
    Vector3 baseScale;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        whineTimer = whineInterval;
    }

    void Update()
    {
        if (state == State.Stuck) UpdateWhine();
        if (state == State.Following && !isStomping) FollowUpdate();
    }

    // ---- Stuck: uzaktan duyulan inleme ----

    void UpdateWhine()
    {
        if (player == null || audioSource == null || whineClip == null) return;

        whineTimer -= Time.deltaTime;
        if (whineTimer > 0f) return;
        whineTimer = whineInterval;

        float dist = Vector3.Distance(player.position, transform.position);
        if (dist > maxHearDistance) return;

        // Yakinsa tam ses, uzaklastikca sifira dogru soner.
        float t = Mathf.InverseLerp(maxHearDistance, minDistanceForMaxVolume, dist);
        audioSource.PlayOneShot(whineClip, Mathf.Clamp01(t));
    }

    // ---- Following: basit takip + yoldaki atesi sondurme ----

    void FollowUpdate()
    {
        if (player == null) return;

        float dist = Mathf.Abs(player.position.x - transform.position.x);

        if (dist > followDistance)
        {
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            Vector3 target = transform.position + Vector3.right * dir * dist;
            transform.position = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);

            if (sprite != null) sprite.flipX = dir < 0f;
        }

        CheckForFireToStomp();
    }

    void CheckForFireToStomp()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, fireDetectRange);
        foreach (Collider2D hit in hits)
        {
            FloorFire fire = hit.GetComponent<FloorFire>();
            if (fire != null && fire.IsGrown && !fire.IsExtinguished)
            {
                StartCoroutine(StompRoutine(fire));
                return;
            }
        }
    }

    IEnumerator StompRoutine(FloorFire fire)
    {
        isStomping = true;
        fire.Extinguish();

        // Kucuk bir ziplama — yeni asset gerektirmeyen ucuz ama okunakli geri bildirim.
        Vector3 start = transform.position;
        float t = 0f;

        while (t < stompHopDuration)
        {
            t += Time.deltaTime;
            float k = t / stompHopDuration;
            float hop = Mathf.Sin(k * Mathf.PI) * stompHopHeight;
            transform.position = start + Vector3.up * hop;
            yield return null;
        }

        transform.position = start;
        isStomping = false;
    }

    // ---- Disaridan cagrilan sozlesme metodlari (Kisi A cagirir) ----

    /// <summary>Kisi A'nin E tusu, oyuncu kopege yakinken bunu cagirir. Mekanik etkisi yok.</summary>
    public void OnPetted()
    {
        if (audioSource != null && happyBarkClip != null)
            audioSource.PlayOneShot(happyBarkClip);

        if (heartParticlePrefab != null)
        {
            Vector3 pos = heartSpawnPoint != null ? heartSpawnPoint.position : transform.position + Vector3.up;
            Instantiate(heartParticlePrefab, pos, Quaternion.identity);
        }
    }

    /// <summary>Sikistigi yerden kurtulunca (oyuncu ustundeki atesi sondurunce) Kisi A tetikler.</summary>
    public void OnRescued()
    {
        if (state != State.Stuck) return;
        state = State.Freed;

        if (audioSource != null && happyBarkClip != null)
            audioSource.PlayOneShot(happyBarkClip);
    }

    /// <summary>Kurtarma animasyonu bitince Kisi A takibi baslatmak icin bunu cagirir.</summary>
    public void StartFollowing()
    {
        state = State.Following;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.9f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, fireDetectRange);
    }
}
