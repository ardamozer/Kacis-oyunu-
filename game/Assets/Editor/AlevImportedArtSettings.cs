using UnityEditor;
using UnityEngine;

/// <summary>
/// INDIRILEN PIXEL ART ICIN OTOMATIK IMPORT AYARI
///
/// Sadece <c>Assets/Art/Imported/</c> altindaki dosyalari etkiler.
/// Oraya bir PNG surukledigin anda dogru ayarlarla iceri alinir;
/// Inspector'da tek tek ugrasmana gerek kalmaz.
///
/// ⚠️ NEDEN KLASORE BAGLI, TUM Art/ KLASORUNE DEGIL:
/// Assets/Art/Generated/ altindaki sprite'lar kodla uretiliyor ve
/// HEPSI 16 DEGIL — Flame, Glow ve Vignette bilerek PPU=100.
/// Onlara 16 dayatilsaydi alevler ve atmosfer vinyeti 6 kat buyurdu.
///
/// ⚠️ PPU'NUN TUM INDIRILEN ASSETLERDE AYNI OLMASI SART.
/// Farkli olursa itfaiyeci ile kopek farkli piksel yogunlugunda gorunur
/// ve "yamali bohca" hissi verir — asset karistirmanin en buyuk riski
/// budur.
///
/// ⚠️ NEDEN 32, kodla uretilenlerin 16'si degil:
/// Kodla uretilen sprite'lar duz dikdortgen; piksel detaylari yok, yani
/// onlarla piksel yogunlugu karsilastirmasi anlamsiz. Belirleyici olan
/// indirilen pixel art'in KENDI arasindaki tutarliligi.
/// Kopek sayfalari 100x100 kare, gorunen govde ~29 piksel:
///   PPU 16 → 1.81 birim (oyuncu 1.60 — kopek adamdan uzun, yanlis)
///   PPU 32 → 0.91 birim (oyuncunun beline geliyor, dogru)
/// Itfaiyeci eklenirken kontrol et: govde yuksekligi / 32 ≈ 1.6 cikmali,
/// yani kaynak sprite'ta ~51 piksel olmali. Cok saparsa ikisini birden
/// ayni yeni PPU'ya tasi — birini olceklemekle COZME.
/// </summary>
public class AlevImportedArtSettings : AssetPostprocessor
{
    public const int PixelsPerUnit = 32;

    const string Root = "Assets/Art/Imported/";

    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(Root)) return;

        var imp = (TextureImporter)assetImporter;

        imp.textureType         = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = PixelsPerUnit;
        imp.filterMode          = FilterMode.Point;              // bulaniklik olmasin
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled       = false;                         // 2D'de mipmap bulandirir
        imp.alphaIsTransparency = true;
        imp.wrapMode            = TextureWrapMode.Clamp;         // kenar sizmasi olmasin
        imp.npotScale           = TextureImporterNPOTScale.None;  // sayfa boyutu bozulmasin

        // ⚠️ spriteImportMode'a KASITLI OLARAK DOKUNULMUYOR.
        // Elle dilimlenmis (Sprite Editor → Slice) bir sayfa her
        // yeniden iceri alinista Single'a donerse tum dilimler gider.
    }
}
