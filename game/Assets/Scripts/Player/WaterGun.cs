using UnityEngine;

/// <summary>
/// Su tabancasi. Basili tuttukca su siker, deposundan su harcar,
/// onundeki IWaterDamageable objelere hasar verir.
///
/// Nisan basit tutuldu: bakilan yon + yukari. Fare ile 360 derece nisan almak
/// kod ve his ayari demek, jam'de gereksiz.
/// </summary>
public class WaterGun : MonoBehaviour
{
    [Header("Tuketim ve Hasar")]
    [Tooltip("Saniyede harcanan su. 15 = 100 birimlik depo ~7 saniye dayanir.")]
    public float waterPerSecond = 15f;

    // ================= DENGE OLCEGI — IKI KISI DE BUNA GORE AYARLASIN =================
    //
    //   damagePerSecond = 40  →  dusmanin cani, olmesi gereken SANIYE x 40 olur.
    //
    //   Dusman tipi            Can    Olme suresi
    //   ---------------------  -----  -----------
    //   Zayif duvar canavari     40      1.0 sn
    //   Normal duvar canavari    60      1.5 sn
    //   Guclu / kalabalik icin   80      2.0 sn
    //   Karabas'i orten ates     50      1.25 sn   (kurtarma ani, uzun surmemeli)
    //   Boss                      —      hic olmez (TakeWater can azaltmaz)
    //
    //   2 saniyeyi ASMAYIN. Jam oyununda tek dusmana 3 saniye su tutmak
    //   agir hissettiriyor — oyuncu sikiliyor ve kacmayi tercih ediyor.
    // ==================================================================================
    [Tooltip("Saniyede verilen hasar. Dusman canlari buna gore ayarlanir — " +
             "koddaki denge olcegi tablosuna bakin.")]
    public float damagePerSecond = 40f;

    [Header("Menzil")]
    [Tooltip("Suyun ulastigi mesafe.")]
    public float range = 3.5f;

    [Tooltip("Su huzmesinin kalinligi. Genis = nisan almasi kolay.")]
    public float width = 1.2f;

    [Tooltip("Namlunun cikis noktasi. Player'in altina bos bir GameObject koyup surukleyin.")]
    public Transform nozzle;

    [Header("Neye Isabet Etsin")]
    [Tooltip("Enemy layer'ini secin.")]
    public LayerMask hitLayers;

    [Header("Gorsel / Ses (opsiyonel, bos birakilabilir)")]
    public ParticleSystem sprayEffect;
    public AudioSource spraySound;

    /// <summary>Su an su sikiliyor mu? Animasyon ve UI okuyabilir.</summary>
    public bool IsSpraying { get; private set; }

    WaterTank tank;
    PlayerController player;

    // Ayni karede ayni dusmana iki kez hasar vermemek icin.
    readonly Collider2D[] hitBuffer = new Collider2D[16];

    void Awake()
    {
        tank = GetComponent<WaterTank>();
        player = GetComponent<PlayerController>();

        if (nozzle == null) nozzle = transform;   // atanmamissa karakterin merkezi
    }

    void Update()
    {
        bool canControl = player == null || player.CanControl;
        bool wantsToSpray = canControl && Input.GetButton("Fire1");

        // Su varsa harca. Yoksa TrySpend false doner ve sikma durur.
        IsSpraying = wantsToSpray && (tank == null || tank.TrySpend(waterPerSecond * Time.deltaTime));

        UpdateEffects();

        if (IsSpraying) DamageTargetsInBeam();
    }

    /// <summary>Nisan yonu: yukari tusuna basiliysa yukari, degilse bakilan yon.</summary>
    Vector2 GetAimDirection()
    {
        if (Input.GetAxisRaw("Vertical") > 0.1f) return Vector2.up;
        int facing = player != null ? player.FacingDirection : 1;
        return new Vector2(facing, 0f);
    }

    void DamageTargetsInBeam()
    {
        Vector2 dir = GetAimDirection();

        // Huzmeyi bir dikdortgen olarak dusun: namludan baslayip 'range' kadar uzanan kutu.
        Vector2 center = (Vector2)nozzle.position + dir * (range * 0.5f);
        Vector2 size = dir.y > 0.5f
            ? new Vector2(width, range)    // yukari nisan alinca kutu dikey
            : new Vector2(range, width);   // yatay nisan alinca kutu yatay

        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, hitBuffer, hitLayers);

        float damageThisFrame = damagePerSecond * Time.deltaTime;

        for (int i = 0; i < count; i++)
        {
            // GetComponent yerine GetComponentInParent: collider alt objede olabilir.
            var target = hitBuffer[i].GetComponentInParent<IWaterDamageable>();
            target?.TakeWater(damageThisFrame);
        }
    }

    void UpdateEffects()
    {
        if (sprayEffect != null)
        {
            // Partikuleyi surekli Play/Stop cagirmamak icin durumu kontrol et.
            if (IsSpraying && !sprayEffect.isEmitting) sprayEffect.Play();
            else if (!IsSpraying && sprayEffect.isEmitting) sprayEffect.Stop();

            // Partikulu nisan yonune cevir.
            Vector2 dir = GetAimDirection();
            sprayEffect.transform.right = dir;
        }

        if (spraySound != null)
        {
            if (IsSpraying && !spraySound.isPlaying) spraySound.Play();
            else if (!IsSpraying && spraySound.isPlaying) spraySound.Stop();
        }
    }

    // Sahne penceresinde su huzmesinin kapsama alanini gosterir — menzil ayarini kolaylastirir.
    void OnDrawGizmosSelected()
    {
        if (nozzle == null || player == null) return;

        Vector2 dir = Application.isPlaying
            ? GetAimDirection()
            : Vector2.right;

        Vector2 center = (Vector2)nozzle.position + dir * (range * 0.5f);
        Vector2 size = dir.y > 0.5f ? new Vector2(width, range) : new Vector2(range, width);

        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.4f);
        Gizmos.DrawWireCube(center, size);
    }
}
