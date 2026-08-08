using System.Collections;
using UnityEngine;

/// <summary>
/// Kilitli kapi. Dogru anahtari tasiyan oyuncu degince acilir.
///
/// ⚠️ EN ONEMLI DETAY: anahtari OLMAYAN oyuncuya da geri bildirim vermeli.
/// Sessizce duran bir kapi, oyuncuya "burasi duvar" der ve geri doner.
/// Sarsilma + kilit sesi + kisa bir renk parlamasi, "kilitli, anahtar lazim"
/// mesajini kelime kullanmadan verir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LockedDoor : MonoBehaviour
{
    [Header("Kilit")]
    public KeyColor requiredKey = KeyColor.Mavi;

    [Tooltip("Kapiyi engelleyen KATI collider. Trigger olan ayri.")]
    public Collider2D blocker;

    [Header("Acilma")]
    [Tooltip("Kapi yukari kayarak acilir.")]
    public float openHeight = 3.4f;
    public float openDuration = 0.6f;

    [Header("Kilitli geri bildirimi")]
    public float shakeDuration = 0.25f;
    public float shakeMagnitude = 0.12f;
    public float feedbackCooldown = 0.8f;

    [Header("Ses")]
    public AudioSource openSound;
    public AudioSource lockedSound;

    SpriteRenderer sprite;
    Vector3 basePos;
    Color baseColor;
    bool opened;
    float lastFeedback = -99f;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        basePos = transform.position;

        if (sprite != null)
        {
            // Kapi hangi anahtari istiyorsa o renge boyanir — oyuncu
            // anahtari alirken "bu su kapiyi acacak" diye baglanti kurar.
            Color k = KeyColors.Of(requiredKey);
            baseColor = Color.Lerp(sprite.color, k, 0.55f);
            sprite.color = baseColor;
        }
    }

    void OnTriggerEnter2D(Collider2D other) => TryOpen(other);
    void OnTriggerStay2D(Collider2D other) => TryOpen(other);

    void TryOpen(Collider2D other)
    {
        if (opened) return;

        var inv = other.GetComponentInParent<KeyInventory>();
        if (inv == null) return;

        if (inv.Has(requiredKey))
        {
            opened = true;
            StartCoroutine(OpenRoutine());
        }
        else
        {
            if (Time.time - lastFeedback < feedbackCooldown) return;
            lastFeedback = Time.time;
            StartCoroutine(LockedRoutine());
        }
    }

    IEnumerator OpenRoutine()
    {
        if (openSound != null) openSound.Play();
        if (blocker != null) blocker.enabled = false;

        Vector3 from = transform.position;
        Vector3 to = from + Vector3.up * openHeight;

        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / openDuration);
            k = k * k * (3f - 2f * k);
            transform.position = Vector3.Lerp(from, to, k);
            yield return null;
        }

        transform.position = to;
        // Not: tetikleyici collider'i kapatmiyoruz — objede birden fazla
        // collider var (kati engel + tetikleyici) ve yanlisini kapatma riski
        // var. 'opened' bayragi zaten tekrar calismasini engelliyor.
    }

    IEnumerator LockedRoutine()
    {
        if (lockedSound != null) lockedSound.Play();

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float fade = 1f - t / shakeDuration;

            transform.position = basePos +
                (Vector3)(Random.insideUnitCircle * shakeMagnitude * fade);

            if (sprite != null)
                sprite.color = Color.Lerp(baseColor, KeyColors.Of(requiredKey), fade);

            yield return null;
        }

        transform.position = basePos;
        if (sprite != null) sprite.color = baseColor;
    }
}
