using UnityEngine;

/// <summary>
/// Su doldurma noktasi. Oyuncu icine girince deposunu doldurur.
///
/// Iki tip var:
///  - Infinite (musluk, kuvet): yaninda durdukca yavas dolar, SONSUZ kullanilir.
///  - OneShot  (akvaryum, bidon): aninda buyuk dolum, tek kullanimlik.
///
/// ⚠️ SEVIYE TASARIMI KURALI:
/// Her odada erisilebilir EN AZ BIR Infinite kaynak olmali.
/// Yoksa oyuncu susuz kalip kilitlenebilir. OneShot'lar sadece bonus.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaterSource : MonoBehaviour
{
    public enum SourceType
    {
        Infinite,   // musluk, kuvet — tekrar tekrar kullanilir
        OneShot     // akvaryum, bidon — bir kere
    }

    [Header("Tip")]
    public SourceType type = SourceType.Infinite;

    [Header("Infinite ayarlari")]
    [Tooltip("Saniyede doldurulan su. Musluk 30, kuvet 50 onerilir.")]
    public float refillPerSecond = 30f;

    [Header("OneShot ayarlari")]
    [Tooltip("Aninda verilen su. Akvaryum 100, bidon 50 onerilir.")]
    public float instantAmount = 100f;

    [Tooltip("Kullanildiktan sonra objeyi yok et. Kapaliysa sadece gorseli degisir.")]
    public bool destroyAfterUse = true;

    [Header("Gorsel / Ses (opsiyonel)")]
    public GameObject fullVisual;      // kullanilmadan onceki hali (dolu akvaryum)
    public GameObject emptyVisual;     // kullanildiktan sonraki hali (kirik akvaryum)
    public ParticleSystem refillEffect;
    public AudioSource refillSound;

    bool used;

    void Reset()
    {
        // Yeni eklendiginde collider otomatik trigger olsun — sik yapilan hatayi onler.
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        SetVisual(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (type != SourceType.OneShot) return;

        var tank = other.GetComponentInParent<WaterTank>();
        if (tank == null || used) return;

        // Depo zaten doluysa harcama — oyuncu geri gelip kullanabilsin.
        if (tank.IsFull) return;

        tank.Refill(instantAmount);
        used = true;

        PlayFeedback();
        SetVisual(true);

        if (destroyAfterUse) Destroy(gameObject, 0.1f);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (type != SourceType.Infinite) return;

        var tank = other.GetComponentInParent<WaterTank>();
        if (tank == null || tank.IsFull)
        {
            StopFeedback();
            return;
        }

        tank.Refill(refillPerSecond * Time.deltaTime);
        PlayFeedback();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<WaterTank>() != null) StopFeedback();
    }

    void SetVisual(bool isUsed)
    {
        if (fullVisual != null) fullVisual.SetActive(!isUsed);
        if (emptyVisual != null) emptyVisual.SetActive(isUsed);
    }

    void PlayFeedback()
    {
        if (refillEffect != null && !refillEffect.isEmitting) refillEffect.Play();
        if (refillSound != null && !refillSound.isPlaying) refillSound.Play();
    }

    void StopFeedback()
    {
        if (refillEffect != null && refillEffect.isEmitting) refillEffect.Stop();
        if (refillSound != null && refillSound.isPlaying) refillSound.Stop();
    }
}
