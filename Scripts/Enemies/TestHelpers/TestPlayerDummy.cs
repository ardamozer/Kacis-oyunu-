using UnityEngine;

/// <summary>
/// ⚠️ SADECE TEST ICIN — Kisi B'nin TestScene_B sahnesinde kullanilir.
///
/// Kisi A'nin PlayerHealth sinifi henuz yok. Alevin gercekten hasar verdigini
/// gorebilmek icin gecici bir alici.
///
/// FlameProjectile "TakeDamage" mesajini SendMessage ile yolluyor; bu sinifta da
/// ayni isimde bir metod oldugu icin mesaj buraya duser ve Console'a yazar.
///
/// Sinif adi bilerek "TestPlayerDummy" — A'nin "PlayerHealth" siniftiyla ASLA
/// cakismaz, ayni anda ikisi de projede durabilir. A'nin dosyasi gelince
/// bu script'i Player objesinden cikarmak yeterli, hicbir kodu degistirmem gerekmez.
/// </summary>
public class TestPlayerDummy : MonoBehaviour
{
    [Tooltip("PLAN.md: oyuncunun 3 kalbi var.")]
    public int health = 3;

    [Tooltip("Hasar alinca kisa bir dokunulmazlik — tek alevden ust uste hasar almasin.")]
    public float invulnerableTime = 0.8f;

    float invulnerableUntil;

    /// <summary>
    /// Imza Kisi A'nin PlayerHealth.CurrentHealth ile ayni — EndingSequence
    /// boss sahnesinde "can 1'e dustu mu" kontrolunu bunun uzerinden test eder.
    /// </summary>
    public int CurrentHealth => health;

    /// <summary>Imza Kisi A'nin PlayerHealth.TakeDamage(int) ile ayni olmali.</summary>
    public void TakeDamage(int amount)
    {
        if (Time.time < invulnerableUntil)
        {
            Debug.Log("[TEST] Dokunulmazlik acik, hasar yok.");
            return;
        }

        invulnerableUntil = Time.time + invulnerableTime;
        health -= amount;

        Debug.Log($"[TEST] Hasar aldim: {amount}. Kalan can: {health}");

        if (health <= 0) Debug.Log("[TEST] OLDUM. (Gercek olum ekrani Kisi A'da)");
    }
}
