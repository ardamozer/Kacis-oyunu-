using UnityEngine;

/// <summary>
/// KARABAS ANIMASYONU — kare degistirici.
///
/// ⚠️ NEDEN ANIMATOR CONTROLLER DEGIL:
/// Animator + 7 state + gecis oklari kurmak jam'de 40 dakika alir ve
/// her durum degisikliginde Editor'e donmek gerekir. Burada durum zaten
/// DogCompanion'da yaziyor; tek yapmamiz gereken dogru kare dizisini
/// oynatmak. Kod 100 satir ve tamami tek dosyada okunuyor.
///
/// ⚠️ SPRITE'IN flipX'INE VE localScale'INE DOKUNMUYORUZ.
/// Yon cevirmeyi (flipX) ve nefes/ezilme efektini (localScale) zaten
/// DogCompanion yapiyor. Ikimiz de yazsak kopek titrer.
/// Bu bilesen SADECE spriteRenderer.sprite atar.
///
/// Kareleri bagalamak icin: ALEV → Kopek Animasyonlarini Bagla
/// </summary>
[RequireComponent(typeof(DogCompanion))]
public class DogAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Clip
    {
        public Sprite[] frames;
        [Tooltip("Saniyedeki kare sayisi.")]
        public float fps = 10f;
        [Tooltip("Kapaliysa son karede durur — olum/uyuma icin.")]
        public bool loop = true;

        public bool IsValid => frames != null && frames.Length > 0;
    }

    [Header("Kareler — ALEV menusundeki komut doldurur")]
    public Clip idle;
    public Clip walk;
    public Clip run;
    public Clip bark;
    public Clip lyingDown;
    public Clip sitting;
    public Clip licking;
    [Tooltip("Olum karesi. Dongusuz olmali — olu kopek nefes almaz.")]
    public Clip dead;

    [Header("Hiz esikleri (birim/saniye)")]
    [Tooltip("Bunun altinda duruyor sayilir → idle.")]
    public float walkThreshold = 0.4f;
    [Tooltip("Bunun ustunde kosuyor sayilir → run. Oyuncunun hizi 7.")]
    public float runThreshold = 4.5f;

    SpriteRenderer sr;
    DogCompanion dog;

    Clip current;
    int frame;
    float timer;

    float lastX;
    float smoothedSpeed;

    void Awake()
    {
        dog = GetComponent<DogCompanion>();
        sr  = GetComponentInChildren<SpriteRenderer>();
        lastX = transform.position.x;
    }

    void Update()
    {
        if (sr == null || dog == null) return;

        MeasureSpeed();

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

        timer += Time.deltaTime;
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

    /// <summary>
    /// Yatay hiz. DogCompanion Rigidbody kullanmiyor (konumu elle tasiyor),
    /// o yuzden hizi konum farkindan olcuyoruz. Yumusatma sart: tek karelik
    /// duraklamalar yurume/idle arasinda takirdamaya yol aciyor.
    /// </summary>
    void MeasureSpeed()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float raw = Mathf.Abs(transform.position.x - lastX) / dt;
        lastX = transform.position.x;

        smoothedSpeed = Mathf.Lerp(smoothedSpeed, raw, 1f - Mathf.Exp(-10f * dt));
    }

    /// <summary>
    /// Oncelik sirasi onemli: yukaridakiler asagidakileri eziyor.
    /// Olum > yakalanmis > havlama > sevilme > hareket.
    /// </summary>
    Clip Choose()
    {
        // Oldu: hareketsiz tek kare. lyingDown'a DUSMEZ cunku o dongulu —
        // olu kopegin nefes alip vermesi sahneyi mahveder.
        if (dog.IsDead) return Pick(dead, sitting, idle);

        // Henuz kurtarilmadi — atesin altinda yatiyor.
        if (!dog.IsRescued) return Pick(lyingDown, sitting, idle);

        // Yer atesinin ustune atliyor — oyunun "kopek sana yardim ediyor"
        // dedigi tek an. Havlama kareleri atilma hissini veriyor.
        if (dog.IsStomping) return Pick(bark, run, idle);

        // Sevildi — mekanik islevi yok ama PLAN'da "en cok konusulacak
        // detay" olmasi bekleniyor. Karsiliginda bir sey gorunmeli.
        if (Time.time < dog.PetReactionUntil) return Pick(licking, idle);

        // Kurtarildi ama daha pesimize takilmadi: oturuyor.
        if (!dog.IsFollowing) return Pick(sitting, idle);

        if (smoothedSpeed >= runThreshold) return Pick(run, walk, idle);
        if (smoothedSpeed >= walkThreshold) return Pick(walk, run, idle);
        return Pick(idle, sitting);
    }

    /// <summary>Ilk gecerli klibi dondurur — eksik kare dizisi oyunu bozmasin.</summary>
    Clip Pick(params Clip[] options)
    {
        foreach (var c in options)
            if (c != null && c.IsValid) return c;
        return current;
    }
}
