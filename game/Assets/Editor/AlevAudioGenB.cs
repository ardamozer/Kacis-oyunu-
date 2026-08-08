using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// KISI B'NIN SES DOSYALARINI KODLA URETIR.
///
/// Menu: ALEV → Kisi B — Sesleri Uret
///
/// NEDEN VAR: projede tek bir ses dosyasi yok. PLAN.md havlama sesini
/// "ASLA kesilmeyecekler" listesine koymus ve oyunun ilk 30 saniyesi
/// tamamen ona dayaniyor — oyuncu kopegi GORMEDEN sesini duyup ariyor.
/// Ses indirmeyi bekleyip o bolumu hic test edememektense, calisan bir
/// yer tutucu uretiyoruz.
///
/// ⚠️ BUNLAR YER TUTUCU. Ozellikle havlama/inleme sentezle ikna edici
///    olmuyor — insan kulagi hayvan sesini cok iyi taniyor. Gercekleri
///    freesound.org'dan alinca AYNI ISIMLERLE uzerine yazin, hicbir kod
///    degismesi gerekmez. Lisansi ASSETS.md'ye not etmeyi unutmayin.
///
/// Uretilenler (Assets/Audio/Generated/):
///   DogWhine.wav        sikismis Karabas — uzaktan duyulan inilti
///   DogBark.wav         mutlu havlama (kurtarilinca + sevilince)
///   FireExtinguish.wav  ates sonerken "pssss"
/// </summary>
public static class AlevAudioGenB
{
    const string AudioFolder = "Assets/Audio/Generated";
    const int SampleRate = 22050;      // konusma/efekt icin fazlasiyla yeterli

    [MenuItem("ALEV/Kisi B — Sesleri Uret", false, 23)]
    public static void GenerateAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Sesleri uret",
                "Assets/Audio/Generated/ altina 3 WAV yazilacak:\n\n" +
                "  DogWhine.wav\n  DogBark.wav\n  FireExtinguish.wav\n\n" +
                "Bunlar YER TUTUCU. Gercek sesler gelince ayni isimle\n" +
                "uzerine yazabilirsiniz, kod degismez.\n\n" +
                "Var olan dosyalarin uzerine yazilir.",
                "Uret", "Vazgec"))
        {
            return;
        }

        EnsureFolder(AudioFolder);

        WriteWav(AudioFolder + "/DogWhine.wav", MakeWhine());
        WriteWav(AudioFolder + "/DogBark.wav", MakeBark());
        WriteWav(AudioFolder + "/FireExtinguish.wav", MakeExtinguish());

        AssetDatabase.Refresh();

        Debug.Log("<b>[ALEV-B]</b> Sesler uretildi: DogWhine, DogBark, FireExtinguish.\n" +
                  "Karabas prefabina baglamak icin: <b>ALEV → Kisi B — Prefablari Kur</b>\n" +
                  "⚠️ Bunlar yer tutucu — gercekleri freesound.org'dan alip ayni isimle degistirin.");
    }

    // ==================================================================
    //  Sentez
    // ==================================================================

    /// <summary>
    /// Sikismis kopek iniltisi.
    ///
    /// Inilti = tiz baslayip alcalan, vibratolu, burundan gelen bir ton.
    /// Sabit perdeli sinus "canli" duyulmuyor, o yuzden hem kayan bir
    /// temel frekans hem vibrato hem de harmonikler var.
    /// </summary>
    static float[] MakeWhine()
    {
        const float dur = 1.15f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float k = t / dur;

            // Once hafif yukselip sonra dusen perde — "iiiiii-yuuu"
            float f = Mathf.Lerp(620f, 760f, Mathf.Clamp01(k / 0.25f));
            if (k > 0.25f) f = Mathf.Lerp(760f, 400f, (k - 0.25f) / 0.75f);

            // Vibrato: canli bir ses asla sabit perdede durmaz.
            f *= 1f + Mathf.Sin(t * Mathf.PI * 2f * 5.5f) * 0.035f;

            phase += f / SampleRate;

            // Harmonikler — tek sinus "flut", harmonikli ton "ses" gibi duyulur.
            float v = Mathf.Sin(phase * Mathf.PI * 2f) * 0.55f
                    + Mathf.Sin(phase * Mathf.PI * 4f) * 0.28f
                    + Mathf.Sin(phase * Mathf.PI * 6f) * 0.12f;

            // Nefes gurultusu — tamamen temiz ton makine gibi duyulur.
            v += (Random.value * 2f - 1f) * 0.045f;

            s[i] = v * Envelope(k, 0.14f, 0.35f) * 0.5f;
        }

        return s;
    }

    /// <summary>
    /// Kisa mutlu havlama. "Hav" = ani gurultu patlamasi + hizla dusen
    /// alcak ton. Atak cok kisa olmali, yoksa havlama degil "uuf" olur.
    /// </summary>
    static float[] MakeBark()
    {
        const float dur = 0.22f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float k = t / dur;

            // Perde hizla duser — havlamanin karakteri bu.
            float f = Mathf.Lerp(320f, 130f, k * k);
            phase += f / SampleRate;

            float tone = Mathf.Sin(phase * Mathf.PI * 2f) * 0.5f
                       + Mathf.Sin(phase * Mathf.PI * 4f) * 0.3f
                       + Mathf.Sin(phase * Mathf.PI * 6f) * 0.18f;

            // Gurultu bileseni basta cok, sonda az — "h" sesi.
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-k * 14f) * 0.5f;

            // Cok kisa atak (0.008), uzun cikis: havlamanin zarfı.
            float env = Mathf.Min(k / 0.008f, 1f) * Mathf.Exp(-k * 5.5f);

            s[i] = (tone + noise) * env * 0.62f;
        }

        return s;
    }

    /// <summary>
    /// Ates sonerken cikan "pssss". Filtrelenmis beyaz gurultu —
    /// alcak geciren filtre olmadan "cccc" gibi tiz ve dijital duyuluyor.
    /// </summary>
    static float[] MakeExtinguish()
    {
        const float dur = 0.55f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        float low = 0f;

        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;

            float noise = Random.value * 2f - 1f;

            // Tek kutuplu alcak geciren filtre. Katsayi zamanla dusuyor:
            // ses once tiz (buhar fiskiriyor), sonra boguklasip sonuyor.
            float cutoff = Mathf.Lerp(0.55f, 0.12f, k);
            low += (noise - low) * cutoff;

            s[i] = low * Mathf.Exp(-k * 3.2f) * 0.55f;
        }

        return s;
    }

    /// <summary>Basit atak / cikis zarfi. Ortada duz kalir.</summary>
    static float Envelope(float k, float attack, float release)
    {
        float a = Mathf.Clamp01(k / attack);
        float r = Mathf.Clamp01((1f - k) / release);
        return a * r;
    }

    // ==================================================================
    //  WAV yazimi (16-bit PCM, mono)
    // ==================================================================

    static void WriteWav(string path, float[] samples)
    {
        const int channels = 1;
        const int bits = 16;

        int dataSize = samples.Length * channels * bits / 8;

        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));

            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);                                   // fmt blok boyutu
            bw.Write((short)1);                             // 1 = PCM
            bw.Write((short)channels);
            bw.Write(SampleRate);
            bw.Write(SampleRate * channels * bits / 8);     // byte/sn
            bw.Write((short)(channels * bits / 8));         // blok hizalama
            bw.Write((short)bits);

            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            foreach (float sample in samples)
                bw.Write((short)(Mathf.Clamp(sample, -1f, 1f) * 32767f));
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
