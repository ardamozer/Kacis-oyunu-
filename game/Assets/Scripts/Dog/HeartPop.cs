using UnityEngine;

/// <summary>
/// Sevme tusuna basinca Karabas'in ustunde beliren kalp.
///
/// PLAN.md: sevme tusunun HICBIR mekanik islevi yok, ama "oyunun en cok
/// konusulacak detayi" olmasi bekleniyor. Karsiliginda hicbir sey
/// gormezseniz o detay yok demektir — bu yuzden gorsel tepki sart.
///
/// Kendi kendini yok eder, sahnede birikmez.
/// </summary>
public class HeartPop : MonoBehaviour
{
    [Tooltip("Yukari suzulme hizi.")]
    public float riseSpeed = 1.2f;
    [Tooltip("Saga sola salinim genligi.")]
    public float swayAmount = 0.25f;
    public float swaySpeed = 3f;
    [Tooltip("Toplam omur. Sonunda tamamen saydamlasip yok olur.")]
    public float lifetime = 0.9f;
    [Tooltip("Baslangicta bu orandan gercek boyutuna buyur.")]
    public float popFromScale = 0.4f;

    float age;
    Vector3 startPosition;
    Vector3 targetScale;
    SpriteRenderer sprite;
    float seed;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        targetScale = transform.localScale;
        startPosition = transform.position;
        seed = Random.value * 10f;

        // Ust uste basilirsa kalpler ust uste binmesin.
        startPosition += Vector3.right * Random.Range(-0.15f, 0.15f);
        transform.position = startPosition;
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / lifetime);

        // Yukari suzul + hafif salin.
        float sway = Mathf.Sin((seed + age) * swaySpeed) * swayAmount * k;
        transform.position = startPosition
                           + Vector3.up * riseSpeed * age
                           + Vector3.right * sway;

        // Basta hizli buyur (pop), sonra sabit kal.
        float grow = Mathf.Clamp01(age / (lifetime * 0.25f));
        float scale = Mathf.Lerp(popFromScale, 1f, grow * grow * (3f - 2f * grow));
        transform.localScale = targetScale * scale;

        // Son yarida sonerek kaybol.
        if (sprite != null)
        {
            Color c = sprite.color;
            c.a = 1f - Mathf.Clamp01((k - 0.5f) / 0.5f);
            sprite.color = c;
        }

        if (age >= lifetime) Destroy(gameObject);
    }
}
