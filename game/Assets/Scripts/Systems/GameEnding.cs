using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// FINAL EKRANI — oyunun sonu.
///
/// Bu yoktu: oyuncu evden cikiyordu ve hicbir sey olmuyordu. Bir oyunun
/// bitisi olmadan "oyun" degil "demo" olur.
///
/// Ekran kendi kendini kuruyor, Inspector'da hicbir sey baglamaniza gerek yok.
/// Sahnedeki herhangi bir bos objeye takin, ya da EscapeTrigger otomatik bulur.
///
/// AKIS: beyaz patlama → karartma → tasma + KARABAS + sure → "R ile tekrar"
/// Sessizlik ve sadelik kasitli. Bu sahnede yapilacak en pahali hata
/// "havali" yapmaya calismaktir.
/// </summary>
public class GameEnding : MonoBehaviour
{
    public static GameEnding Instance { get; private set; }

    [Header("Zamanlama")]
    public float whiteFlashDuration = 0.5f;
    public float fadeToBlackDuration = 1.2f;
    public float textDelay = 0.8f;

    [Header("Metin")]
    public string dogName = "KARABAŞ";
    public string subtitle = "seni disari cikardi";

    bool ended;
    Canvas canvas;
    Image fullscreen;
    Text titleText, statsText, hintText;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Oyuncu evden cikti. Bir kere calisir.</summary>
    public void Finish()
    {
        if (ended) return;
        ended = true;
        StartCoroutine(EndingRoutine());
    }

    IEnumerator EndingRoutine()
    {
        float elapsed = Time.timeSinceLevelLoad;

        BuildUI();
        FreezePlayer();

        // ---- 1. Beyaz patlama ----
        // Disari cikma ani: kapidan gun isigi / patlama. Kisa ve sert.
        float t = 0f;
        while (t < whiteFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            fullscreen.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / whiteFlashDuration));
            yield return null;
        }

        AudioListener.volume = 0f;   // disarisi sessiz

        // ---- 2. Karartma ----
        t = 0f;
        while (t < fadeToBlackDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / fadeToBlackDuration;
            fullscreen.color = Color.Lerp(Color.white, new Color(0.04f, 0.03f, 0.03f), k);
            yield return null;
        }

        yield return WaitUnscaled(textDelay);

        // ---- 3. Tasma ve isim ----
        titleText.text = dogName;
        yield return FadeIn(titleText, 1.2f);

        yield return WaitUnscaled(0.6f);
        statsText.text = $"{subtitle}\n\nSure  {Mathf.FloorToInt(elapsed / 60f):0}:{Mathf.FloorToInt(elapsed % 60f):00}";
        yield return FadeIn(statsText, 1f);

        yield return WaitUnscaled(1.2f);
        hintText.text = "R  —  tekrar oyna";
        yield return FadeIn(hintText, 0.8f);

        // ---- 4. Yeniden baslatma ----
        while (!Input.GetKeyDown(KeyCode.R)) yield return null;

        AudioListener.volume = 1f;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ------------------------------------------------------------------

    void FreezePlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        var pc = p.GetComponent<PlayerController>();
        if (pc != null) pc.CanControl = false;

        var rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    IEnumerator FadeIn(Graphic g, float duration)
    {
        float t = 0f;
        Color c = g.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            g.color = c;
            yield return null;
        }
        c.a = 1f;
        g.color = c;
    }

    // ------------------------------------------------------------------
    //  UI kendi kendini kuruyor — Inspector'da baglanti gerekmiyor
    // ------------------------------------------------------------------

    void BuildUI()
    {
        var go = new GameObject("FinalEkrani");
        go.transform.SetParent(transform, false);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;          // her seyin ustunde

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        fullscreen = NewImage("Perde", go.transform, new Color(1f, 1f, 1f, 0f));
        Stretch(fullscreen.rectTransform);

        titleText = NewText("Isim", go.transform, 96, FontStyle.Bold,
                            new Color(1f, 0.88f, 0.62f, 0f));
        Center(titleText.rectTransform, 90f, 1200f, 160f);

        statsText = NewText("Bilgi", go.transform, 44, FontStyle.Normal,
                            new Color(0.85f, 0.83f, 0.80f, 0f));
        Center(statsText.rectTransform, -80f, 1200f, 260f);

        hintText = NewText("Ipucu", go.transform, 32, FontStyle.Normal,
                           new Color(0.60f, 0.58f, 0.56f, 0f));
        Center(hintText.rectTransform, -330f, 1200f, 80f);
    }

    static Image NewImage(string name, Transform parent, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    static Text NewText(string name, Transform parent, int size, FontStyle style, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<Text>();
        // Unity'nin yerlesik fontu — ayrica bir font asset'i import etmeye gerek yok.
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = c;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rt, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
