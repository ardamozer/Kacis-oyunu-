using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ALEV — 15 Dakikalik Stil Birlestirme & Atmosfer Katmani (Atmosphere Overlay + Vignette).
///
/// Farkli sanatcilardan (laredgames, LuizMelo, penzilla, piposchpatz) toplanan 
/// pixel art sprite'larini tek bir homojen akan yanan ev atmosferinde birletirir.
///
/// Menuden: ALEV → Atmosfer Katmanini Kur
/// </summary>
public static class AlevAtmosphereSetup
{
    [MenuItem("ALEV/Atmosfer Katmanini Kur", false, 7)]
    public static void SetupAtmosphere()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = cgo.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
        }

        Transform canvasTF = canvas.transform;

        // 1. TAM EKRAN SICAK TURUNCU / KEHRIBAR TINT OVERLAY (%16 Opaklik)
        Transform existingOverlay = canvasTF.Find("AtmosphereOverlay");
        GameObject overlayGO = existingOverlay != null ? existingOverlay.gameObject : new GameObject("AtmosphereOverlay", typeof(RectTransform));
        if (existingOverlay == null)
        {
            overlayGO.transform.SetParent(canvasTF, false);
            Undo.RegisterCreatedObjectUndo(overlayGO, "Atmosfer Katmani");
        }

        var overlayRect = overlayGO.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var overlayImage = overlayGO.GetComponent<Image>() ?? overlayGO.AddComponent<Image>();
        overlayImage.sprite = AlevSceneSetup.GetOrCreateSquareSprite();
        overlayImage.color = new Color(1f, 0.42f, 0.08f, 0.16f); // Sicak akkor kehribar glow
        overlayImage.raycastTarget = false;
        overlayGO.transform.SetAsFirstSibling(); // Arka planda dursun, HUD ustte kalsin

        // 2. TAM EKRAN VİNYET KARARTMA MASKESİ (Ambient Vignette)
        Transform existingVignette = canvasTF.Find("AmbientVignetteOverlay");
        GameObject vignetteGO = existingVignette != null ? existingVignette.gameObject : new GameObject("AmbientVignetteOverlay", typeof(RectTransform));
        if (existingVignette == null)
        {
            vignetteGO.transform.SetParent(canvasTF, false);
            Undo.RegisterCreatedObjectUndo(vignetteGO, "Vinyet Katmani");
        }

        var vignetteRect = vignetteGO.GetComponent<RectTransform>();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;

        var vignetteImage = vignetteGO.GetComponent<Image>() ?? vignetteGO.AddComponent<Image>();
        vignetteImage.sprite = AlevSceneSetup.GetOrCreateSquareSprite(); // Varsayilan sprite veya Vignette
        Sprite vSprite = AlevSceneSetup.GetOrCreateVignetteSprite();
        if (vSprite != null) vignetteImage.sprite = vSprite;

        vignetteImage.color = new Color(0.08f, 0.03f, 0.01f, 0.45f); // Koyu kahve/siyah vinyet kenarlari
        // 3. AmbientAtmosphere Bilesenini Ekle (ExecuteAlways ile CANLI GORUNUR)
        AmbientAtmosphere aa = canvas.gameObject.GetComponent<AmbientAtmosphere>() ?? canvas.gameObject.AddComponent<AmbientAtmosphere>();
        aa.overlayImage = overlayImage;
        aa.vignetteImage = vignetteImage;
        // ⚠️ OPAKLIKLAR DUSURULDU (eskiden 0.25 / 0.60).
        // Bu katman ekranin TAMAMINI boyuyor. Duz renkli dikdortgenler
        // varken guclu tint onlari birlestiriyordu ve ise yariyordu.
        // Gercek pixel art geldiginde ayni tint sprite'in KENDI paletini
        // eziyor — playtest'te "kopek atmosferden cok etkileniyor".
        // Asset karistirmayi gizleme isini artik sprite'larin kendisi
        // yapiyor; katmanin isi sadece sicaklik hissi vermek.
        aa.overlayColor = new Color(1f, 0.45f, 0.08f, 0.10f);
        aa.vignetteColor = new Color(0.08f, 0.03f, 0.01f, 0.40f);
        aa.EnsureAtmosphere();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("<b>[ALEV]</b> Atmosfer Katmani ve Vinyet kuruldu! Canvas secildi, Inspector'dan 'AmbientAtmosphere' bilesenindeki renkleri canli degistirebilirsiniz.");
    }
}
