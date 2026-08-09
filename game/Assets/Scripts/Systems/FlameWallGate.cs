using UnityEngine;

/// <summary>
/// GERI DONUS KAPANIR — Karabas kurtulunca girisin onunde alev duvari yukselir.
///
/// PLAN.MD: "Artik geri donemezsin hissi soylenmemeli, GOSTERILMELI."
/// Oyuncu bu ani gormeli: kamera sarsilir, cokme sesi gelir, alev yukselir.
///
/// YENİ: Duvar yükseldikten sonra ILERLER — oyuncuyu kovalar!
/// Temas ederse hasar verir. "Acele et, arkandan ateş geliyor!"
///
/// ⚠️ Kisi B'nin DogCompanion API'sine BAGLANMIYOR. Kopegin baslangic
/// noktasindan uzaklasmasini izliyor — kurtulup pesinize takildiginin
/// en guvenilir isareti bu, ve hicbir kod bagimliligi kurmuyor.
/// </summary>
public class FlameWallGate : MonoBehaviour
{
    [Header("Ne zaman yukselsin")]
    [Tooltip("Izlenecek kopek. Bos birakilirsa sahnedeki DogCompanion'i bulur.")]
    public Transform dog;

    [Tooltip("Kopek baslangicindan bu kadar uzaklasinca kurtarilmis sayilir.")]
    public float dogMovedDistance = 3f;

    [Header("Yukselme")]
    public float riseHeight = 5f;
    public float riseDuration = 1.2f;

    [Header("Kovalama (Yukseldikten sonra)")]
    [Tooltip("Duvar yukseldikten sonra saga dogru ilerleme hizi (birim/saniye). " +
             "0 = ilerlemez, sadece yükselir.")]
    public float chaseSpeed = 1.5f;

    [Tooltip("Duvarin ilerlerken durma noktasi (x). Boss triggerine kadar " +
             "kovalamali, sonra durmali. 0 = sinir yok.")]
    public float maxChaseX = 300f;

    [Header("Hasar (Temas)")]
    [Tooltip("Oyuncu atese dokunursa alinan hasar.")]
    public int chaseDamage = 2;
    [Tooltip("Hasar alma araligi (saniye).")]
    public float damageInterval = 0.8f;

    [Header("Etki")]
    public float shakeDuration = 0.6f;
    public float shakeMagnitude = 0.45f;
    public AudioSource collapseSound;

    Vector3 dogStart;
    Vector3 hiddenPos, shownPos;
    bool triggered;
    bool riseComplete;
    float t;
    float nextDamageTime;

    Collider2D blockerCollider;
    Renderer[] childRenderers;
    MonoBehaviour[] childScripts;

    PlayerController playerController;

    /// <summary>
    /// Bir ara sahne oynuyor mu? Ara sahnelerde oyuncunun kontrolu aliniyor
    /// (`CanControl = false`), yani KACAMIYOR.
    ///
    /// Kopegin olumu ~4.35 saniye suruyor. Duvar bu sirada durmazsa 1.5 b/sn
    /// ile 6.5 birim ilerler ve 0.8 saniyede bir 2 hasar verir; oyuncunun cani
    /// 3. Yani oyunun duygusal zirvesinde, oyuncu tusa basamazken oldururdu.
    ///
    /// Tek bayrak butun ara sahneleri kapsiyor: olum, boss, hayalet.
    /// </summary>
    bool CutsceneActive => playerController != null && !playerController.CanControl;

    void Awake()
    {
        blockerCollider = GetComponent<Collider2D>();
        childRenderers = GetComponentsInChildren<Renderer>(true);
        childScripts = GetComponentsInChildren<MonoBehaviour>(true);
    }

    void Start()
    {
        if (dog == null)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "DogCompanion") { dog = mb.transform; break; }
        }

        if (dog != null) dogStart = dog.position;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) playerController = playerGO.GetComponent<PlayerController>();

        // Alev duvari zeminin ALTINDA bekler, tetiklenince yukselir.
        shownPos = transform.position;
        hiddenPos = shownPos + Vector3.down * (riseHeight + 3f);
        transform.position = hiddenPos;

        // Gizliyken collider ve gorseller KAPALI olmali.
        // Aksi halde gizli collider kapidan gecisi engeller,
        // gorseller de zeminin altindan sarkar.
        SetGateActive(false);
    }

    void SetGateActive(bool active)
    {
        if (blockerCollider != null) blockerCollider.enabled = active;

        if (childRenderers != null)
        {
            foreach (var r in childRenderers)
                if (r != null) r.enabled = active;
        }

        if (childScripts != null)
        {
            foreach (var s in childScripts)
                if (s != null && s != this) s.enabled = active;
        }
    }

    void Update()
    {
        if (!triggered)
        {
            if (dog == null)
            {
                foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if (mb.GetType().Name == "DogCompanion") { dog = mb.transform; break; }
                if (dog != null) dogStart = dog.position;
                return;
            }

            // Kopegin bilesenini bul. Kopek henuz KURTARILMADIP (Stuck durumundayken)
            // alev duvari ASLA yukselmez! Oyuncu Mor Kapi'dan girip kopegi kurtarmadan
            // once odada serbestce gezebilmeli ve geriye donebilmeli.
            var dogComp = dog.GetComponent<DogCompanion>();
            if (dogComp != null && !dogComp.IsRescued) return;

            // Kopek kurtarildiktan sonra yuvasindan uzaklasinca alev duvari yukselir.
            if (dogStart == Vector3.zero) dogStart = dog.position;
            if (Vector3.Distance(dog.position, dogStart) < dogMovedDistance) return;

            triggered = true;
            SetGateActive(true);

            if (CameraShake.Instance != null)
                CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
            if (collapseSound != null) collapseSound.Play();

            Debug.Log("[FlameWallGate] Karabas kurtarildi, geri donus kapandi. " +
                      "Ates duvari kovalamaya basliyor!");
            return;
        }

        // ---- Yukselme fazı ----
        if (!riseComplete)
        {
            if (t >= riseDuration)
            {
                riseComplete = true;
                transform.position = shownPos;
                return;
            }

            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / riseDuration);
            k = k * k * (3f - 2f * k);
            transform.position = Vector3.Lerp(hiddenPos, shownPos, k);
            return;
        }

        // ---- Kovalama fazı: saga dogru ilerle ----
        // Ara sahnede DUR. Oyuncu kacamazken yaklasmaya devam etmesi haksizlik.
        if (CutsceneActive) return;

        if (chaseSpeed > 0f)
        {
            if (maxChaseX > 0f && transform.position.x >= maxChaseX) return;

            transform.position += Vector3.right * chaseSpeed * Time.deltaTime;
        }
    }

    // ---- Temas hasarı ----
    void OnTriggerStay2D(Collider2D other)
    {
        if (!triggered || !riseComplete) return;
        if (!other.CompareTag("Player")) return;

        // Ara sahnede hasar da YOK — durmus bir duvarin icinde kalmis olsa bile
        // oyuncu kacamiyor. Sahne bitip kontrol geri gelince tehdit geri doner.
        if (CutsceneActive) return;

        if (Time.time < nextDamageTime) return;

        nextDamageTime = Time.time + damageInterval;

        // PlayerHealth doğrudan erişim yerine SendMessage kullanıyoruz
        // böylece Kisi A'nin koduna derleme bağımlılığı kurmuyoruz.
        other.SendMessageUpwards("TakeDamage", chaseDamage,
            SendMessageOptions.DontRequireReceiver);

        Debug.Log("[FlameWallGate] Oyuncuya ates duvari hasari verildi!");
    }
}
