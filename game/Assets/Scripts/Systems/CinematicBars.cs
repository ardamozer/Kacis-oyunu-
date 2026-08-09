using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SINEMA BANTLARI — ust ve alttan kapanan siyah paravanlar.
///
/// Ara sahnelerde ekrani daraltip "su an oynamiyorsun, izliyorsun" mesajini
/// tek bakista verir. Oyuncu kontrolun alindigini zaten hissediyor; bant onu
/// bir arizadan bir tercihe cevirir ("tuşlar calismiyor" degil, "sahne oynuyor").
///
/// Sahneye ELLE KURULMAZ: ilk cagrida kendi Canvas'ini olusturur. Bagli alan,
/// prefab, serilestirme yok — bu jam'de en cok zamani serilestirilmis alanlarin
/// eksik kalmasi yedi.
///
/// Kullanim:
///   CinematicBars.Show(0.12f, 0.35f);   // ekranin %12'si ust + %12 alt
///   CinematicBars.Hide(0.35f);
/// </summary>
public class CinematicBars : MonoBehaviour
{
    static CinematicBars instance;

    RectTransform topBar, bottomBar;
    Coroutine anim;
    float current;          // 0 = acik, 1 = tam bant yuksekligi
    float targetHeight = 0.12f;

    // ------------------------------------------------------------------
    //  Disari acilan API
    // ------------------------------------------------------------------

    /// <param name="heightFraction">Bant yuksekligi, ekran yuksekliginin orani.</param>
    public static void Show(float heightFraction, float duration)
    {
        CinematicBars b = Ensure();
        if (b == null) return;

        b.targetHeight = heightFraction;
        b.Animate(1f, duration);
    }

    public static void Hide(float duration)
    {
        if (instance == null) return;      // hic acilmadiysa yapacak is yok
        instance.Animate(0f, duration);
    }

    /// <summary>Aninda kapat — guvenlik agi (sahne yeniden yuklenirse vs).</summary>
    public static void HideImmediate()
    {
        if (instance == null) return;

        if (instance.anim != null) instance.StopCoroutine(instance.anim);
        instance.anim = null;
        instance.current = 0f;
        instance.Apply();
    }

    // ------------------------------------------------------------------
    //  Kurulum
    // ------------------------------------------------------------------

    static CinematicBars Ensure()
    {
        if (instance != null) return instance;

        var go = new GameObject("CinematicBars");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // HUD'un (kalpler, su bari, anahtarlar) USTUNDE olmali.
        canvas.sortingOrder = 500;

        instance = go.AddComponent<CinematicBars>();
        instance.Build();
        return instance;
    }

    void Build()
    {
        topBar = NewBar("TopBar");
        bottomBar = NewBar("BottomBar");
        Apply();
    }

    RectTransform NewBar(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;         // tiklamayi engellemesin

        return go.GetComponent<RectTransform>();
    }

    // ------------------------------------------------------------------
    //  Animasyon
    // ------------------------------------------------------------------

    void Animate(float to, float duration)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateRoutine(to, duration));
    }

    IEnumerator AnimateRoutine(float to, float duration)
    {
        float from = current;

        if (duration <= 0f)
        {
            current = to;
            Apply();
            anim = null;
            yield break;
        }

        float e = 0f;
        while (e < duration)
        {
            // ⚠️ unscaled: bantlar agir cekimden ETKILENMEMELI. Olum sahnesinde
            // timeScale 0.35'e dusuyor; olcekli zaman kullanirsak bantlar uc kat
            // yavas kapanir ve sahnenin girisi sarkar.
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / duration);
            k = k * k * (3f - 2f * k);     // SmoothStep

            current = Mathf.Lerp(from, to, k);
            Apply();
            yield return null;
        }

        current = to;
        Apply();
        anim = null;
    }

    /// <summary>
    /// Bantlari yerlestirir. Anchor ile oran kullaniyoruz — piksel degil:
    /// her cozunurlukte ve WebGL'in degisken pencere boyutunda ayni duruyor,
    /// CanvasScaler'a da ihtiyac kalmiyor.
    /// </summary>
    void Apply()
    {
        float h = current * targetHeight;

        topBar.anchorMin = new Vector2(0f, 1f - h);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.offsetMin = Vector2.zero;
        topBar.offsetMax = Vector2.zero;

        bottomBar.anchorMin = new Vector2(0f, 0f);
        bottomBar.anchorMax = new Vector2(1f, h);
        bottomBar.offsetMin = Vector2.zero;
        bottomBar.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
