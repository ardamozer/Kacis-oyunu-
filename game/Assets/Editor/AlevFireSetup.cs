using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ATESLERI CANLANDIR — menuden: ALEV → Atesleri Canlandir
///
/// Fire_Spreadsheet.png'i dilimler ve ESKI alev sprite'ini kullanan her
/// seyi yeni animasyonlu alevle degistirir: sahnedeki dekoratif atesler,
/// yerdeki atesler (kopegin sondurdukleri) ve ates prefablari.
///
/// ⚠️ HEDEFLER ISIMDEN TAHMIN EDILMIYOR.
/// "Alev/Ates/Fire" gecen 36 obje var ama bazilari perde, bazilari duvar,
/// bazilari sadece isaret. Isim eslesmesiyle secmek yanlis seyleri de
/// yakalardi. Onun yerine: SU AN Flame.png sprite'ini kullanan
/// SpriteRenderer'lar bulunuyor. Bu kesin bir olcut.
///
/// ⚠️ PIVOT MERKEZ, alt degil.
/// Ates icin taban pivot daha dogru olurdu ama eski Flame.png'in pivotu
/// merkezdi ve 36 atesin konumu ona gore ayarlanmis. Taban pivota
/// gecseydim hepsi yarim boy yukari kayardi.
///
/// ⚠️ HER ATES RASTGELE KAREDEN BASLAR (randomizeStart).
/// Ekranda ayni anda 19 dekoratif ates var; hepsi 0. kareden baslarsa
/// ayni anda ayni sekli alirlar ve ates gibi degil metronom gibi gorunur.
/// </summary>
public static class AlevFireSetup
{
    const string SheetPath = "Assets/Art/Imported/Fire/Fire_Spreadsheet.png";
    const string OldFlame  = "Assets/Art/Generated/Flame.png";

    const int Cell = 512;      // 1024x1024 sayfa, 2x2 = 4 kare

    // Icerik sinirlari OLCULDU: 4 karenin de alt boslugu 96 px,
    // icerik 224 genis. Ortak kirpma dikdortgeni (biraz paylı):
    const int RectX = 128, RectY = 96, RectW = 256, RectH = 352;

    const float Fps = 10f;

    [MenuItem("ALEV/Atesleri Canlandir", false, 8)]
    public static void Setup()
    {
        Sprite[] frames = SliceSheet();
        if (frames == null || frames.Length == 0) return;

        // Degistirilecek olan: eski alev sprite'lari
        var old = new HashSet<Sprite>(
            AssetDatabase.LoadAllAssetsAtPath(OldFlame).OfType<Sprite>());

        if (old.Count == 0)
        {
            Debug.LogWarning($"[ALEV Ates] {OldFlame} icinde sprite yok; " +
                             "hedefler bulunamadi.");
            return;
        }

        int scene = 0, prefabs = 0;

        // ---- Sahnedeki objeler ----
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr.sprite == null || !old.Contains(sr.sprite)) continue;

            Undo.RegisterFullObjectHierarchyUndo(sr.gameObject, "Atesleri canlandir");
            Attach(sr, frames);
            scene++;
        }

        // ---- Prefablar ----
        // ⚠️ FloorFire, Alev_Buyuk ve Alev_Kucuk eski Flame.png'i DEGIL
        // WhiteSquare'i kullaniyor (turuncuya boyanmis duz kare). "Eski
        // alevi kullananlari bul" olcutu onlari kaciriyordu; ekranda
        // dekoratif ates guzel, oyuncunun sondurdugu ates dikdortgen
        // kaliyordu. Bunlari ADIYLA hedefliyoruz.
        // WhiteSquare'i platformlar ve duvarlar da kullaniyor, o yuzden
        // "WhiteSquare kullanan her seyi degistir" YAPILAMAZ.
        // WallMonster da burada: duz turuncu dikdortgendi, hicbir gorseli
        // yoktu. Ates sanati en azindan "bu bir tehdit" okutuyor.
        var byName = new HashSet<string>
        {
            "FloorFire", "Alev_Buyuk", "Alev_Kucuk", "WallMonster"
        };

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool nameMatch = byName.Contains(
                System.IO.Path.GetFileNameWithoutExtension(path));

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite == null) continue;

                // Adi eslesen prefabta KOK renderer'i degistir; digerlerinde
                // sadece eski alevi kullananlari.
                bool target = old.Contains(sr.sprite) ||
                              (nameMatch && sr.transform == root.transform);
                if (!target) continue;

                Attach(sr, frames);
                sr.color = Color.white;   // eski turuncu tint alevi boyamasin
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabs++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<b>[ALEV Ates]</b> {frames.Length} kare bagladi.\n" +
                  $"  sahnede {scene} obje · {prefabs} prefab guncellendi\n" +
                  $"  {Fps} fps, her ates rastgele kareden basliyor");
    }

    /// <summary>
    /// Sprite'i degistirir ve GORUNEN YUKSEKLIGI korur.
    ///
    /// ⚠️ Olcek telafisi sart: FloorFire duz bir KARE (WhiteSquare)
    /// kullaniyordu ve localScale'i o kareye gore ayarlanmisti. Alev
    /// sprite'inin dogal boyutu farkli; telafisiz degistirirsek ates
    /// bambaska bir boyda cikar.
    /// Genislik yuksekligi takip ediyor (tek tip olcek), boylece alevin
    /// kendi en-boy orani korunuyor — ates zaten dar ve uzun olmali.
    /// </summary>
    static void Attach(SpriteRenderer sr, Sprite[] frames)
    {
        if (sr.sprite != null && frames[0] != null)
        {
            float oldH = sr.sprite.bounds.size.y;
            float newH = frames[0].bounds.size.y;

            if (oldH > 0.0001f && newH > 0.0001f)
            {
                Vector3 s = sr.transform.localScale;
                float visibleH = oldH * s.y;          // degisimden onceki gercek yukseklik
                float k = visibleH / newH;
                sr.transform.localScale = new Vector3(k, k, s.z);
            }
        }

        sr.sprite = frames[0];

        var anim = sr.GetComponent<SimpleSpriteAnimator>();
        if (anim == null) anim = sr.gameObject.AddComponent<SimpleSpriteAnimator>();

        anim.frames = frames;
        anim.fps = Fps;
        anim.loop = true;
        anim.randomizeStart = true;

        EditorUtility.SetDirty(sr);
    }

    /// <summary>
    /// Sayfayi 2x2 boler. Her karede TAM 512x512 degil, olculen ortak
    /// icerik dikdortgeni kullaniliyor — bos bosluk sprite'a girerse
    /// alev gorundugunden kucuk kalir ve olcekler tutmaz.
    /// </summary>
    static Sprite[] SliceSheet()
    {
        var imp = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (imp == null)
        {
            EditorUtility.DisplayDialog("ALEV",
                $"{SheetPath} bulunamadi.\n\nAtes paketi o klasorde mi?", "Tamam");
            return null;
        }

        // Unity dokularda Y ASAGIDAN yukari sayar. Gorsel okuma sirasi
        // (sol ust → sag ust → sol alt → sag alt) icin ust satir cy=512.
        var cells = new (int cx, int cy)[]
        {
            (0,    Cell),   // sol ust
            (Cell, Cell),   // sag ust
            (0,    0),      // sol alt
            (Cell, 0),      // sag alt
        };

        var meta = new List<SpriteMetaData>();
        for (int i = 0; i < cells.Length; i++)
        {
            meta.Add(new SpriteMetaData
            {
                name      = $"Fire_{i}",
                rect      = new Rect(cells[i].cx + RectX, cells[i].cy + RectY, RectW, RectH),
                alignment = (int)SpriteAlignment.Center,   // eski Flame.png de merkezdi
                pivot     = new Vector2(0.5f, 0.5f)
            });
        }

        imp.textureType      = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritesheet      = meta.ToArray();
        EditorUtility.SetDirty(imp);
        imp.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(SheetPath)
            .OfType<Sprite>()
            .OrderBy(s =>
            {
                int i = s.name.LastIndexOf('_');
                return i >= 0 && int.TryParse(s.name.Substring(i + 1), out int n) ? n : 99;
            })
            .ToArray();
    }
}
