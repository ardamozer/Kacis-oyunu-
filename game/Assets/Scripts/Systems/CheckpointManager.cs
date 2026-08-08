using UnityEngine;

/// <summary>
/// CHECKPOINT SISTEMI — olunce odanin basinda devam et.
///
/// ⚠️ NEDEN VAR (en buyuk kullanici deneyimi sorunuydu):
/// PlayerHealth olunce SceneManager.LoadScene ile sahneyi bastan
/// yukluyordu. Harita 336 birim; boss'ta (x=328) olen oyuncu 5-8
/// dakikalik ilerlemesini kaybedip x=6'dan basliyordu. Jam oyununda
/// oyuncu iki dakika sabir tanir — bir kez olup bastan baslayan
/// cogu kisi ikinci denemede kapatir.
///
/// ⚠️ SAHNEYI YENIDEN YUKLEMIYORUZ, bilerek.
/// Yukleseydik alinan anahtarlar ve sondurulmus atesler de sifirlanirdi.
/// Oyuncu 6. odada olup 6. odada dogsaydi ama anahtarlar 2. odaya geri
/// donseydi, kilitli kapinin onunde anahtarsiz kalir ve oyun
/// COZULEMEZ hale gelirdi. Sadece oyuncuyu tasiyip canini dolduruyoruz;
/// dunyanin geri kalani oldugu gibi kaliyor.
///
/// Kullanim: sahnedeki her odanin girisine bir Checkpoint trigger'i.
/// Otomatik yerlestirmek icin: ALEV → Checkpoint'leri Odalara Kur
/// </summary>
public static class CheckpointManager
{
    /// <summary>Son dokunulan checkpoint. Hic yoksa kullanilmaz.</summary>
    public static Vector3 Current { get; private set; }

    /// <summary>Hic checkpoint'e dokunuldu mu?</summary>
    public static bool HasCheckpoint { get; private set; }

    /// <summary>Son checkpoint'in adi — Console'da takip icin.</summary>
    public static string CurrentName { get; private set; }

    public static void Set(Vector3 position, string name)
    {
        // Geri gitmek checkpoint'i geri almasin: oyuncu 5. odadan 3. odaya
        // donerse ve orada olurse, yine 5. odada devam etmeli.
        if (HasCheckpoint && position.x <= Current.x) return;

        Current = position;
        CurrentName = name;
        HasCheckpoint = true;

        Debug.Log($"[Checkpoint] {name} (x={position.x:0.#})");
    }

    /// <summary>
    /// PlayerHealth olum aninda cagirir.
    /// true dondururse sahne YENIDEN YUKLENMEZ.
    /// </summary>
    public static bool TryRespawn(PlayerHealth health)
    {
        if (!HasCheckpoint || health == null) return false;

        Transform t = health.transform;

        t.position = Current;

        // Hizi sifirla — olum anindaki dususle checkpoint'ten firlamasin.
        Rigidbody2D rb = health.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        health.Revive();

        Debug.Log($"[Checkpoint] {CurrentName} noktasinda devam ediliyor.");
        return true;
    }

    /// <summary>
    /// Sahne gercekten bastan basladiginda (Play'e yeniden basinca)
    /// static alanlar Editor'de HAYATTA KALIYOR. Sifirlanmazsa ikinci
    /// oynayista oyuncu doğrudan son checkpoint'te basliyor.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        HasCheckpoint = false;
        Current = Vector3.zero;
        CurrentName = null;
    }
}
