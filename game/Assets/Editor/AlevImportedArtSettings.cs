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

    /// <summary>
    /// Klasor basina PPU ve (tek kare sprite'lar icin) pivot Y.
    /// pivotY &lt; 0 → pivota dokunma (sayfalar dilimleyicide ayarlaniyor).
    ///
    /// ⚠️ NEDEN KOPEK 32, ITFAIYECI 16:
    /// Iki paket FARKLI cozunurlukte cizilmis. Ikisinin de gorunen govdesi
    /// 29 piksel — ama biri kopek, digeri insan. Ayni PPU verilirse kopek
    /// adamla AYNI BOYDA olur ve kurgu aninda cokuyor.
    ///   itfaiyeci 29px / 16 = 1.81 birim  (collider 1.60, dogru)
    ///   kopek     29px / 32 = 0.91 birim  (adamin yarisi, Saint Bernard icin dogru)
    ///
    /// Bedeli: kopegin pikselleri itfaiyecininkinin yarisi boyutunda.
    /// Kaynak sanat farkli yogunlukta oldugu icin DOGRU ORAN ile AYNI
    /// PIKSEL BOYUTU ayni anda elde edilemez; oran secildi. Bir kopegin
    /// adam boyunda olmasi, piksel boyutu farkindan cok daha goze batar.
    /// Ev/platformlar zaten dokusuz dikdortgen, onlarla catisma yok.
    /// </summary>
    static readonly (string folder, int ppu, float pivotY)[] PerFolder =
    {
        ("Dog/",     32, -1f),    // sayfa halinde; pivotu dilimleyici koyuyor
        ("Fireman/", 16,  0f),    // tek kare dosyalar; alt bosluk 0 olculdu

        // ⚠️ 367 keyfi degil, ESKI ALEVDEN hesaplandi.
        // Eski Flame.png 64x96 @ PPU 100 → dogal boyut 0.64 x 0.96 birim.
        // Sahnedeki 36 atesin localScale'i o dogal boyuta gore ayarlanmis.
        // Yeni karenin icerigi 256x352 px; ayni dogal YUKSEKLIGI vermesi
        // icin PPU = 352 / 0.96 = 367. Boylece butun ates olcekleri
        // oldugu gibi calisiyor ve hicbir ates yeniden konumlandirilmiyor.
        ("Fire/",   367, -1f),
    };

    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(Root)) return;

        string rest = path.Substring(Root.Length);
        int ppu = PixelsPerUnit;
        float pivotY = -1f;

        foreach (var (folder, p, pv) in PerFolder)
        {
            if (rest.StartsWith(folder)) { ppu = p; pivotY = pv; break; }
        }

        var imp = (TextureImporter)assetImporter;

        imp.textureType         = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode          = FilterMode.Point;              // bulaniklik olmasin
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled       = false;                         // 2D'de mipmap bulandirir
        imp.alphaIsTransparency = true;
        imp.wrapMode            = TextureWrapMode.Clamp;         // kenar sizmasi olmasin
        imp.npotScale           = TextureImporterNPOTScale.None;  // sayfa boyutu bozulmasin

        // pivotY < 0 → SAYFA klasoru (Dog gibi). spriteImportMode'a
        // dokunmuyoruz: elle dilimlenmis bir sayfa her yeniden iceri
        // alinista Single'a donerse tum dilimler gider.
        //
        // pivotY >= 0 → TEK KARE dosyalar (Fireman gibi). Burada Single
        // ZORLANIR.
        // ⚠️ Onceki surum sadece "zaten Single ise" pivot yaziyordu ve
        // dosyalar Multiple modunda geldigi icin pivot hic uygulanmadi:
        // merkez pivotla sprite'in ortasi collider tabanina oturdu,
        // itfaiyecinin alt yarisi zemine gomuldu.
        if (pivotY >= 0f)
        {
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);

            s.spriteMode      = (int)SpriteImportMode.Single;
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot     = new Vector2(0.5f, pivotY);

            imp.SetTextureSettings(s);
        }
    }
}
