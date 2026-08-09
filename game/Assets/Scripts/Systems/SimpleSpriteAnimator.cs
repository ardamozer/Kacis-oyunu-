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

    SpriteRenderer sr;
    int frame;
    float timer;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void OnEnable()
    {
        frame = 0;
        timer = 0f;
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
