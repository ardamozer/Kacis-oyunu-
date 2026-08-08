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
    [Tooltip("Yanan evin akkor turuncu tonu. Opakligi artirarak etkiyi belirginlestirebilirsiniz.")]
    public Color overlayColor = new Color(1f, 0.45f, 0.08f, 0.25f);

    [Header("2. Vinyet Karartma Maskesi")]
    [Tooltip("Ekran kenarlarini karartan vinyet maskesinin rengi ve kule donusme opakligi.")]
    public Color vignetteColor = new Color(0.08f, 0.03f, 0.01f, 0.60f);

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
        EnsureAtmosphere();
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
        if (vignetteImage == null)
        {
            Transform t = canvasTF.Find("AmbientVignetteOverlay");
            if (t != null) vignetteImage = t.GetComponent<Image>();
        }

        if (vignetteImage == null)
        {
            var go = new GameObject("AmbientVignetteOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasTF, false);
            go.transform.SetSiblingIndex(1); // Overlay'in ustunde
            vignetteImage = go.GetComponent<Image>();
        }

        SetupFullRect(vignetteImage.rectTransform);
        vignetteImage.raycastTarget = false;
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
