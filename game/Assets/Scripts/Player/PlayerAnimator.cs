using UnityEngine;

/// <summary>
/// ITFAIYECI ANIMASYONU — kare degistirici.
///
/// DogAnimator ile ayni desende: durum zaten PlayerController ve WaterGun'da
/// yaziyor, bu bilesen sadece dogru kare dizisini oynatiyor. Animator
/// Controller kurulmuyor.
///
/// ⚠️ flipX'E DOKUNMUYORUZ — yon cevirmeyi PlayerController yapiyor
/// (FacingDirection'a gore). Ikimiz de yazsak karakter titrer.
/// Bu bilesen SADECE spriteRenderer.sprite atar.
///
/// Kareleri baglamak icin sprite'lar Assets/Art/Imported/Fireman/ altina
/// konur, once ALEV → Indirilen Spritelari Dilimle calistirilir.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Clip
    {
        public Sprite[] frames;
        public float fps = 10f;
        public bool loop = true;

        public bool IsValid => frames != null && frames.Length > 0;
    }

    [Header("Kareler")]
    public Clip idle;
    public Clip run;
    [Tooltip("Havadayken. Tek kare yeterli.")]
    public Clip jump;
    [Tooltip("Su sikarken. Yoksa idle/run kullanilir.")]
    public Clip spray;

    [Header("Esik")]
    [Tooltip("Bunun altinda duruyor sayilir. moveSpeed = 7.")]
    public float runThreshold = 0.5f;

    SpriteRenderer sr;
    PlayerController pc;
    WaterGun gun;

    Clip current;
    int frame;
    float timer;
    float lastX;
    float smoothedSpeed;

    void Awake()
    {
        pc  = GetComponent<PlayerController>();
        gun = GetComponent<WaterGun>();
        sr  = GetComponentInChildren<SpriteRenderer>();
        lastX = transform.position.x;
    }

    void Update()
    {
        if (sr == null || pc == null) return;

        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            float raw = Mathf.Abs(transform.position.x - lastX) / dt;
            lastX = transform.position.x;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, raw, 1f - Mathf.Exp(-12f * dt));
        }

        Clip want = Choose();
        if (want == null || !want.IsValid) return;

        if (want != current)
        {
            current = want;
            frame = 0;
            timer = 0f;
            Apply();
            return;
        }

        if (current.fps <= 0f) return;

        timer += dt;
        float step = 1f / current.fps;
        while (timer >= step)
        {
            timer -= step;
            frame++;
            if (frame >= current.frames.Length)
            {
                if (current.loop) frame = 0;
                else { frame = current.frames.Length - 1; timer = 0f; break; }
            }
        }

        Apply();
    }

    void Apply()
    {
        Sprite s = current.frames[Mathf.Clamp(frame, 0, current.frames.Length - 1)];
        if (s != null) sr.sprite = s;
    }

    Clip Choose()
    {
        // Ara sahne: kontrol kapali, karakter durgun dursun. Kosma karesinde
        // donmus bir itfaiyeci olum sahnesinin tonunu bozuyor.
        if (!pc.CanControl) return Pick(idle, run);

        // Havada olmak su sikmayi ezer — ziplarken su sikan bir karakterin
        // hangi durumda oldugu okunmuyor.
        if (!pc.IsGrounded) return Pick(jump, run, idle);

        if (gun != null && gun.IsSpraying) return Pick(spray, idle, run);

        if (smoothedSpeed >= runThreshold) return Pick(run, idle);
        return Pick(idle, run);
    }

    Clip Pick(params Clip[] options)
    {
        foreach (var c in options)
            if (c != null && c.IsValid) return c;
        return current;
    }
}
