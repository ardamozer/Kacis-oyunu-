using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// KARAKTERIN USTUNDE BELIREN REPLIK.
///
/// Duygusal anlarda tek cumlelik yazi gosterir: kurtarma, olum, hayalet.
/// Dunya konumunu her karede ekrana cevirir, yani karakter hareket etse de
/// yazi ustunde kalir.
///
/// Sahneye ELLE KURULMAZ — ilk cagrida kendi Canvas'ini olusturur.
///
/// Okunabilirlik: oyun turuncu ve parlak bir yangin icinde geciyor, duz beyaz
/// yazi kayboluyor. Bu yuzden her replikte siyah kontur + golge var.
///
/// Kullanim:
///   SpeechBubble.Say(dog.transform, "Geleceğini biliyordum.", 2.5f);
/// </summary>
public class SpeechBubble : MonoBehaviour
{
    const int CanvasSortingOrder = 400;   // sinema bantlarinin (500) ALTINDA

    static Canvas canvas;
    static Font font;

    Transform target;
    RectTransform rect;
    Text label;
    Outline outline;
    Outline backOutline;
    RectTransform plate;
    Image plateImage;
    float worldYOffset;

    // ------------------------------------------------------------------
    //  API
    // ------------------------------------------------------------------

    /// <summary>
    /// Hedefin ustunde bir replik gosterir.
    /// Bos/null metin sessizce yok sayilir — replikleri Inspector'dan
    /// bosaltarak butun sistemi kapatabilirsiniz.
    /// </summary>
    public static SpeechBubble Say(Transform target, string text, float duration,
                                   Color? color = null, float yOffset = 1.9f,
                                   int fontSize = 34)
    {
        if (target == null || string.IsNullOrWhiteSpace(text)) return null;

        EnsureCanvas();
        if (canvas == null) return null;

        var go = new GameObject("SpeechBubble", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var bubble = go.AddComponent<SpeechBubble>();
        bubble.Build(target, text, color ?? Color.white, yOffset, fontSize);
        bubble.StartCoroutine(bubble.LifeRoutine(duration));
        return bubble;
    }

    static void EnsureCanvas()
    {
        if (canvas != null) return;

        var go = new GameObject("SpeechBubbles");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;

        // CanvasScaler KULLANMIYORUZ: konumu dogrudan piksel cinsinden
        // yaziyoruz (WorldToScreenPoint zaten piksel donduruyor). Scaler
        // eklersek iki farkli koordinat sistemi karisir.

        if (font == null)
        {
            // Unity 6'da Arial.ttf kaldirildi, yerine LegacyRuntime.ttf geldi.
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }

    // ------------------------------------------------------------------
    //  Kurulum
    // ------------------------------------------------------------------

    void Build(Transform t, string text, Color color, float yOffset, int fontSize)
    {
        target = t;
        worldYOffset = yOffset;

        rect = GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;      // sol-alt: anchoredPosition = ekran pikseli
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(760f, 76f);

        // ARKA PLAKA — yaziyi yangindan ayiran koyu zemin.
        //
        // Proje font iceremiyor (TMP Essentials import edilmemis), o yuzden
        // yazinin "guzel" gorunmesi tipografiden degil KONTRASTTAN geliyor:
        // koyu plaka + cift kontur, ayni fontu bambaska gosteriyor.
        //
        // ⚠️ Plaka KOK objede, yazi COCUKTA. Unity UI cocuklari sonra ciziyor;
        // ters kurarsak plaka yazinin ustune biner ve yaziyi tamamen orter.
        plateImage = gameObject.AddComponent<Image>();
        plateImage.color = new Color(0f, 0f, 0f, 0f);
        plateImage.raycastTarget = false;
        plate = rect;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(transform, false);

        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        label = labelGO.AddComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.text = text;
        label.color = new Color(color.r, color.g, color.b, 0f);   // fade-in ile gelecek

        // Yangin ekraninda duz yazi kayboluyor: kalin siyah kontur sart.
        outline = labelGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Ikinci kontur ters yonde: tek yonlu kontur yaziyi "kaymis" gosteriyor,
        // ikisi birlikte her kenari sariyor ve dokme harf gibi duruyor.
        backOutline = labelGO.AddComponent<Outline>();
        backOutline.effectColor = new Color(0f, 0f, 0f, 0f);
        backOutline.effectDistance = new Vector2(-3f, 3f);

        UpdatePosition();
    }

    void LateUpdate() => UpdatePosition();

    void UpdatePosition()
    {
        if (target == null || Camera.main == null) return;

        Vector3 world = target.position + Vector3.up * worldYOffset;
        Vector3 screen = Camera.main.WorldToScreenPoint(world);

        // Kamera arkasina duserse gizle (WorldToScreenPoint ters deger dondurur).
        bool visible = screen.z > 0f;
        if (label.enabled != visible) label.enabled = visible;

        rect.anchoredPosition = new Vector2(screen.x, screen.y);
    }

    // ------------------------------------------------------------------
    //  Yasam dongusu
    // ------------------------------------------------------------------

    IEnumerator LifeRoutine(float duration)
    {
        const float fadeIn = 0.18f;
        const float fadeOut = 0.35f;

        yield return Fade(0f, 1f, fadeIn);

        // ⚠️ unscaled: replikler agir cekimde de normal hizda okunmali.
        // Olum sahnesinde timeScale 0.35; olcekli beklersek yazi uc kat
        // uzun kalir ve sahnenin ritmi dagilir.
        float e = 0f;
        while (e < duration) { e += Time.unscaledDeltaTime; yield return null; }

        yield return Fade(1f, 0f, fadeOut);

        Destroy(gameObject);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / duration);
            SetAlpha(Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        Color c = label.color;
        label.color = new Color(c.r, c.g, c.b, a);

        outline.effectColor = new Color(0f, 0f, 0f, a * 0.95f);
        if (backOutline != null) backOutline.effectColor = new Color(0f, 0f, 0f, a * 0.95f);
        if (plateImage != null) plateImage.color = new Color(0f, 0f, 0f, a * 0.42f);
    }
}
