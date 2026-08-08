using System.Collections;
using UnityEngine;

/// <summary>
/// Itfaiyecinin yatay hareketi ve ziplamasi.
///
/// Icinde platformculari "iyi hissettiren" 4 numara var (coyote time, jump buffer,
/// degisken zipla yuksekligi, hizli dusme). Bunlar oyunun kontrol hissini
/// belirleyen seyler — degerleri Inspector'dan oynayarak ayarlayin.
///
/// Ayrica IStompable objelerin (yer atesi) ustune dusunce ezme islemini yapar.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket")]
    [Tooltip("Maksimum yurume hizi.")]
    public float moveSpeed = 7f;
    [Tooltip("Hizlanma. Buyuk = daha keskin baslangic.")]
    public float acceleration = 70f;
    [Tooltip("Yavaslama. Buyuk = daha cabuk durur.")]
    public float deceleration = 80f;

    [Header("Ziplama")]
    public float jumpForce = 15f;
    [Tooltip("Kenardan dustukten sonra hala ziplayabildigin sure. Oyuncu 'kacirdim' demez.")]
    public float coyoteTime = 0.12f;
    [Tooltip("Yere degmeden once basilan zipla tusunun hatirlanma suresi.")]
    public float jumpBufferTime = 0.12f;
    [Tooltip("Duserken yercekimi carpani. Zipla yayvan degil, tokat gibi hissedilsin.")]
    public float fallMultiplier = 3f;
    [Tooltip("KAPALI = her ziplama ayni yukseklikte. Ogrenmesi kolay, " +
             "hassas parkurlarda daha adil.\n" +
             "ACIK = tusu erken birakinca alcak ziplarsiniz. Daha fazla " +
             "kontrol verir ama alismasi gerekir.")]
    public bool variableJumpHeight = true;

    [Tooltip("Degisken yukseklik ne kadar sert olsun. Buyuk = tusu birakinca " +
             "cok cabuk duser. 5 sertti, 2.5 daha affedici.")]
    public float lowJumpMultiplier = 2.5f;

    [Header("Zemin Kontrolu")]
    [Tooltip("Karakterin ayaklarinin altina koydugunuz bos GameObject.")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;
    [Tooltip("Ground layer'ini secin.")]
    public LayerMask groundLayer;

    [Header("Ezme (Stomp)")]
    [Tooltip("Bir dusmani ezdikten sonraki sekme kuvveti.")]
    public float stompBounce = 13f;
    [Tooltip("Ezmenin sayilmasi icin carpismanin ne kadar yukaridan gelmesi gerektigi. 0.5 = yaklasik 60 derece.")]
    public float stompMinNormalY = 0.5f;

    /// <summary>Bakilan yon: +1 sag, -1 sol. WaterGun bunu okuyup suyu o yone sikar.</summary>
    public int FacingDirection { get; private set; } = 1;

    /// <summary>Yerde mi? Diger scriptler okuyabilir.</summary>
    public bool IsGrounded { get; private set; }

    /// <summary>Final sahnesinde kontrolu oyuncudan almak icin bunu false yapin.</summary>
    public bool CanControl { get; set; } = true;

    Rigidbody2D rb;
    SpriteRenderer sprite;

    float horizontalInput;
    float coyoteCounter;
    float jumpBufferCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

        // Karakterin devrilmemesi icin. Inspector'dan da ayarlanabilir ama
        // unutulmasin diye burada garantiye aliyoruz.
        rb.freezeRotation = true;
    }

    void Update()
    {
        // ---- Girdi okuma ----
        // Kontrol alinmissa (final sahnesi) girdiyi sifirla, karakter dursun.
        horizontalInput = CanControl ? Input.GetAxisRaw("Horizontal") : 0f;

        // ---- Zemin kontrolu ----
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // ---- Coyote time ----
        // Yerdeyken sayaci doldur, havadayken azalt. Sayac > 0 iken ziplayabilir.
        // Bu sayede platformun kenarindan yeni dustuyse hala ziplayabilir.
        if (IsGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        // ---- Jump buffer ----
        // Oyuncu yere degmeden hemen once zipla'ya bastiysa, yere deger degmez zipla.
        if (CanControl && Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // ---- Zipla ----
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;   // cift ziplamayi engelle
        }

        // ---- Bakis yonu ----
        if (horizontalInput != 0f)
        {
            FacingDirection = horizontalInput > 0f ? 1 : -1;
            if (sprite != null) sprite.flipX = FacingDirection < 0;
        }
    }

    void FixedUpdate()
    {
        ApplyHorizontalMovement();
        ApplyBetterGravity();
    }

    void ApplyHorizontalMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    void ApplyBetterGravity()
    {
        // Unity'nin varsayilan yercekimi platformcularda "aya inmis gibi" hissettirir.
        // Duserken agirlastirmak, ziplamayi keskin ve tatminkar yapar.
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        // Yukari cikarken tus birakildiysa erken indir → degisken zipla yuksekligi.
        else if (variableJumpHeight && rb.linearVelocity.y > 0f && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    void Jump()
    {
        // Dikey hizi sifirlayip sabit kuvvet vermek, her ziplamanin ayni
        // yuksekligi vermesini garanti eder.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.linearVelocity += Vector2.up * jumpForce;
    }

    /// <summary>
    /// Oyuncuyu kontrolu disinda bir noktaya yumusakca kaydirir.
    /// Ara sahneler icin — ozellikle Karabas'in olum aninda kopegin
    /// oyuncuyu itmesi.
    ///
    /// Kullanim (Kisi B, EndingSequence icinden):
    ///     controller.CanControl = false;
    ///     yield return controller.ForceMoveTo(hedefX, 0.25f);
    ///
    /// ⚠️ Once CanControl = false yapin, yoksa oyuncunun girdisi kaydirmayla
    ///    yarisir ve sahne bozulur.
    /// </summary>
    public IEnumerator ForceMoveTo(float targetX, float duration)
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        float startX = transform.position.x;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            // SmoothStep: itilme sert baslayip yumusak bitsin.
            k = k * k * (3f - 2f * k);

            Vector3 p = transform.position;
            p.x = Mathf.Lerp(startX, targetX, k);
            transform.position = p;

            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>
    /// Bir seyin USTUNE dustugumuzu algila → IStompable ise ez ve sek.
    /// Kisi B'nin yer atesi (FloorFire) bu arayuzu uyguluyor.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        var stompable = collision.collider.GetComponent<IStompable>();
        if (stompable == null) return;

        // Carpisma normali yukari bakiyorsa, biz onun ustune dusmusuzdur.
        // Yandan carptiysak ezme sayilmaz (o zaman hasar almaliyiz).
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y >= stompMinNormalY)
            {
                stompable.OnStomped();

                // Sekme: oyuncuya "ezdim!" geri bildirimi verir ve zincirleme
                // ezmeye izin verir. Platformcularda cok tatminkar hissettirir.
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.linearVelocity += Vector2.up * stompBounce;
                return;
            }
        }
    }

    // Sahne penceresinde zemin kontrol dairesini gorunur yapar — ayarlamayi kolaylastirir.
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
