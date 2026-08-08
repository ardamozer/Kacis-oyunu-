using UnityEngine;

/// <summary>
/// Dekoratif yangin. Hasar VERMEZ, sondurulemez, oynanisa dokunmaz —
/// tek isi evin yaniyor gormesini saglamak.
///
/// Neden ayri bir script: oyunun en buyuk gorsel eksigi "ev yanmiyor gibi
/// duruyor"du. Gercek yangin sistemi (FloorFire) oynanis nesnesi, her yere
/// serpistirilemez. Bu ise bedava — sadece titreyen bir sprite ve bir isik
/// halkasi.
///
/// Titreme uc katmanli: olcek, parlaklik ve isik halkasi ayri ayri ve
/// FARKLI hizlarda oynuyor. Tek bir sinus egrisiyle yanip sonen ates
/// mekanik durur; katmanlar ust uste binince organik gorunur.
/// </summary>
public class AmbientFire : MonoBehaviour
{
    [Header("Titreme")]
    [Tooltip("Olcek ne kadar oynasin. 0.15 = %15.")]
    public float scaleJitter = 0.18f;

    [Tooltip("Parlaklik ne kadar oynasin.")]
    public float alphaJitter = 0.25f;

    [Tooltip("Titreme hizi. Her ates biraz farkli olsun diye rastgeleniyor.")]
    public float speed = 6f;

    [Header("Isik halkasi")]
    [Tooltip("Atesin etrafindaki turuncu parilti. Opsiyonel ama en cok " +
             "fark yaratan sey bu — karanlik odada ates isik veriyor gibi durur.")]
    public SpriteRenderer glow;

    public float glowScaleJitter = 0.25f;
    public float glowBaseAlpha = 0.35f;

    SpriteRenderer sprite;
    Vector3 baseScale;
    Vector3 glowBaseScale;
    float baseAlpha;

    // Her ates kendi fazinda baslasin, hepsi ayni anda yanip sonmesin
    float seedA, seedB, seedC;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseAlpha = sprite != null ? sprite.color.a : 1f;

        if (glow != null) glowBaseScale = glow.transform.localScale;

        seedA = Random.value * 100f;
        seedB = Random.value * 100f;
        seedC = Random.value * 100f;

        // Hizi da biraz rastgelele — komsu atesler senkron oynamasin
        speed *= Random.Range(0.8f, 1.25f);
    }

    void Update()
    {
        float t = Time.time * speed;

        // Uc ayri egri, farkli hizlarda. Tek sinus mekanik durur.
        float wobble = (Mathf.Sin(t + seedA) * 0.6f + Mathf.Sin(t * 1.7f + seedB) * 0.4f);

        if (sprite != null)
        {
            float s = 1f + wobble * scaleJitter;
            transform.localScale = new Vector3(baseScale.x * (1f + wobble * scaleJitter * 0.4f),
                                               baseScale.y * s,
                                               baseScale.z);

            Color c = sprite.color;
            c.a = Mathf.Clamp01(baseAlpha + wobble * alphaJitter);
            sprite.color = c;
        }

        if (glow != null)
        {
            // Isik halkasi atesten YAVAS oynasin — birebir takip ederse
            // yapistirilmis gibi durur.
            float g = 1f + Mathf.Sin(t * 0.55f + seedC) * glowScaleJitter;
            glow.transform.localScale = glowBaseScale * g;

            Color gc = glow.color;
            gc.a = glowBaseAlpha * (0.75f + wobble * 0.25f);
            glow.color = gc;
        }
    }
}
