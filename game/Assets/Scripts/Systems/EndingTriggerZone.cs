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

    [Header("Onkosullar — SIRADISILIK KORUMASI")]
    [Tooltip("Oyuncunun en az bu kadar anahtari olmali. Kacis yoluna erken " +
             "ulasan oyuncu final sahnesini yanlis sirada tetiklemesin.")]
    public int requiredKeyCount = 0;

    [Tooltip("Bu bolge tetiklenmeden burasi calismaz. Ornek: boss sahnesi, " +
             "kopegin olumunu bekler.")]
    public EndingTriggerZone requires;

    /// <summary>Bu bolge tetiklendi mi? Baska bolgeler onkosul olarak okur.</summary>
    public bool HasFired => fired;

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

        // ---- Siradisilik korumasi ----
        // Oyuncu beklenmedik bir yoldan buraya ulastiysa sahneyi tetiklemesin.
        // Sessizce gecmek, oyunu yanlis sirada kirmaktan iyidir.
        if (requires != null && !requires.HasFired)
        {
            Debug.Log($"[EndingTriggerZone] {name}: onkosul saglanmadi " +
                      $"({requires.name} henuz tetiklenmedi), atlandi.");
            return;
        }

        if (requiredKeyCount > 0)
        {
            var inv = other.GetComponentInParent<KeyInventory>();
            if (inv == null || inv.Count < requiredKeyCount)
            {
                Debug.Log($"[EndingTriggerZone] {name}: yeterli anahtar yok " +
                          $"({inv?.Count ?? 0}/{requiredKeyCount}), atlandi.");
                return;
            }
        }

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
