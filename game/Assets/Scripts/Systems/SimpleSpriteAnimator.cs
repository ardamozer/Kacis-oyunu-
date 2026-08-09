using UnityEngine;

/// <summary>
/// BASIT KARE OYNATICI — durum mantigi olmayan seyler icin.
///
/// DogAnimator ve PlayerAnimator karakterin durumuna bakiyor; bu ise
/// sadece verilen kareleri sirayla oynatiyor. Hayalet gibi tek isi olan
/// objeler icin yeterli.
///
/// ⚠️ OnEnable'da basa sariyor. Hayalet sahnede kapali duruyor ve final
/// sirasinda aciliyor; sifirlanmasaydi son karesinde donmus olarak
/// belirirdi.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SimpleSpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;
    public bool loop = true;

    [Tooltip("Rastgele bir kareden basla.\n\n" +
             "⚠️ Ayni anda ekranda 19 dekoratif ates var. Hepsi 0. kareden\n" +
             "baslarsa ayni anda ayni sekli alirlar ve ates gibi degil\n" +
             "metronom gibi gorunur. Ates icin ACIK olmali.\n" +
             "Hayalet gibi tek ve senaryolu seylerde KAPALI kalmali —\n" +
             "orada animasyonun bastan baslamasi isteniyor.")]
    public bool randomizeStart;

    SpriteRenderer sr;
    int frame;
    float timer;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void OnEnable()
    {
        if (randomizeStart && frames != null && frames.Length > 0)
        {
            frame = Random.Range(0, frames.Length);
            timer = Random.Range(0f, fps > 0f ? 1f / fps : 0f);
        }
        else
        {
            frame = 0;
            timer = 0f;
        }

        Apply();
    }

    void Update()
    {
        if (frames == null || frames.Length < 2 || fps <= 0f) return;

        timer += Time.deltaTime;
        float step = 1f / fps;

        while (timer >= step)
        {
            timer -= step;
            frame++;

            if (frame >= frames.Length)
            {
                if (loop) frame = 0;
                else { frame = frames.Length - 1; timer = 0f; break; }
            }
        }

        Apply();
    }

    void Apply()
    {
        if (sr == null || frames == null || frames.Length == 0) return;
        Sprite s = frames[Mathf.Clamp(frame, 0, frames.Length - 1)];
        if (s != null) sr.sprite = s;
    }
}
