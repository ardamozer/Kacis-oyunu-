using System.Collections;
using UnityEngine;

/// <summary>
/// FINAL — kopegin olumu + boss sahnesi + hayalet. PLAN.md'nin "kesinlikle
/// kesilmeyecek" dedigi tek blok, saat 19.5 hedefiyle oncelikli.
///
/// Iki giris noktasi var (sozlesme, Kisi A cagirir):
///   TriggerDogDeath()  — oyuncu arkasindan gelen buyuk alevle kopek olur.
///   TriggerBossScene() — cikis koridorunda dev alev (boss) ortaya cikar.
///
/// Boss dovusu YOK (PLAN.md "Boss = dovus DEGIL, sahne"): can hic azalmaz,
/// oyuncunun canı 1 kalbe dusunce sahne KENDILIGINDEN tetiklenir.
/// </summary>
public class EndingSequence : MonoBehaviour
{
    [Header("Oyuncu")]
    [Tooltip("Bos birakin — Tag='Player' ile kendi bulur.")]
    public Transform player;

    [Header("Kopegin olumu")]
    [Tooltip("Sahnedeki Karabas. Olum aninda gizlenir.")]
    public DogCompanion dog;
    [Tooltip("Buyuk alevin cikacagi nokta — kopegin arkasi, oyuncunun gormedigi yer.")]
    public Transform dogDeathPoint;
    [Tooltip("Yukselip sonecek buyuk alev gorseli. Sahnede devre disi baslamali.")]
    public GameObject bigFlameVisual;
    [Tooltip("Alev sonunce yerde kalan tasma. Sahnede devre disi baslamali.")]
    public GameObject collarPickup;
    public float silenceDuration = 3f;
    public float flameRiseDuration = 1f;
    public float flameFallDuration = 1f;

    [Header("Olum — dusen tahta (tercih edilen yontem)")]
    [Tooltip("Karabas'in uzerine dusecek yanan tahta.\n" +
             "Atanirsa buyuyen alev yerine BU kullanilir.\n" +
             "Auto Trigger'i kod zaten kapatiyor — erken dusmez.")]
    public FallingDebris deathDebris;
    [Tooltip("Tahta dustukten sonra kopegin gizlenmesine kadar gecen sure. " +
             "Tahtanin yere varma suresine gore ayarlayin.")]
    public float debrisImpactDelay = 0.55f;

    [Header("Olum — Karabas'in hamlesi")]
    [Tooltip("Karabas oyuncuyu kac birim yana iter. 0 = itme yok.\n" +
             "Anlami: kopek seni kurtariyor. Mekanigi: dusen tahtanin " +
             "altindan cikiyorsun, tahta sana hasar vermiyor.")]
    public float pushDistance = 2f;
    public float pushDuration = 0.35f;
    [Tooltip("Kopegin oyuncunun yerine atilma suresi. Itmeyle ayni anda calisir.\n" +
             "Itmeden KISA olsun — once kopek carpsin, sonra oyuncu savrulsun.")]
    public float dogLungeDuration = 0.25f;

    [Header("Boss")]
    [Tooltip("BossCore tasiyan dev alev objesi. Sahnede devre disi baslamali.")]
    public GameObject bossObject;
    public Transform bossFirePoint;
    [Tooltip("WallMonster ile ayni FlameProjectile prefabi kullanilabilir.")]
    public GameObject flamePrefab;
    public float bossFireInterval = 2.5f;
    [Tooltip("Opsiyonel. Ghost sahnesi bitince gizlenir (= kapi acildi).")]
    public GameObject exitDoor;

    [Header("Hayalet")]
    [Tooltip("Kopek sprite'i mavi/beyaz tint + yari saydam. Sahnede devre disi baslamali.")]
    public GameObject ghostVisual;
    public Transform ghostStart;
    public Transform ghostEnd;
    public float ghostCrossDuration = 2.5f;
    public Color collarGlowColor = new Color(1f, 1f, 0.6f);

    bool dogDeathTriggered;
    bool bossActive;
    bool bossSceneEnding;
    float bossFireTimer;

    // Alevin sahnede ayarladiginiz gercek boyutu. Sabit Vector3.one KULLANMIYORUZ:
    // Inspector'da alevi buyutup kucultmekte ozgur olmalisiniz, kod onu ezmesin.
    Vector3 bigFlameBaseScale = Vector3.one;

    // Kopegin oldugu yer — tasma buraya birakilir.
    Vector3 dogLastPosition;

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (bossObject != null) bossObject.SetActive(false);

        if (bigFlameVisual != null)
        {
            bigFlameBaseScale = bigFlameVisual.transform.localScale;
            bigFlameVisual.SetActive(false);
        }

        if (collarPickup != null) collarPickup.SetActive(false);
        if (ghostVisual != null) ghostVisual.SetActive(false);
    }

    void Update()
    {
        if (bossActive && !bossSceneEnding) BossUpdate();
    }

    // ============================================================
    //  Sozlesme — Kisi A cagirir
    // ============================================================

    /// <summary>Oyuncunun arkasindan cikan buyuk alevle Karabas olur.</summary>
    public void TriggerDogDeath()
    {
        if (dogDeathTriggered) return;
        dogDeathTriggered = true;
        StartCoroutine(DogDeathRoutine());
    }

    /// <summary>Cikis koridorunda dev alev (boss) ortaya cikar, ates etmeye baslar.</summary>
    public void TriggerBossScene()
    {
        if (bossActive) return;
        bossActive = true;
        bossFireTimer = bossFireInterval * 0.5f; // ilk atesten once kisa bir nefes payi
        if (bossObject != null) bossObject.SetActive(true);
    }

    // ============================================================
    //  Kopegin olumu
    // ============================================================

    IEnumerator DogDeathRoutine()
    {
        SetPlayerControl(false);

        float previousVolume = AudioListener.volume;
        AudioListener.volume = 0f; // PLAN.md: "3 saniye, tum sesler kesilir"

        // ⚠️ SAHNENIN TAMAMI BU NOKTA UZERINE KURULU.
        // Tahta, oyuncunun TAM DURDUGU yere duser. Karabas oraya kosar,
        // oyuncu savrulur. Anlatilan cumle: "sana gelecek darbeyi o aldi."
        // Sabit bir olum noktasi kullanirsak bu cumle kurulmaz — oyuncu
        // sadece "kendiliginden kaydim, kopek uzakta oldu" gorur.
        Vector3 impactPoint = player != null ? player.position : transform.position;

        // Itme yonunu SIMDI hesapla: kopek birazdan hareket edecek, sonra
        // hesaplarsak yon yanlis cikar.
        float pushDir = 1f;
        if (dog != null && player != null)
            pushDir = player.position.x >= dog.transform.position.x ? 1f : -1f;

        // Kopek ve oyuncu AYNI ANDA hareket etmeli. Sirayla yaparsak
        // "itti" degil, "once kopek yurudu, sonra oyuncu kaydi" gorunur.
        Coroutine lunge = null;
        if (dog != null)
        {
            dog.enabled = false;   // kendi takip mantigi hamleyle yarismasin
            lunge = StartCoroutine(DogLungeRoutine(impactPoint));
        }

        yield return PushPlayerAside(pushDir);
        if (lunge != null) yield return lunge;

        if (deathDebris != null)
        {
            // Tahtayi carpma noktasinin USTUNE tasi — kopek artik orada.
            Vector3 d = deathDebris.transform.position;
            deathDebris.transform.position = new Vector3(impactPoint.x, d.y, d.z);

            // ⚠️ DropNow(false): UYARI YOK.
            // Oyun boyunca "unlem gorursen kac" ogretiliyor. Olum aninda unlem
            // cikarsa oyuncu "kacabilirdim, benim hatam" der; duygu suclulukdan
            // ofkeye doner ve final coker. Kacilamaz olmasi KASITLI.
            deathDebris.autoTrigger = false;   // oyuncu yaklasinca erken dusmesin
            deathDebris.DropNow(false);

            yield return new WaitForSeconds(debrisImpactDelay);
        }
        else if (bigFlameVisual != null)
        {
            // Yedek yol: sahnede tahta atanmadiysa eski buyuyen alev oynar.
            // Boylece tahta baglanmadan da sahne test edilebilir.
            if (dogDeathPoint != null) bigFlameVisual.transform.position = dogDeathPoint.position;
            bigFlameVisual.SetActive(true);
            yield return ScaleRoutine(bigFlameVisual.transform, Vector3.zero, bigFlameBaseScale, flameRiseDuration);
        }

        // Kopek gorunmez olur — kanli/grafik degil, sessiz ve yumusak (PLAN.md).
        // Tahta indikten SONRA gizleniyor ki carpma gorunur olsun.
        if (dog != null) dog.gameObject.SetActive(false);

        yield return new WaitForSeconds(silenceDuration);

        if (deathDebris == null && bigFlameVisual != null)
        {
            yield return ScaleRoutine(bigFlameVisual.transform, bigFlameBaseScale, Vector3.zero, flameFallDuration);
            bigFlameVisual.SetActive(false);

            // Sonraki acilista sifir boyutta kalmasin.
            bigFlameVisual.transform.localScale = bigFlameBaseScale;
        }

        // "Yerde sadece tasma kalir." — kopegin oldugu yerde, sabit bir
        // noktada degil. Tasma nerede kaldiysa oyuncu oraya bakar.
        if (collarPickup != null)
        {
            Vector3 collarPos = dogLastPosition;
            if (dog == null && dogDeathPoint != null) collarPos = dogDeathPoint.position;

            collarPickup.transform.position = collarPos;
            collarPickup.SetActive(true);
        }

        AudioListener.volume = previousVolume;
        SetPlayerControl(true);
    }

    /// <summary>
    /// Oyuncuyu kopekten UZAGA, yana kaydirir.
    ///
    /// Olcek animasyonu degil duz pozisyon lerp'i — "itildim" hissi
    /// dogrudan konum degisiminden geliyor.
    /// </summary>
    /// <summary>
    /// Karabas oyuncunun durdugu yere atilir. Tahta oraya dusecek.
    ///
    /// DogCompanion'i kapatiyoruz cunku kendi takip mantigi ayni transform'u
    /// oynatiyor ve hamleyle yarisip kopegi titretiyor.
    /// </summary>
    IEnumerator DogLungeRoutine(Vector3 impactPoint)
    {
        if (dog == null) yield break;

        Transform t = dog.transform;
        Vector3 from = t.position;
        // Sadece X: kopek zemininde kalsin, oyuncu havadaysa ucmasin.
        Vector3 to = new Vector3(impactPoint.x, from.y, from.z);

        SpriteRenderer sr = dog.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = to.x < from.x;

        float e = 0f;
        while (e < dogLungeDuration)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / dogLungeDuration);
            k = k * k * (3f - 2f * k);          // SmoothStep — atilis hissi
            t.position = Vector3.Lerp(from, to, k);
            yield return null;
        }

        t.position = to;
        dogLastPosition = to;                   // tasma buraya dusecek
    }

    IEnumerator PushPlayerAside(float dir)
    {
        if (player == null || pushDistance <= 0f || pushDuration <= 0f) yield break;

        float targetX = player.position.x + dir * pushDistance;

        // Kisi A'nin ForceMoveTo'su: SmoothStep'li ve sadece X'i oynatiyor,
        // dikey hizi fizige birakiyor. Kendi elle lerp'imiz oyuncu havadayken
        // yercekimini de iptal ediyordu — bu daha dogru.
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            yield return controller.ForceMoveTo(targetX, pushDuration);
            yield break;
        }

        // Yedek: PlayerController yoksa (test kuklasi) duz lerp.
        Vector3 from = player.position;
        float t = 0f;
        while (t < pushDuration)
        {
            t += Time.deltaTime;
            Vector3 p = player.position;
            p.x = Mathf.Lerp(from.x, targetX, t / pushDuration);
            player.position = p;
            yield return null;
        }
    }

    // ============================================================
    //  Boss sahnesi
    // ============================================================

    void BossUpdate()
    {
        bossFireTimer -= Time.deltaTime;
        if (bossFireTimer <= 0f)
        {
            ShootAtPlayer();
            bossFireTimer = bossFireInterval;
        }

        // Can hic azalmaz (boss tarafinda) — ama oyuncunun kendi cani azalir.
        // 1 kalbe dusunce sahne kendiliginden tetiklenir (PLAN.md madde 4).
        if (TryGetPlayerHealth(out int current) && current <= 1)
        {
            bossSceneEnding = true;
            StartCoroutine(GhostRoutine());
        }
    }

    void ShootAtPlayer()
    {
        if (flamePrefab == null || bossFirePoint == null || player == null) return;

        GameObject flame = Instantiate(flamePrefab, bossFirePoint.position, Quaternion.identity);
        FlameProjectile projectile = flame.GetComponent<FlameProjectile>();
        if (projectile != null)
        {
            float dir = player.position.x >= bossFirePoint.position.x ? 1f : -1f;
            projectile.Launch(new Vector2(dir, 0f));
        }
    }

    IEnumerator GhostRoutine()
    {
        SetPlayerControl(false);

        if (collarPickup != null) yield return CollarGlowRoutine();

        if (ghostVisual != null && ghostStart != null && ghostEnd != null)
        {
            ghostVisual.SetActive(true);
            ghostVisual.transform.position = ghostStart.position;

            float t = 0f;
            while (t < ghostCrossDuration)
            {
                t += Time.deltaTime;
                ghostVisual.transform.position = Vector3.Lerp(ghostStart.position, ghostEnd.position, t / ghostCrossDuration);
                yield return null;
            }

            ghostVisual.SetActive(false);
        }

        if (bossObject != null) bossObject.SetActive(false);
        bossActive = false;

        if (exitDoor != null) exitDoor.SetActive(false); // kapi acilir

        SetPlayerControl(true);
    }

    IEnumerator CollarGlowRoutine()
    {
        SpriteRenderer sprite = collarPickup.GetComponentInChildren<SpriteRenderer>();
        if (sprite == null) yield break;

        Color original = sprite.color;
        float t = 0f;
        float duration = 1.2f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.PingPong(t * 4f, 1f);
            sprite.color = Color.Lerp(original, collarGlowColor, k);
            yield return null;
        }

        sprite.color = original;
    }

    // ============================================================
    //  Yardimcilar
    // ============================================================

    // ScaleRoutine bu dosyanin sonunda — Kisi A yazdi (SmoothStep'li surum).

    void SetPlayerControl(bool canControl)
    {
        if (player == null) return;
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null) controller.CanControl = canControl;
    }

    /// <summary>
    /// PlayerHealth.CurrentHealth'i derleme bagimliligi kurmadan okur.
    ///
    /// NEDEN reflection? Sozlesmede "CurrentHealth { get; } // final sahnesi
    /// bunu okur" yaziyor — yani bu okuma tasarim geregi. Ama CurrentHealth bir
    /// DEGER DONDURUYOR; FlameProjectile.cs'teki SendMessage cozumu (cagri
    /// yapar, sonuc almaz) burada yetmiyor. GetComponent(string) + reflection,
    /// sinif yokken sessizce false doner, sinif gelince tek satir degismeden
    /// calisir. Once kendi test ikizimize (TestPlayerDummy) bakar.
    /// </summary>
    bool TryGetPlayerHealth(out int current)
    {
        current = 0;
        if (player == null) return false;

        TestPlayerDummy testDummy = player.GetComponent<TestPlayerDummy>();
        if (testDummy != null)
        {
            current = testDummy.CurrentHealth;
            return true;
        }

        Component healthComponent = player.GetComponent("PlayerHealth");
        if (healthComponent == null) return false;

        System.Reflection.PropertyInfo prop = healthComponent.GetType().GetProperty("CurrentHealth");
        if (prop == null) return false;

        current = (int)prop.GetValue(healthComponent);
        return true;
    }

    // ============================================================
    //  Yardimci
    //  NOT (Kisi A ekledi): Bu metod cagriliyordu ama yazilmamisti,
    //  proje derlenmiyordu ve ikimiz de Play'e basamiyorduk. Eksigi
    //  tamamladim. Farkli calismasini istersen serbestce degistir.
    // ============================================================

    /// <summary>
    /// Bir Transform'un olcegini verilen surede 'from'dan 'to'ya yumusak gecisle tasir.
    /// Alevin yukselmesi ve sonmesi icin kullaniliyor.
    /// </summary>
    IEnumerator ScaleRoutine(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null) yield break;

        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        target.localScale = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // SmoothStep: basta ve sonda yavas, ortada hizli. Lineerden cok daha iyi durur.
            t = t * t * (3f - 2f * t);
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.localScale = to;
    }
}
