using UnityEngine;

/// <summary>
/// Seviyeye yerlestirilen tetikleyici bolge. Oyuncu icine girince
/// Kisi B'nin EndingSequence'indeki ilgili sahneyi baslatir.
///
/// Sahnede iki tane olacak:
///   1. Kopegin olumu   → oyuncunun ARKASINDA, gormedigi yerde tetiklenmeli
///   2. Boss sahnesi    → cikis koridorunun girisinde
///
/// Kurulum: bos GameObject + Collider2D (Is Trigger ✅) + bu script.
/// Ending alanini bos birakabilirsiniz, sahnedeki EndingSequence'i kendi bulur.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EndingTriggerZone : MonoBehaviour
{
    public enum What
    {
        DogDeath,     // Karabas'in olumu
        BossScene     // cikistaki dev alev
    }

    [Header("Ne tetiklenecek")]
    public What trigger = What.DogDeath;

    [Tooltip("Bos birakin — sahnedeki EndingSequence'i kendi bulur.")]
    public EndingSequence ending;

    [Tooltip("Bir kere tetiklendikten sonra devre disi kalsin. Acik birakin.")]
    public bool oneShot = true;

    bool fired;

    void Reset()
    {
        // Yeni eklendiginde trigger olsun — sik yapilan hatayi onler.
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        if (ending == null) ending = FindFirstObjectByType<EndingSequence>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (fired && oneShot) return;
        if (!other.CompareTag("Player")) return;

        if (ending == null)
        {
            Debug.LogWarning($"[EndingTriggerZone] {name}: sahnede EndingSequence yok, " +
                             "tetikleme atlandi.");
            return;
        }

        fired = true;

        switch (trigger)
        {
            case What.DogDeath:
                Debug.Log("[EndingTriggerZone] Kopegin olumu tetiklendi.");
                ending.TriggerDogDeath();
                break;

            case What.BossScene:
                Debug.Log("[EndingTriggerZone] Boss sahnesi tetiklendi.");
                ending.TriggerBossScene();
                break;
        }
    }

    void OnDrawGizmos()
    {
        // Sahne penceresinde bolgeyi gorunur yapar — yerlestirmeyi kolaylastirir.
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = trigger == What.DogDeath
            ? new Color(1f, 0.3f, 0.3f, 0.25f)
            : new Color(1f, 0.6f, 0.1f, 0.25f);

        Bounds b = col.bounds;
        Gizmos.DrawCube(b.center, b.size);
    }
}
