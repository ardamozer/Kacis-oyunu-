using UnityEngine;

/// <summary>
/// GERI DONUS KAPANIR — Karabas kurtulunca girisin onunde alev duvari yukselir.
///
/// PLAN.md: "Artik geri donemezsin hissi soylenmemeli, GOSTERILMELI."
/// Oyuncu bu ani gormeli: kamera sarsilir, cokme sesi gelir, alev yukselir.
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

    [Header("Etki")]
    public float shakeDuration = 0.6f;
    public float shakeMagnitude = 0.45f;
    public AudioSource collapseSound;

    Vector3 dogStart;
    Vector3 hiddenPos, shownPos;
    bool triggered;
    float t;

    Collider2D blockerCollider;
    Renderer[] childRenderers;
    MonoBehaviour[] childScripts;

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

            Debug.Log("[FlameWallGate] Karabas kurtarildi, geri donus kapandi.");
            return;
        }

        if (t >= riseDuration) return;

        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / riseDuration);
        k = k * k * (3f - 2f * k);
        transform.position = Vector3.Lerp(hiddenPos, shownPos, k);
    }
}
