using UnityEngine;

/// <summary>
/// Anahtar renkleri.
///
/// ⚠️ TURUNCU/KIRMIZI YOK. Bu oyunda turuncu "tehlike" demek (ates, uyari,
/// hasar). Anahtarlari sicak renk yaparsak oyuncu onlari tehdit saniyor.
/// Soguk renkler tehlikeyle karismaz ve karanlik odada iyi okunur.
/// </summary>
public enum KeyColor { Mavi, Yesil, Mor }

public static class KeyColors
{
    public static Color Of(KeyColor k) => k switch
    {
        KeyColor.Mavi  => new Color(0.35f, 0.70f, 1.00f),
        KeyColor.Yesil => new Color(0.40f, 0.90f, 0.55f),
        KeyColor.Mor   => new Color(0.72f, 0.50f, 1.00f),
        _              => Color.white
    };
}
