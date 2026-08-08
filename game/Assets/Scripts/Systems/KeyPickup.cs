using UnityEngine;

/// <summary>
/// Toplanabilir anahtar.
///
/// Karanlik ve kaotik bir odada goze carpmasi sart, yoksa oyuncu
/// "nereye gidecegimi bilmiyorum" der. Uc sey yapiyor:
///   - yukari asagi suzuluyor (hareket dikkat ceker)
///   - nabiz gibi parliyor
///   - etrafinda soguk renkli isik halkasi var (turuncu atesten ayrisir)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour
{
    [Header("Hangi anahtar")]
    public KeyColor color = KeyColor.Mavi;

    [Header("Dikkat cekme")]
    [Tooltip("Yukari asagi suzulme mesafesi.")]
    public float bobHeight = 0.35f;
    public float bobSpeed = 2f;

    [Tooltip("Etrafindaki isik halkasi. Opsiyonel.")]
    public SpriteRenderer glow;

    [Header("Ses")]
    public AudioSource pickupSound;

    SpriteRenderer sprite;
    Vector3 basePos;
    Vector3 glowBaseScale;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        basePos = transform.position;

        Color c = KeyColors.Of(color);
        if (sprite != null) sprite.color = c;
        if (glow != null)
        {
            glow.color = new Color(c.r, c.g, c.b, 0.4f);
            glowBaseScale = glow.transform.localScale;
        }
    }

    void Update()
    {
        float t = Time.time * bobSpeed;

        transform.position = basePos + Vector3.up * Mathf.Sin(t) * bobHeight;

        if (glow != null)
        {
            float pulse = 1f + Mathf.Sin(t * 1.3f) * 0.18f;
            glow.transform.localScale = glowBaseScale * pulse;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponentInParent<KeyInventory>();
        if (inv == null) return;

        inv.Add(color);

        if (pickupSound != null)
        {
            // Ses obje yok olmadan bitsin diye sesi ayirip birakiyoruz.
            pickupSound.transform.SetParent(null);
            pickupSound.Play();
            Destroy(pickupSound.gameObject, 2f);
        }

        Destroy(gameObject);
    }
}
