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

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (bossObject != null) bossObject.SetActive(false);
        if (bigFlameVisual != null) bigFlameVisual.SetActive(false);
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

        // Kopek gorunmez olur — kanli/grafik degil, sessiz ve yumusak (PLAN.md).
        if (dog != null) dog.gameObject.SetActive(false);

        if (bigFlameVisual != null)
        {
            if (dogDeathPoint != null) bigFlameVisual.transform.position = dogDeathPoint.position;
            bigFlameVisual.SetActive(true);
            yield return ScaleRoutine(bigFlameVisual.transform, Vector3.zero, Vector3.one, flameRiseDuration);
        }

        yield return new WaitForSeconds(silenceDuration);

        if (bigFlameVisual != null)
        {
            yield return ScaleRoutine(bigFlameVisual.transform, bigFlameVisual.transform.localScale, Vector3.zero, flameFallDuration);
            bigFlameVisual.SetActive(false);
        }

        // "Yerde sadece tasma kalir."
        if (collarPickup != null)
        {
            if (dogDeathPoint != null) collarPickup.transform.position = dogDeathPoint.position;
            collarPickup.SetActive(true);
        }

        AudioListener.volume = previousVolume;
        SetPlayerControl(true);
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
