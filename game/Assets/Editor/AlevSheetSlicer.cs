using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// INDIRILEN SPRITE SAYFALARINI DILIMLE — menuden:
/// ALEV → Indirilen Spritelari Dilimle
///
/// Assets/Art/Imported/ altindaki HER alt klasoru isler. Kare boyutunu ve
/// pivotu tahmin etmez, OLCER:
///
///   Kare boyutu = sayfanin yuksekligi (kareler kare varsayilir).
///                 Genislik buna tam bolunmuyorsa o sayfa atlanir.
///
///   Pivot Y     = klasordeki TUM karelerin en alt opak pikseli.
///
/// ⚠️ PIVOT NEDEN KLASOR GENELINDE, SAYFA BASINA DEGIL:
/// Pivot her sayfa icin ayri olculurse karakter animasyon degistirdiginde
/// dikeyde ziplar — "yatiyor" pozunun en alt pikseli "duruyor" pozundan
/// farklidir ve ikisi farkli pivot alirsa zemin cizgisi kayar. Zemin
/// cizgisi TEK olmali; onu veren sey klasordeki en alt piksel.
///
/// ⚠️ PIVOT NEDEN ORTADA DEGIL:
/// Kareler karakterden cok daha buyuk oluyor (kopekte 100x100 kare,
/// 29 piksel govde, altinda 35 piksel bosluk). Pivot ortadayken:
///   - zemine oturtma hesabi bos boslugu da sayiyor, karakter havada duruyor
///   - localScale ile yapilan ezilme efektleri karakteri yere gomuyor
/// Pivot ayaklara alininca ikisi de kendiliginden cozuluyor.
/// </summary>
public static class AlevSheetSlicer
{
    const string Root = "Assets/Art/Imported";

    [MenuItem("ALEV/Indirilen Spritelari Dilimle", false, 5)]
    public static void SliceAll()
    {
        if (!AssetDatabase.IsValidFolder(Root))
        {
            EditorUtility.DisplayDialog("ALEV", $"{Root} klasoru yok.", "Tamam");
            return;
        }

        string[] folders = AssetDatabase.GetSubFolders(Root);
        if (folders.Length == 0)
        {
            EditorUtility.DisplayDialog("ALEV",
                $"{Root} altinda alt klasor yok.\n\n" +
                "Her karakter icin bir klasor acin: Art/Imported/Dog, .../Fireman", "Tamam");
            return;
        }

        var report = new System.Text.StringBuilder();

        foreach (string folder in folders)
            SliceFolder(folder, report);

        AssetDatabase.Refresh();
        Debug.Log($"<b>[ALEV Dilimleme]</b>{report}");
    }

    static void SliceFolder(string folder, System.Text.StringBuilder report)
    {
        string name = Path.GetFileName(folder);
        var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                                 .Select(AssetDatabase.GUIDToAssetPath)
                                 .OrderBy(p => p)
                                 .ToList();

        if (paths.Count == 0)
        {
            report.Append($"\n\n📁 {name} — PNG yok, atlandi.");
            return;
        }

        // ---- 1. Okunabilir hale getir (piksel taramasi icin sart) ----
        var restore = new Dictionary<string, bool>();
        foreach (string p in paths)
        {
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) continue;
            restore[p] = imp.isReadable;
            if (!imp.isReadable)
            {
                imp.isReadable = true;
                imp.SaveAndReimport();
            }
        }

        // ---- 2. Klasor genelinde EN ALT opak pikseli bul ----
        int cell = -1;
        int lowestOpaqueRow = -1;      // karenin USTUNDEN sayilan satir
        var usable = new List<(string path, int count)>();

        foreach (string p in paths)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (tex == null) continue;

            int c = tex.height;
            if (c <= 0 || tex.width % c != 0)
            {
                report.Append($"\n  ✖ {Path.GetFileName(p)} — {tex.width}x{tex.height}, " +
                              $"genislik yukseklige tam bolunmuyor. ATLANDI.");
                continue;
            }

            if (cell < 0) cell = c;
            else if (cell != c)
            {
                report.Append($"\n  ✖ {Path.GetFileName(p)} — kare {c}, klasorun geri kalani " +
                              $"{cell}. Karisik olcu; ATLANDI.");
                continue;
            }

            usable.Add((p, tex.width / c));

            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch { continue; }

            // GetPixels32 SOL ALTTAN baslar. Bizim istedigimiz "karenin
            // altindan kac piksel bos" oldugu, o yuzden alttan yukari tarayip
            // ilk opak satiri buluyoruz.
            for (int y = 0; y < tex.height; y++)
            {
                bool opaque = false;
                int row = y * tex.width;
                for (int x = 0; x < tex.width; x++)
                {
                    if (px[row + x].a > 8) { opaque = true; break; }
                }
                if (opaque)
                {
                    if (lowestOpaqueRow < 0 || y < lowestOpaqueRow) lowestOpaqueRow = y;
                    break;
                }
            }
        }

        if (usable.Count == 0 || cell <= 0)
        {
            report.Append($"\n\n📁 {name} — islenebilir sayfa yok.");
            RestoreReadable(restore);
            return;
        }

        float pivotY = lowestOpaqueRow > 0 ? (float)lowestOpaqueRow / cell : 0f;

        // ---- 3. Izgarayi ve pivotu uygula ----
        int sheets = 0, frames = 0;
        foreach (var (path, count) in usable)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            string baseName = Path.GetFileNameWithoutExtension(path);
            var meta = new List<SpriteMetaData>(count);

            for (int i = 0; i < count; i++)
            {
                meta.Add(new SpriteMetaData
                {
                    name      = $"{baseName}_{i}",
                    rect      = new Rect(i * cell, 0, cell, cell),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot     = new Vector2(0.5f, pivotY)
                });
            }

            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.spritesheet      = meta.ToArray();
            imp.isReadable       = restore.TryGetValue(path, out bool r) && r;

            EditorUtility.SetDirty(imp);
            imp.SaveAndReimport();

            sheets++;
            frames += count;
        }

        report.Append($"\n\n📁 {name} — {sheets} sayfa, {frames} kare")
              .Append($"\n   kare {cell}x{cell}, pivot (0.50, {pivotY:0.00})")
              .Append($"\n   ayaklar karenin altindan {lowestOpaqueRow} piksel yukarida");
    }

    static void RestoreReadable(Dictionary<string, bool> restore)
    {
        foreach (var kv in restore)
        {
            var imp = AssetImporter.GetAtPath(kv.Key) as TextureImporter;
            if (imp == null || imp.isReadable == kv.Value) continue;
            imp.isReadable = kv.Value;
            imp.SaveAndReimport();
        }
    }
}
