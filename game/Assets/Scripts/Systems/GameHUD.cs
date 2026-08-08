using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekran arayuzu: su bari, kalpler ve hasar aninda ekran kenarindan giren alevler.
///
/// Alanlar ALEV → Test Sahnesini Kur ile otomatik baglanir, elle ugrasmayin.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Kaynaklar")]
    public WaterTank tank;
    public PlayerHealth health;

    [Header("Su Bari")]
    public Image waterFill;
    public Color waterNormalColor = new Color(0.25f, 0.65f, 1f);
    public Color waterLowColor = new Color(1f, 0.3f, 0.2f);

    [Header("Kalpler")]
    public Image[] hearts;
    public Color heartFullColor = new Color(0.95f, 0.25f, 0.3f);
    public Color heartEmptyColor = new Color(0.25f, 0.2f, 0.2f);

    [Header("Ekran Kenari Alev")]
    [Tooltip("Tam ekran vinyet. Hasar alinca yanip soner.")]
    public Image damageVignette;
    public Color vignetteColor = new Color(1f, 0.35f, 0.05f);

    void Update()
    {
        UpdateWater();
        UpdateHearts();
        UpdateVignette();
    }

    void UpdateWater()
    {
        if (tank == null || waterFill == null) return;

        waterFill.fillAmount = tank.Percent;

        if (tank.IsLow)
        {
            // Su azalinca kirmizi yanip sonsun — oyuncu fark etsin.
            float pulse = Mathf.PingPong(Time.time * 3f, 1f);
            waterFill.color = Color.Lerp(waterLowColor, waterNormalColor, pulse * 0.4f);
        }
        else
        {
            waterFill.color = waterNormalColor;
        }
    }

    void UpdateHearts()
    {
        if (health == null || hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            hearts[i].color = i < health.CurrentHealth ? heartFullColor : heartEmptyColor;
        }
    }

    void UpdateVignette()
    {
        if (health == null || damageVignette == null) return;

        Color c = vignetteColor;
        c.a = health.DamageFlash;          // hasarda 1, sonra soner
        damageVignette.color = c;
    }
}
