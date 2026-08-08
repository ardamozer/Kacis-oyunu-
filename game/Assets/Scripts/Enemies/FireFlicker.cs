using UnityEngine;

/// <summary>
/// ATES TITREMESI — herhangi bir ates objesine takilabilen kucuk cila.
///
/// Neden var: elimizde cizilmis ates animasyonu yok, her sey duz renkli kare.
/// Sabit duran bir kare "ates" gibi okunmuyor. Olcegi ve parlakligi surekli
/// oynatmak, tek bir asset olmadan hareketli ates hissi veriyor —
/// ASSETS.md'nin "olcek animasyonu insan gozune cok ikna edici gelir" notu.
///
/// Perlin gurultusu kullaniyoruz, sinus degil: dogal ates duzenli yanip
/// sonmez. Duzenli olursa "ates" degil "yanip sonen lamba" gibi durur.
///
/// ⚠️ BASKASININ OLCEGINI EZMEZ. Her karede sadece kendi yazdigi degeri
/// bekler; degeri baskasi degistirmisse (FloorFire.Grow, sondurme
/// animasyonu) onu yeni taban kabul eder. Boylece buyume ve sonme
/// animasyonlari bozulmadan calisir.
/// </summary>
public class FireFlicker : MonoBehaviour
{
    [Header("Olcek")]
    [Tooltip("Olcek ne kadar oynasin. 0.06 = %6 nefes alip verme.")]
    public float scaleAmount = 0.06f;
    [Tooltip("Titremenin hizi.")]
    public float scaleSpeed = 7f;
    [Tooltip("Dikey titreme yatayin kac kati olsun. Alev yukari dogru uzar.")]
    public float verticalBias = 1.8f;

    [Header("Parlaklik")]
    [Tooltip("Renk ne kadar acilip kararsin. 0 = kapali.")]
    public float brightnessAmount = 0.12f;
    public float brightnessSpeed = 11f;

    SpriteRenderer sprite;
    Color baseColor;

    Vector3 baseScale;
    Vector3 lastApplied;
    bool hasApplied;

    Color lastAppliedColor;
    bool hasAppliedColor;

    // Her ates ayni anda titremesin diye objeye ozel rastgele faz.
    float seed;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;

        baseScale = transform.localScale;
        seed = Random.value * 100f;
    }

    void LateUpdate()
    {
        // Olcegi baskasi degistirdiyse (buyume / sonme) yeni tabani kabul et.
        // Karsilastirma tam olarak GECEN KARE YAZDIGIMIZ degere karsi —
        // bu yuzden "kim degistirdi" sorusu net cevaplaniyor.
        if (hasApplied && (transform.localScale - lastApplied).sqrMagnitude > 1e-6f)
            baseScale = transform.localScale;

        float n = (Mathf.PerlinNoise(seed + Time.time * scaleSpeed, seed) - 0.5f) * 2f;

        Vector3 s = new Vector3(
            baseScale.x * (1f + n * scaleAmount),
            baseScale.y * (1f + n * scaleAmount * verticalBias),
            baseScale.z);

        transform.localScale = s;
        lastApplied = s;
        hasApplied = true;

        if (sprite != null && brightnessAmount > 0f)
        {
            // Rengi baskasi degistirdiyse yeni tabani kabul et. Bu sart:
            //   FloorFire.Grow      -> grownColor'a boyuyor
            //   WallMonster/Trapped -> su degince beyaza calıyor
            // Bu kontrol olmadan titreme o renkleri her karede geri ezerdi.
            if (hasAppliedColor && !Approximately(sprite.color, lastAppliedColor))
                baseColor = sprite.color;

            float b = (Mathf.PerlinNoise(seed + Time.time * brightnessSpeed, 0f) - 0.5f) * 2f;

            // Rengin ALFASINA dokunmuyoruz: sonme animasyonlari alfayi
            // kullaniyor, ezersek ates hic kaybolmaz.
            Color c = new Color(
                Mathf.Clamp01(baseColor.r + b * brightnessAmount),
                Mathf.Clamp01(baseColor.g + b * brightnessAmount),
                Mathf.Clamp01(baseColor.b + b * brightnessAmount),
                sprite.color.a);

            sprite.color = c;
            lastAppliedColor = c;
            hasAppliedColor = true;
        }
    }

    static bool Approximately(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.002f
            && Mathf.Abs(a.g - b.g) < 0.002f
            && Mathf.Abs(a.b - b.b) < 0.002f;
    }
}
