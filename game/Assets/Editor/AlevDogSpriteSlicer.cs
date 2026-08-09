using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// KOPEK SAYFALARINI DILIMLE — menuden: ALEV → Kopek Sayfalarini Dilimle
///
/// Assets/Art/Imported/Dog/ altindaki tum PNG'leri 100x100 izgaraya boler
/// ve pivotu KOPEGIN AYAKLARINA koyar.
///
/// ⚠️ PIVOT NEDEN AYAKLARDA (0.5, 0.35):
/// Kareler 100x100 ama kopek sadece ~29 piksel; altinda 35 piksel saydam
/// bosluk var (11 sayfanin HEPSINDE ayni olcuLdu). Pivot ortada kalirsa:
///   1. DogCompanion.groundOffset'i sprite.bounds.extents.y'den hesapliyor;
///      bounds bos boslugu da sayar, kopek havada durur.
///   2. Sevme animasyonundaki ezilme localScale ile yapiliyor; merkez
///      pivotta ezilen kopek yere gomulur.
/// Pivot ayaklara alininca ikisi de kendiliginden cozulur ve
/// groundOffsetOverride = 0 verilebilir.
///
/// 35/100 = 0.35  (pivot Y, karenin ALTINDAN olculur)
/// </summary>
public static class AlevDogSpriteSlicer
{
    const string Folder = "Assets/Art/Imported/Dog";
    const int Cell = 100;

    /// <summary>Ayaklarin kare altindan yuksekligi / kare yuksekligi.</summary>
    const float PivotY = 0.35f;

    [MenuItem("ALEV/Kopek Sayfalarini Dilimle", false, 3)]
    public static void Slice()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("ALEV",
                $"{Folder} altinda PNG bulunamadi.\n\n" +
                "Kopek paketini once o klasore kopyala.", "Tamam");
            return;
        }

        int sheets = 0, frames = 0;
        var log = new System.Text.StringBuilder();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            // Yukseklik 100 degilse bu sayfa bu izgaraya uymuyor — dokunma.
            if (tex.height != Cell)
            {
                log.Append($"\n  ✖ {System.IO.Path.GetFileName(path)} — yukseklik " +
                           $"{tex.height}, beklenen {Cell}. ATLANDI.");
                continue;
            }

            int count = tex.width / Cell;
            if (count < 1 || tex.width % Cell != 0)
            {
                log.Append($"\n  ✖ {System.IO.Path.GetFileName(path)} — genislik " +
                           $"{tex.width}, {Cell}'un tam kati degil. ATLANDI.");
                continue;
            }

            string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            var meta = new List<SpriteMetaData>(count);

            for (int i = 0; i < count; i++)
            {
                meta.Add(new SpriteMetaData
                {
                    name      = $"{baseName}_{i}",
                    rect      = new Rect(i * Cell, 0, Cell, Cell),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot     = new Vector2(0.5f, PivotY)
                });
            }

            imp.textureType       = TextureImporterType.Sprite;
            imp.spriteImportMode  = SpriteImportMode.Multiple;
            imp.spritesheet       = meta.ToArray();

            EditorUtility.SetDirty(imp);
            imp.SaveAndReimport();

            sheets++;
            frames += count;
            log.Append($"\n  ✔ {baseName} — {count} kare");
        }

        AssetDatabase.Refresh();

        Debug.Log($"<b>[ALEV Dilimleme]</b> {sheets} sayfa, {frames} kare. " +
                  $"Pivot (0.5, {PivotY}) — ayaklarda.{log}");
    }
}
