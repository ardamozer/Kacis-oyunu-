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

    [Header("Zemin takibi")]
    [Tooltip("Ground layer'ini secin. Kopek bunun uzerinde yurur.\n" +
             "BOS BIRAKIRSANIZ kopek dikeyde hic hareket etmez (havada kalir).")]
    public LayerMask groundLayer;
    [Tooltip("Zemin aranirken kopegin ne kadar USTUNDEN isin atilir.\n" +
             "Ayni zamanda cikabilecegi basamak yuksekligidir.")]
    public float groundProbeUp = 1.5f;
    [Tooltip("Asagi dogru ne kadar zemin aranir.")]
    public float groundProbeDown = 8f;
    [Tooltip("Dikey hizalanma hizi. Buyuk = daha ani oturur.")]
    public float verticalSpeed = 14f;

    [Header("Isinlanma — kat degisince kaybolmasin")]
    [Tooltip("Oyuncu bu mesafeden uzaklasirsa kopek yanina isinir.\n" +
             "Kat plani iki katli (y=0 ve y=9); kopek merdiveni tirmanamaz,\n" +
             "bu olmadan asagida kalir ve final sahnesine hic gelemez.\n" +
             "0 = kapali.")]
    public float warpDistance = 14f;

    [Header("Koruma ani — onunde canavar varsa durur")]
    [Tooltip("Karabas onunde CANLI bir duvar canavari varsa durur ve havlar.\n" +
             "Oyuncu canavari sondurunce devam eder.\n\n" +
             "PLAN.md madde 7: 'bir kez oyuncu kopegi korusun'. Escort\n" +
             "bolumunun tek mekanik ani bu — yoksa oyuncu sadece yuruyup\n" +
             "kopegin atesleri sondurmesini izliyor.")]
    public bool stopAtMonsters = true;
    [Tooltip("Onunde bu mesafede canavar varsa durur.")]
    public float monsterBlockRange = 6f;
    [Tooltip("Beklerken kac saniyede bir havlasin — 'buraya gecemiyorum'.")]
    public float blockedBarkInterval = 1.8f;

    [Header("Yoldaki atesi sondurme (Following sirasinda)")]
    [Tooltip("Bu yaricapta buyumus bir FloorFire varsa ustune atlayip sondurur.")]
    public float fireDetectRange = 1.0f;
    public float stompHopHeight = 0.3f;
    public float stompHopDuration = 0.3f;

    [Header("Ses — acilis havlamasi")]
    [Tooltip("Oyun baslar baslamaz bir kez havlar: 'ileride bir kopek var'.\n" +
             "PLAN.md: oyuncu kopegi GORMEDEN once onu aramaya baslamali.")]
    public bool openingBark = true;
    [Tooltip("Oyun basladiktan kac saniye sonra havlasin. Cok erken olursa\n" +
             "oyuncu daha kontrolleri kavramadan kacirir.")]
    public float openingBarkDelay = 1.5f;
    [Tooltip("Acilis havlamasi mesafeden BAGIMSIZ bu seviyede calar —\n" +
             "amaci duyulmak. Uzakta oldugu icin kisilirsa ise yaramaz.")]
    [Range(0f, 1f)] public float openingBarkVolume = 0.5f;

    [Header("Ses — inleme (Stuck halindeyken)")]
    public AudioSource audioSource;
    public AudioClip whineClip;
    [Tooltip("Bu mesafeden yakinken ses tam seviyede calar.")]
    public float minDistanceForMaxVolume = 4f;
    [Tooltip("Bu mesafeden sonra hic duyulmaz.\n" +
             "Oda genisligi ~20 birim; 32 = bir onceki odadan kisik duyulur.")]
    public float maxHearDistance = 32f;
    [Tooltip("Iki inleme arasindaki ortalama sure. Buyuk = daha seyrek,\n" +
             "daha rahatsiz etmeyen. Kopek surekli inlerse kulak aliskanlik\n" +
             "yapiyor ve etki kayboluyor.")]
    public float whineInterval = 7f;
    [Tooltip("Sureye eklenen rastgele sapma (+/-). Tam duzenli araliklarla\n" +
             "inleyen sey canli degil metronom gibi duyuluyor.")]
    public float whineIntervalJitter = 2.5f;

    [Header("Ses — mutlu havlama (Freed / Petted)")]
    public AudioClip happyBarkClip;
    [Tooltip("Her calista perde bu kadar rastgele sapar (0.08 = ±%8).\n" +
             "Ayni kaydin ust uste tipatip calmasi kulaga hemen 'dosya'\n" +
             "gibi geliyor — ozellikle sevme tusu arka arkaya basiliyor.\n" +
             "0 = kapali.")]
    public float pitchVariation = 0.08f;

    [Header("Sevme (OnPetted)")]
    [Tooltip("Opsiyonel. Bos birakilirsa sadece ses calar, kalp cikmaz.")]
    public GameObject heartParticlePrefab;
    public Transform heartSpawnPoint;
    [Tooltip("Sevilince kac kalp ciksin.")]
    public int heartCount = 3;

    [Header("Canlilik")]
    [Tooltip("Nefes alip verme: olcek ne kadar oynasin. 0 = kapali.\n" +
             "Duran bir kare 'canli' okunmuyor; bu tek satirlik hile cok is goruyor.")]
    public float breathAmount = 0.04f;
    public float breathSpeed = 3f;
    [Tooltip("Sevilince yapilan ezil-uza tepkisi. 0 = kapali.")]
    public float petSquashAmount = 0.22f;
    public float petSquashDuration = 0.4f;

    State state = State.Stuck;

    /// <summary>Kopek kurtarildi mi? (Freed veya Following durumunda).</summary>
    public bool IsRescued => state != State.Stuck;

    /// <summary>Kopek oyuncuyu takip ediyor mu?</summary>
    public bool IsFollowing => state == State.Following;

    float whineTimer;
    bool isStomping;
    float petSquashUntil;
    float blockedBarkTimer;

    SpriteRenderer sprite;
    Vector3 baseScale;

    // Govdenin yariyuksekligi — zemine otururken merkezi bu kadar yukari koyariz,
    // yoksa kopek zeminin icine gomulur.
    float groundOffset = 0.35f;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;

        if (sprite != null) groundOffset = sprite.bounds.extents.y;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        whineTimer = NextWhineDelay();

        // Baslangicta da zemine otursun — sahneye goz karariyla yerlestirilmis
        // olabilir, havada durmasin.
        SnapToGround(true);

        if (openingBark) StartCoroutine(OpeningBarkRoutine());
    }

    /// <summary>
    /// Oyunun ilk saniyelerinde bir kez havlar.
    ///
    /// Mesafeye gore KISILMIYOR: amaci "ileride bir kopek var" demek.
    /// Uzakta oldugu icin sessizlestirirsek oyuncu hic duymaz ve
    /// PLAN.md'nin "kopegi gormeden once ara" akisi hic baslamaz.
    /// </summary>
    IEnumerator OpeningBarkRoutine()
    {
        yield return new WaitForSeconds(openingBarkDelay);

        // Arada kurtarilmissa (test sirasinda olur) acilis havlamasi anlamsiz.
        if (state != State.Stuck) yield break;

        PlayVaried(happyBarkClip, openingBarkVolume);
    }

    /// <summary>Bir sonraki inlemeye kadar gecen sure — sabit degil, sapmali.</summary>
    float NextWhineDelay()
    {
        return Mathf.Max(0.5f, whineInterval + Random.Range(-whineIntervalJitter, whineIntervalJitter));
    }

    void Update()
    {
        if (state == State.Stuck) UpdateWhine();
        if (state == State.Following && !isStomping) FollowUpdate();

        ApplyBodyAnimation();
    }

    /// <summary>
    /// Nefes alip verme + sevilince ezil-uza tepkisi.
    ///
    /// Sadece localScale'e dokunuyor. Konuma dokunmuyoruz cunku SnapToGround
    /// ve StompRoutine konumu yonetiyor — ikisi ayni seyi oynatirsa kopek titrer.
    /// </summary>
    void ApplyBodyAnimation()
    {
        float sx = 1f;
        float sy = 1f;

        if (breathAmount > 0f)
        {
            float b = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
            sx -= b * 0.5f;   // hacim korunsun: genisleyince alcalir
            sy += b;
        }

        if (petSquashDuration > 0f && Time.time < petSquashUntil)
        {
            // 0 -> 1 arasi ilerleme; sonuna dogru soner.
            float k = 1f - (petSquashUntil - Time.time) / petSquashDuration;
            float punch = Mathf.Sin(k * Mathf.PI * 2f) * petSquashAmount * (1f - k);
            sx += punch;
            sy -= punch;
        }

        transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
    }

    // ---- Stuck: uzaktan duyulan inleme ----

    void UpdateWhine()
    {
        if (player == null || audioSource == null || whineClip == null) return;

        whineTimer -= Time.deltaTime;
        if (whineTimer > 0f) return;
        whineTimer = NextWhineDelay();

        float dist = Vector3.Distance(player.position, transform.position);
        if (dist > maxHearDistance) return;

        // Yakinsa tam ses, uzaklastikca sifira dogru soner.
        // maxHearDistance ~32 (bir onceki oda) oldugu icin komsu odada
        // %20-30 seviyesinde, "var ama uzakta" hissi veriyor.
        float t = Mathf.Clamp01(Mathf.InverseLerp(maxHearDistance, minDistanceForMaxVolume, dist));

        // Neredeyse duyulmayacak kadar kisiksa hic calma — sesi bosa harcama.
        if (t < 0.05f) return;

        PlayVaried(whineClip, t);
    }

    /// <summary>
    /// Klibi her seferinde biraz farkli perdeyle calar.
    ///
    /// Tek bir kaydin ust uste ayni calmasi kulaga hemen "dosya" gibi
    /// geliyor — ozellikle sevme tusu gibi arka arkaya basilan seylerde.
    /// Kucuk bir perde sapmasi bunu neredeyse tamamen gizliyor.
    /// </summary>
    void PlayVaried(AudioClip clip, float volume = 1f)
    {
        if (audioSource == null || clip == null) return;

        audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        audioSource.PlayOneShot(clip, volume);
    }

    // ---- Following: basit takip + yoldaki atesi sondurme ----

    void FollowUpdate()
    {
        if (player == null) return;

        // Cok uzaklastiysa (genelde oyuncu ust kata cikti) yanina isin.
        //
        // ⚠️ Bu kontrol koruma anindan ONCE geliyor ve bilerek: oyuncu
        // canavari sondurmeden ilerlemeye devam ederse kopek sonsuza kadar
        // beklemesin, isinlanip yetissin. Aksi halde oyuncu farkinda olmadan
        // kopegi geride birakip finale kopeksiz varirdi.
        if (TryWarpToPlayer()) return;

        // Onunde canlı bir canavar varsa gecmiyor — oyuncunun sondurmesi lazim.
        if (stopAtMonsters && IsBlockedByMonster())
        {
            UpdateBlockedBark();
            SnapToGround(false);
            return;
        }

        float dist = Mathf.Abs(player.position.x - transform.position.x);

        if (dist > followDistance)
        {
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            Vector3 target = transform.position + Vector3.right * dir * dist;
            transform.position = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);

            if (sprite != null) sprite.flipX = dir < 0f;
        }

        // Yatay hareketten SONRA zemine otur — yoksa yokusta/basamakta
        // bir kare havada veya zeminin icinde gorunur.
        SnapToGround(false);

        CheckForFireToStomp();
    }

    /// <summary>
    /// Kopegin ILERISINDE canli bir duvar canavari var mi?
    ///
    /// Canavar oldugunde GameObject yok ediliyor, yani bileseni bulabiliyorsak
    /// hala hayattadir. Ayri bir "olu mu" bayragi okumaya gerek yok.
    /// </summary>
    bool IsBlockedByMonster()
    {
        if (player == null) return false;

        float dir = player.position.x > transform.position.x ? 1f : -1f;
        float half = monsterBlockRange * 0.5f;

        // Sadece ILERIYE bak: arkada kalan canavar kopegi durdurmamalı.
        // Canavarlar yuksekte (or. y=3.45) kopek zeminde (y=1) olabilecegi icin
        // arama merkezini biraz yukari (y+1.5) aliyoruz ki dikeydeki canavarlar da kapsama girsin.
        Vector2 center = (Vector2)transform.position + Vector2.up * 1.5f + Vector2.right * dir * half;

        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, half))
        {
            if (c.GetComponentInParent<WallMonster>() != null) return true;
        }

        return false;
    }

    /// <summary>Beklerken araliklarla havlar — "buraya gecemiyorum".</summary>
    void UpdateBlockedBark()
    {
        if (sprite != null && player != null)
            sprite.flipX = player.position.x < transform.position.x;

        blockedBarkTimer -= Time.deltaTime;
        if (blockedBarkTimer > 0f) return;

        blockedBarkTimer = blockedBarkInterval;
        PlayVaried(happyBarkClip, 0.75f);
    }

    /// <summary>
    /// Kopegi altindaki zeminin ustune oturtur.
    ///
    /// Rigidbody kullanmiyoruz: fizikli bir escort AI duvarlara sikisir,
    /// oyuncuyu iter ve jam'de saatlerce bug avina donusur. Isin atmak
    /// tahmin edilebilir ve yokusu/basamagi bedavaya cozer.
    /// </summary>
    void SnapToGround(bool instant)
    {
        Vector2 origin = new Vector2(transform.position.x, transform.position.y + groundProbeUp);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down,
                                             groundProbeUp + groundProbeDown, groundLayer);

        // Zemin bulunamadi (bosluk ustunde ya da groundLayer secilmemis) — dokunma.
        if (hit.collider == null) return;

        float targetY = hit.point.y + groundOffset;
        float y = instant
            ? targetY
            : Mathf.MoveTowards(transform.position.y, targetY, verticalSpeed * Time.deltaTime);

        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    /// <summary>
    /// Oyuncu cok uzaktaysa kopegi yanina tasir.
    ///
    /// Kat plani iki katli (y=0 ve y=9). Kopek merdiven tirmanamiyor; bu
    /// olmadan alt katta kalir, oyuncu onu bir daha goremez ve final
    /// sahnesinde ortada kopek olmaz. Escort AI'larda standart cozum.
    /// </summary>
    bool TryWarpToPlayer()
    {
        if (warpDistance <= 0f) return false;
        if (Vector2.Distance(player.position, transform.position) < warpDistance) return false;

        // Oyuncunun biraz gerisine birak, tam ustune degil.
        float side = sprite != null && sprite.flipX ? 1f : -1f;
        transform.position = new Vector3(player.position.x + side * followDistance,
                                         player.position.y,
                                         transform.position.z);

        SnapToGround(true);
        return true;
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
            PlayVaried(happyBarkClip);

        // Govde tepkisi — ses dosyasi olmasa bile bir sey oldugu belli olsun.
        petSquashUntil = Time.time + petSquashDuration;

        if (heartParticlePrefab != null)
        {
            Vector3 pos = heartSpawnPoint != null
                ? heartSpawnPoint.position
                : transform.position + Vector3.up * 0.7f;

            for (int i = 0; i < Mathf.Max(1, heartCount); i++)
            {
                GameObject heart = Instantiate(heartParticlePrefab, pos, Quaternion.identity);

                // Hepsi ayni anda cikarsa tek bir kalp gibi durur; kademeli
                // cikmalari icin omurlerini biraz kaydiriyoruz.
                HeartPop pop = heart.GetComponent<HeartPop>();
                if (pop != null)
                {
                    pop.riseSpeed *= Random.Range(0.85f, 1.25f);
                    pop.lifetime *= Random.Range(0.85f, 1.2f);
                }
            }
        }
    }

    /// <summary>Sikistigi yerden kurtulunca (oyuncu ustundeki atesi sondurunce) Kisi A tetikler.</summary>
    public void OnRescued()
    {
        if (state != State.Stuck) return;
        state = State.Freed;

        if (audioSource != null && happyBarkClip != null)
            PlayVaried(happyBarkClip);
    }

    /// <summary>Kurtarma animasyonu bitince Kisi A takibi baslatmak icin bunu cagirir.</summary>
    public void StartFollowing()
    {
        state = State.Following;
    }

    /// <summary>
    /// Belirtilen sure sonra takibi baslatir.
    ///
    /// ⚠️ Bekleme KOPEGIN uzerinde calisir, cagiranin uzerinde degil.
    /// Sebep: sikismis ates sonunce kendini SetActive(false) yapiyor ve Unity
    /// kapatilan bir objenin coroutine'lerini durduruyor. Beklemeyi orada
    /// yaparsak StartFollowing hic calismiyor — kopek kurtulup yerinde
    /// kaliyor. (Yasanmis bug, boyle bulundu.)
    /// </summary>
    public void StartFollowingAfter(float delay)
    {
        StartCoroutine(StartFollowingAfterRoutine(delay));
    }

    IEnumerator StartFollowingAfterRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartFollowing();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.9f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, fireDetectRange);
    }
}
