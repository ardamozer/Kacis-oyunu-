using UnityEngine;

/// <summary>
/// Itfaiyecinin sirtindaki su deposu. Su harcamayi ve doldurmayi yonetir.
///
/// ⚠️ TASARIM KURALI — kilitlenme onleme:
/// Su bitmesi "zorlasti" demek olmali, ASLA "oyun bitti" olmamali.
/// Musluk ve kuvet sonsuz kullanilabilir, her odada erisilebilir bir tane olsun.
/// Yer atesleri de zaten susuz (ziplayarak) oldurulebiliyor.
/// </summary>
public class WaterTank : MonoBehaviour
{
    [Header("Kapasite")]
    public float maxWater = 100f;

    [Tooltip("Oyuna kac su ile baslanacak.")]
    public float startWater = 100f;

    [Header("Uyari Esigi")]
    [Tooltip("Bu yuzdenin altinda UI kirmizi yanip soner. 0.25 = %25")]
    [Range(0f, 1f)] public float lowWarningPercent = 0.25f;

    [Header("Canli Durum (sadece izlemek icin)")]
    [Tooltip("Play modunda burayi izleyerek suyun harcanip dolmasini gorebilirsiniz.")]
    [SerializeField] float current;

    /// <summary>Su anki su miktari.</summary>
    public float Current
    {
        get => current;
        private set => current = value;
    }

    /// <summary>Maksimum kapasite.</summary>
    public float Max => maxWater;

    /// <summary>0-1 arasi doluluk orani. UI barinin fillAmount'i icin.</summary>
    public float Percent => maxWater <= 0f ? 0f : Current / maxWater;

    /// <summary>Su kritik seviyede mi? UI kirmizi yanip sonme icin okur.</summary>
    public bool IsLow => Percent <= lowWarningPercent;

    /// <summary>Depo tamamen dolu mu? WaterSource gereksiz doldurmasin diye.</summary>
    public bool IsFull => Current >= maxWater - 0.01f;

    void Awake()
    {
        Current = Mathf.Clamp(startWater, 0f, maxWater);
    }

    /// <summary>
    /// Su harcamayi dener. Yeterli su varsa harcar ve true doner.
    /// Yetmiyorsa hic harcamaz ve false doner.
    ///
    /// WaterGun her karede bunu cagirir; false donerse su sikmayi durdurur.
    /// </summary>
    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (Current < amount) return false;

        Current -= amount;
        return true;
    }

    /// <summary>Depoyu doldurur. Kapasiteyi asamaz.</summary>
    public void Refill(float amount)
    {
        if (amount <= 0f) return;
        Current = Mathf.Min(Current + amount, maxWater);
    }

    /// <summary>Depoyu tamamen doldurur (akvaryum gibi tek kullanimlik kaynaklar icin).</summary>
    public void RefillFull()
    {
        Current = maxWater;
    }
}
