using UnityEngine;

/// <summary>
/// Duvar canavarinin firlattigi alev topu.
/// Duz gider, oyuncuya degerse hasar verir, duvara/zemine degerse veya
/// sure dolunca yok olur.
///
/// ⚠️ NEDEN SendMessage?
/// Kisi A'nin PlayerHealth sinifi henuz yazilmadi. Eger burada
/// "GetComponent&lt;PlayerHealth&gt;()" yazsaydik proje DERLENMEZDI ve
/// A o dosyayi push'layana kadar hicbir sey test edemezdim.
/// SendMessage derleme bagimliligi kurmaz: sinif yoksa sessizce hicbir sey yapar,
/// A dosyayi ekleyince TEK SATIR DEGISIKLIK OLMADAN calismaya baslar.
/// (Imza sozlesmede sabit: PlayerHealth.TakeDamage(int amount))
/// </summary>
public class FlameProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 6f;
    [Tooltip("Bu sure sonunda kendini yok eder (sahnede alev birikmesin).")]
    public float lifeTime = 4f;

    [Header("Hasar")]
    [Tooltip("Oyuncunun 3 kalbi var, 1 = bir kalp.")]
    public int damage = 1;

    [Header("Carpisma")]
    [Tooltip("Alevin uzerinde sonecegi katmanlar — Ground'u secin.")]
    public LayerMask blockingLayers;

    Vector2 direction = Vector2.right;

    /// <summary>WallMonster dogurduktan hemen sonra bunu cagirir.</summary>
    public void Launch(Vector2 dir)
    {
        direction = dir.normalized;

        // Sprite'i gittigi yone cevir.
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) sprite.flipX = direction.x < 0f;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Kendi canavarina veya diger dusmanlara zarar vermesin.
        if (other.CompareTag("Enemy")) return;

        // Kopek asla oyuncunun hatasiyla olmez (PLAN.md tasarim karari) —
        // alev kopege carparsa hicbir sey olmaz, alev de sonmez, icinden gecer.
        if (other.CompareTag("Dog")) return;

        if (other.CompareTag("Player"))
        {
            other.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
            return;
        }

        // Duvar / zemin
        if (((1 << other.gameObject.layer) & blockingLayers) != 0)
        {
            Destroy(gameObject);
        }
    }
}
