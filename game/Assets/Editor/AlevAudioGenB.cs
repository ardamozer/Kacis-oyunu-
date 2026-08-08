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
///
/// ═══ YONTEM: KAYNAK-FILTRE SENTEZI ═══
///
/// Ilk surum sinus harmonikleri topluyordu ve flut gibi duyuluyordu.
/// Bir sesi "ses" yapan sey harmonikler degil, FORMANTLAR: gırtlak ve
/// agiz boslugunun rezonanslari. Gercek yontem ikiye ayirmak:
///
///   KAYNAK  — gırtlak darbesi (Rosenberg dalgasi). Sinus degil; keskin
///             kapanisi olan asimetrik bir darbe. Zengin harmonik uretir.
///   FILTRE  — 3 rezonansli bant geciren filtre (formant). Kaynaktaki
///             harmonikleri sekillendirip "agiz" karakterini verir.
///
/// Ustune iki kusur ekliyoruz, cunku KUSURSUZ olan sey canli duyulmuyor:
///   JITTER  — perdenin periyottan periyoda hafif oynamasi
///   SHIMMER — genligin hafif oynamasi
/// Bunlar olmadan ses "sentezlenmis" degil "makine" gibi cikiyor.
///
/// ⚠️ YINE DE YER TUTUCU. Gercek kopek kaydinin yerini tutmaz; insan
///    kulagi hayvan sesini cok iyi taniyor. Gercekleri freesound.org'dan
///    alip AYNI ISIMLERLE uzerine yazin, hicbir kod degismez.
///    Lisansi ASSETS.md'ye not edin.
/// </summary>
public static class AlevAudioGenB
{
    const string AudioFolder = "Assets/Audio/Generated";
    const int SampleRate = 44100;      // formant filtreleri icin 22k yetmiyordu

    [MenuItem("ALEV/Kisi B — Sesleri Uret", false, 23)]
    public static void GenerateAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Sesleri uret",
                "Assets/Audio/Generated/ altina 7 WAV yazilacak:\n\n" +
                "  DogWhine · DogBark · FireExtinguish\n" +
                "  FireCrackle · FlameWhoosh · DebrisCrash · GhostWhoosh\n\n" +
                "Bunlar YER TUTUCU. Gercek sesler gelince ayni isimle\n" +
                "uzerine yazabilirsiniz, kod degismez.\n\n" +
                "Var olan dosyalarin uzerine yazilir.",
                "Uret", "Vazgec"))
        {
            return;
        }

        EnsureFolder(AudioFolder);

        WriteWav(AudioFolder + "/DogWhine.wav", Normalize(MakeWhine(), 0.72f));
        WriteWav(AudioFolder + "/DogBark.wav", Normalize(MakeBark(), 0.88f));
        WriteWav(AudioFolder + "/FireExtinguish.wav", Normalize(MakeExtinguish(), 0.6f));
        WriteWav(AudioFolder + "/FireCrackle.wav", Normalize(MakeCrackle(), 0.45f));
        WriteWav(AudioFolder + "/FlameWhoosh.wav", Normalize(MakeWhoosh(), 0.65f));
        WriteWav(AudioFolder + "/DebrisCrash.wav", Normalize(MakeCrash(), 0.95f));
        WriteWav(AudioFolder + "/GhostWhoosh.wav", Normalize(MakeGhost(), 0.55f));

        AssetDatabase.Refresh();

        Debug.Log("<b>[ALEV-B]</b> Sesler uretildi (kaynak-filtre sentezi).\n" +
                  "Karabas'a baglamak icin: <b>ALEV → Kisi B — Prefablari Kur</b>\n" +
                  "⚠️ Yer tutucu — gercekleri freesound.org'dan alip ayni isimle degistirin.");
    }

    // ==================================================================
    //  KAYNAK — gırtlak darbesi
    // ==================================================================

    /// <summary>
    /// Rosenberg gırtlak darbesi. Bir periyot icinde:
    ///   acilma  — yumusak yukselis (ses telleri ayriliyor)
    ///   kapanma — daha hizli dusus (keskin kapanis)
    /// Bu asimetri harmonik zenginligi uretir; sinusun uretemedigi sey bu.
    /// </summary>
    static float GlottalPulse(float phase, float openQuotient)
    {
        float open = openQuotient * 0.62f;      // acilma bolumu
        float close = openQuotient * 0.38f;     // kapanma bolumu (daha kisa)

        if (phase < open)
        {
            float t = phase / open;
            return 3f * t * t - 2f * t * t * t;         // yumusak yukselis
        }

        if (phase < open + close)
        {
            float t = (phase - open) / close;
            return 1f - t * t;                          // hizli dusus
        }

        return 0f;                                      // kapali faz — sessizlik
    }

    // ==================================================================
    //  FILTRE — rezonansli bant geciren (biquad, RBJ cookbook)
    // ==================================================================

    struct Biquad
    {
        float b0, b1, b2, a1, a2;
        float x1, x2, y1, y2;

        public static Biquad BandPass(float freq, float q, int sampleRate)
        {
            var f = new Biquad();

            float w0 = 2f * Mathf.PI * freq / sampleRate;
            float cos = Mathf.Cos(w0);
            float alpha = Mathf.Sin(w0) / (2f * q);

            // Sabit tepe kazancli bant geciren.
            float a0 = 1f + alpha;
            f.b0 = alpha / a0;
            f.b1 = 0f;
            f.b2 = -alpha / a0;
            f.a1 = -2f * cos / a0;
            f.a2 = (1f - alpha) / a0;

            return f;
        }

        public float Process(float x)
        {
            float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;

            x2 = x1; x1 = x;
            y2 = y1; y1 = y;

            return y;
        }
    }

    // ==================================================================
    //  SESLER
    // ==================================================================

    /// <summary>
    /// Sikismis kopek iniltisi.
    ///
    /// Inilti burundan gelen, tiz, guclu vibratolu bir ses. Nazal karakter
    /// icin alcak birinci formant + guclu ikinci formant kullaniyoruz.
    /// Perde konturu duz inis degil: once yukselir, sonra kirilarak duser —
    /// duz inen bir kayma "siren" gibi duyuluyor.
    /// </summary>
    static float[] MakeWhine()
    {
        const float dur = 1.25f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        // Nazal formantlar. F1 alcak + F2 yuksek = "burundan" karakteri.
        var f1 = Biquad.BandPass(520f, 7f, SampleRate);
        var f2 = Biquad.BandPass(1950f, 9f, SampleRate);
        var f3 = Biquad.BandPass(3100f, 11f, SampleRate);

        float phase = 0f;
        float jitter = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float k = t / dur;

            // Perde konturu: hizli yukselis, uzun kirilarak inis.
            float f0 = k < 0.18f
                ? Mathf.Lerp(540f, 780f, k / 0.18f)
                : Mathf.Lerp(780f, 430f, Mathf.Pow((k - 0.18f) / 0.82f, 0.75f));

            // Vibrato — inleyen bir sesin en belirgin ozelligi.
            f0 *= 1f + Mathf.Sin(t * Mathf.PI * 2f * 6.2f) * 0.045f;

            // Jitter: rastgele yuruyus. Kusursuz periyot makine gibi duyulur.
            jitter += (Random.value * 2f - 1f) * 0.0035f;
            jitter = Mathf.Clamp(jitter, -0.02f, 0.02f);
            f0 *= 1f + jitter;

            phase += f0 / SampleRate;
            if (phase >= 1f) phase -= 1f;

            float src = GlottalPulse(phase, 0.62f);

            // Nefes gurultusu — kaynaga karisir, filtrelerden birlikte gecer.
            src += (Random.value * 2f - 1f) * 0.06f;

            float v = f1.Process(src) * 1.0f
                    + f2.Process(src) * 0.75f
                    + f3.Process(src) * 0.28f;

            // Shimmer: genlikte hafif oynama.
            float shimmer = 1f + Mathf.Sin(t * 41f) * 0.04f;

            s[i] = v * Envelope(k, 0.12f, 0.30f) * shimmer;
        }

        return s;
    }

    /// <summary>
    /// Kisa mutlu havlama.
    ///
    /// Gercek havlamanin uc bolumu var:
    ///   1. hava patlamasi — genis bantli gurultu, cok kisa
    ///   2. sesli bolum    — gırtlak darbesi + formantlar, perde once
    ///                       yukselip sonra duser
    ///   3. puruzlenme     — sonda alcak frekansli genlik modulasyonu,
    ///                       "hirilti" dokusu
    /// Sadece dusen ton yazarsaniz havlama degil "uuf" cikiyor.
    /// </summary>
    static float[] MakeBark()
    {
        const float dur = 0.30f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        // Kopek agzi insandan buyuk ve acik — formantlar daha alcak/genis.
        var f1 = Biquad.BandPass(420f, 5f, SampleRate);
        var f2 = Biquad.BandPass(1150f, 6f, SampleRate);
        var f3 = Biquad.BandPass(2400f, 7f, SampleRate);

        float phase = 0f;
        float jitter = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float k = t / dur;

            // Perde: cok hizli yukselip uzun uzun duser.
            float f0 = k < 0.07f
                ? Mathf.Lerp(210f, 340f, k / 0.07f)
                : Mathf.Lerp(340f, 145f, Mathf.Pow((k - 0.07f) / 0.93f, 0.6f));

            jitter += (Random.value * 2f - 1f) * 0.008f;
            jitter = Mathf.Clamp(jitter, -0.045f, 0.045f);
            f0 *= 1f + jitter;

            phase += f0 / SampleRate;
            if (phase >= 1f) phase -= 1f;

            float src = GlottalPulse(phase, 0.55f);

            // 1. Hava patlamasi — sadece ilk anlarda, cok hizli soner.
            src += (Random.value * 2f - 1f) * Mathf.Exp(-k * 45f) * 0.85f;

            // Surekli hafif nefes.
            src += (Random.value * 2f - 1f) * 0.05f;

            float v = f1.Process(src) * 1.0f
                    + f2.Process(src) * 0.62f
                    + f3.Process(src) * 0.3f;

            // 3. Puruzlenme: sonlara dogru artan alcak frekansli modulasyon.
            float rough = 1f - Mathf.Clamp01((k - 0.45f) / 0.55f) * 0.35f
                             * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * 58f));

            // Zarf: cok keskin atak, ustel inis.
            float env = Mathf.Min(k / 0.006f, 1f) * Mathf.Exp(-k * 4.2f);

            s[i] = v * env * rough;
        }

        return s;
    }

    /// <summary>
    /// Ates sonerken cikan "pssss".
    ///
    /// Buhar = genis bantli gurultu, ama duz beyaz gurultu "cccc" gibi
    /// dijital duyuluyor. Iki sey ekliyoruz: zamanla kapanan alcak geciren
    /// filtre (buhar dagilirken boguklasir) ve genis bir rezonans
    /// (fiskiran deligin karakteri).
    /// </summary>
    static float[] MakeExtinguish()
    {
        const float dur = 0.7f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        var body = Biquad.BandPass(1400f, 1.1f, SampleRate);
        float low = 0f;

        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;

            float noise = Random.value * 2f - 1f;

            // Alcak geciren: kesim frekansi zamanla dusuyor.
            float cutoff = Mathf.Lerp(0.75f, 0.10f, k * k);
            low += (noise - low) * cutoff;

            // Gurultunun bir kismi rezonanstan gecsin — karakter kazansin.
            float v = low * 0.65f + body.Process(low) * 0.9f;

            // Ani baslangic, once hizli sonra yavas sonme.
            float env = Mathf.Min(k / 0.015f, 1f) * Mathf.Exp(-k * 3.6f);

            s[i] = v * env;
        }

        return s;
    }

    /// <summary>
    /// Ates citirtisi — dongu icin. Iki katman:
    ///   gurultu  — alcak frekansli surekli ugultu (yanmanin "govdesi")
    ///   citirti  — rastgele anlarda kisa, tiz patlamalar (tahta catlamasi)
    /// Sadece ugultu "rüzgar", sadece citirti "statik" gibi duyuluyor.
    /// Ikisi birlikte ates oluyor.
    /// </summary>
    static float[] MakeCrackle()
    {
        const float dur = 2.4f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        var pop = Biquad.BandPass(2600f, 3.5f, SampleRate);
        float low = 0f;
        float popEnv = 0f;

        for (int i = 0; i < n; i++)
        {
            float noise = Random.value * 2f - 1f;

            // Govde: agir alcak geciren -> bogus ugultu.
            low += (noise - low) * 0.06f;

            // Citirti: rastgele tetiklenip hizla sonen kisa patlamalar.
            if (Random.value < 0.0022f) popEnv = 1f;
            popEnv *= 0.9986f;
            popEnv -= 0.0004f;
            if (popEnv < 0f) popEnv = 0f;

            float crack = pop.Process(noise) * popEnv * popEnv * 2.2f;

            // Dongu dikisi duyulmasin diye iki ucta kisa fade.
            float k = (float)i / n;
            float loopFade = Mathf.Min(Mathf.Clamp01(k / 0.03f), Mathf.Clamp01((1f - k) / 0.03f));

            s[i] = (low * 0.5f + crack) * loopFade;
        }

        return s;
    }

    /// <summary>
    /// Alev firlatma — kisa "fuu". Asagi kayan bant geciren gurultu.
    /// Sabit filtreli gurultu "ss" gibi duruyor; kayma ona hareket veriyor.
    /// </summary>
    static float[] MakeWhoosh()
    {
        const float dur = 0.35f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        float low = 0f;

        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;

            float noise = Random.value * 2f - 1f;

            // Kesim once acilip sonra kapanir — "fuuu" konturu.
            float cutoff = k < 0.25f
                ? Mathf.Lerp(0.08f, 0.5f, k / 0.25f)
                : Mathf.Lerp(0.5f, 0.06f, (k - 0.25f) / 0.75f);

            low += (noise - low) * cutoff;

            s[i] = low * Mathf.Min(k / 0.02f, 1f) * Mathf.Exp(-k * 2.8f);
        }

        return s;
    }

    /// <summary>
    /// Dusen tahtanin carpmasi — Karabas'in olum ani.
    ///
    /// Uc katman, hepsi ayni anda:
    ///   gumleme — cok alcak sinus, hizla dusen perde (kutlenin agirligi)
    ///   catirti — orta bantli gurultu patlamasi (tahtanin kirilmasi)
    ///   dagilma — sonda uzayan bogus gurultu (enkazin savrulmasi)
    /// </summary>
    static float[] MakeCrash()
    {
        const float dur = 1.0f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        var wood = Biquad.BandPass(900f, 2.2f, SampleRate);
        float phase = 0f;
        float low = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float k = t / dur;

            // 1) Gumleme: 95 Hz'den 40 Hz'e dusen sinus. Alcak = agir.
            float f = Mathf.Lerp(95f, 40f, Mathf.Clamp01(k * 5f));
            phase += f / SampleRate;
            float thump = Mathf.Sin(phase * Mathf.PI * 2f) * Mathf.Exp(-k * 9f) * 0.9f;

            float noise = Random.value * 2f - 1f;

            // 2) Catirti: sadece ilk anlarda, tahtanin kirilmasi.
            float crack = wood.Process(noise) * Mathf.Exp(-k * 22f) * 1.6f;

            // 3) Dagilma: bogus, uzun kuyruk.
            low += (noise - low) * 0.09f;
            float rubble = low * Mathf.Exp(-k * 3.4f) * 0.5f;

            s[i] = thump + crack + rubble;
        }

        return s;
    }

    /// <summary>
    /// Karabas'in hayaleti gecerken — oyunun duygusal zirvesi.
    ///
    /// Bilerek "korku filmi" degil: yukari kayan yumusak gurultu +
    /// ustunde titresen tiz bir ton. Sicak ve hafif, tehditkar degil.
    /// PLAN.md: sahne sessiz ve yumusak olmali, urkutucu degil.
    /// </summary>
    static float[] MakeGhost()
    {
        const float dur = 2.2f;
        int n = (int)(SampleRate * dur);
        var s = new float[n];

        var air = Biquad.BandPass(1100f, 1.4f, SampleRate);
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float k = t / dur;

            // Yumusak hava akimi, yukari dogru kayan.
            float noise = Random.value * 2f - 1f;
            float airy = air.Process(noise) * 0.55f;

            // Ustunde titresen tiz ton — "parlama" hissi.
            float f = Mathf.Lerp(880f, 1320f, k);
            f *= 1f + Mathf.Sin(t * Mathf.PI * 2f * 4.5f) * 0.02f;
            phase += f / SampleRate;

            float shimmer = (Mathf.Sin(phase * Mathf.PI * 2f) * 0.6f
                           + Mathf.Sin(phase * Mathf.PI * 4f) * 0.2f) * 0.35f;

            // Iki ucta da yumusak girip cikmali — aniden baslarsa urkutur.
            float env = Mathf.Min(Mathf.Clamp01(k / 0.3f), Mathf.Clamp01((1f - k) / 0.35f));

            s[i] = (airy + shimmer) * env;
        }

        return s;
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================

    static float Envelope(float k, float attack, float release)
    {
        float a = Mathf.Clamp01(k / attack);
        float r = Mathf.Clamp01((1f - k) / release);
        return a * r * r;      // cikis egrisi kare — daha dogal sonme
    }

    /// <summary>
    /// Tepe degerini hedefe cekiyor. Filtrelerin kazanci onceden
    /// bilinemedigi icin sabit carpan kullanmak ya kisik ya kirpilmis
    /// ses veriyordu.
    /// </summary>
    static float[] Normalize(float[] samples, float target)
    {
        float peak = 0f;
        foreach (float v in samples) peak = Mathf.Max(peak, Mathf.Abs(v));

        if (peak < 1e-5f) return samples;

        float gain = target / peak;
        for (int i = 0; i < samples.Length; i++) samples[i] *= gain;

        return samples;
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
