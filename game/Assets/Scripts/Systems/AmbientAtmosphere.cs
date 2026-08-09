using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// YANAN EV ATMOSFERİ VE VİNYET KATMANI (ExecuteAlways).
///
/// Oyundaki tum pixel art sprite'larini (itfaiyeci, kopek, ev mobilyalari)
/// yanan evin akkor turuncu aydinlatmasi altinda homojenlestirir.
///
/// [ExecuteAlways] sayesinde Unity Editor (Scene & Game view) pencerelerinde
/// ANINDA gorunur. Opakligi Inspector'dan slider ile canli ayarlayabilirsiniz.
/// </summary>
[ExecuteAlways]
public class AmbientAtmosphere : MonoBehaviour
{
    [Header("1. Kehribar / Turuncu Atmosfer Glow")]
    [Tooltip("Yanan evin akkor turuncu tonu.\n\n" +
             "⚠️ %10'un cok ustune cikmayin. Bu katman EKRANIN TAMAMINI\n" +
             "boyuyor; gercek pixel art'in kendi paletini eziyor.")]
    public Color overlayColor = new Color(1f, 0.45f, 0.08f, 0.10f);

    [Header("2. Vinyet Karartma Maskesi")]
    [Tooltip("Ekran kenarlarini karartan vinyet maskesi.\n\n" +
             "Cok yuksek olursa kenarlardaki dusman ve platformlar okunmaz olur.")]
    public Color vignetteColor = new Color(0.08f, 0.03f, 0.01f, 0.40f);

    [Header("3. Canlilik (Ateş Nefesi)")]
    [Tooltip("Ateslerin hafif akkor gibi nefes alip vermesi. 0 = kapali.")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.04f;

    [Header("Baglantilar (Otomatik Olusturulur)")]
    public Image overlayImage;
    public Image vignetteImage;

    void OnEnable()
    {
        EnsureAtmosphere();
    }

    void Start()
    {
        EnsureAtmosphere();
    }

    void Update()
    {
        // ⚠️ EnsureAtmosphere BURADA CAGRILMIYOR, bilerek.
        // Her karede cagrildiginda:
        //   - referans bulunamazsa FindFirstObjectByType<Canvas>() calisiyordu
        //     (sahne geneli arama, her kare)
        //   - SetupFullRect her karede RectTransform yaziyordu, bu da her
        //     karede Canvas layout rebuild tetikliyor — WebGL'de bu tek
        //     basina kare suresini gozle gorulur sekilde dusuruyor
        //   - Editor'de ExecuteAlways ile birlesince sahne surekli "kaydedilmemis"
        //     kaliyordu
        // Referanslar OnEnable/Start/OnValidate'te bir kez kuruluyor; kaybolurlarsa
        // asagidaki ucuz null kontrolu yakaliyor.
        if (overlayImage == null || vignetteImage == null) EnsureAtmosphere();

        ApplyColors();
    }

    public void EnsureAtmosphere()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
        }

        if (canvas == null) return;

        Transform canvasTF = canvas.transform;

        // 1. Overlay Image
        if (overlayImage == null)
        {
            Transform t = canvasTF.Find("AtmosphereOverlay");
            if (t != null) overlayImage = t.GetComponent<Image>();
        }

        if (overlayImage == null)
        {
            var go = new GameObject("AtmosphereOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasTF, false);
            go.transform.SetAsFirstSibling(); // Arka planda kalsin
            overlayImage = go.GetComponent<Image>();
        }

        SetupFullRect(overlayImage.rectTransform);
        overlayImage.raycastTarget = false;

        // 2. Vignette Image
        // ⚠️ VINYETI SIFIRDAN YARATMIYORUZ.
        // Vinyetin isi ekran KENARLARINI karartmak; bunu yapan sey radyal
        // gecisli sprite (merkez saydam, kenar opak). Sprite'siz bir Image
        // duz beyaz dikdortgendir ve vignetteColor ile boyandiginda EKRANIN
        // TAMAMINI %60 karartir — oyun kapkara olur.
        //
        // Sprite'i sadece Editor script'i uretebiliyor (AlevSceneSetup).
        // Bulamazsak vinyeti sessizce atliyoruz; siyah ekran vermektense
        // vinyetsiz calismak yeglenir.
        if (vignetteImage == null)
        {
            Transform t = canvasTF.Find("AmbientVignetteOverlay");
            if (t != null) vignetteImage = t.GetComponent<Image>();
        }

        if (vignetteImage != null)
        {
            SetupFullRect(vignetteImage.rectTransform);
            vignetteImage.raycastTarget = false;
        }
    }

    void SetupFullRect(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void ApplyColors()
    {
        float pulse = 0f;
        if (Application.isPlaying && pulseAmount > 0f)
        {
            pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        }

        if (overlayImage != null)
        {
            Color c = overlayColor;
            c.a = Mathf.Clamp01(c.a + pulse);
            overlayImage.color = c;
        }

        if (vignetteImage != null)
        {
            Color v = vignetteColor;
            vignetteImage.color = v;
        }
    }

    void OnValidate()
    {
        EnsureAtmosphere();
        ApplyColors();
    }
}
