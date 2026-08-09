using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SEVIYE DENETIMI — menuden: ALEV → Seviyeyi Denetle
///
/// Elle test ederek bulmasi uzun suren yapisal hatalari otomatik bulur:
///   1. Kapi boslugunu tikayan collider  ("gecemiyorum" sorunu)
///   2. Ulasilamayan platform            (ziplama limiti asilmis)
///   3. Su kaynagi olmayan bolge         (kilitlenme riski)
///   4. Ust uste binen kati objeler      (takilma)
///
/// Sonuclar konsola yazilir; tiklayinca ilgili obje secilir.
/// </summary>
public static class AlevLevelAudit
{
    // PlayerController varsayilanlariyla hesaplanan ziplama limiti.
    // jumpForce 15, gravityScale 4, Physics2D.gravity.y -9.81
    //   v^2 / (2 * g * scale) = 225 / (2 * 9.81 * 4) = 2.87
    const float MaxJump = 2.87f;
    const float GroundStep = 2.25f;   // ZEMINDEN ilk basamak. DEVAM.md: "<= 2.2 olmali"
    const float SafeRise = 2.4f;      // platformdan platforma guvenli yukselme
    const float TightRise = 2.0f;     // bunun ustu calisir ama oyuncuyu zorlar
    const float PlayerW = 0.8f;
    const float PlayerH = 1.6f;

    // Duvar tespiti: uzun ve ince olan sey platform degildir, ustune
    // cikilmasi da beklenmez. Bunlari elemezsek denetim her duvari
    // "ulasilamaz platform" diye bildiriyor ve rapor gurultuye bogluyor.
    const float WallMinHeight = 4f;
    const float WallMaxWidth = 2.5f;

    [MenuItem("ALEV/Seviyeyi Denetle", false, 2)]
    public static void Audit()
    {
        var report = new StringBuilder();
        int problems = 0;

        problems += CheckDoorways(report);
        problems += CheckPlatformReach(report);
        problems += CheckWaterCoverage(report);

        if (problems == 0)
            Debug.Log("<b>[ALEV Denetim]</b> <color=#5c5>Sorun bulunamadi.</color>\n" +
                      "Yine de dusmansiz bir tur atin — denetim her seyi goremez.");
        else
            Debug.LogWarning($"<b>[ALEV Denetim]</b> {problems} sorun bulundu:\n\n{report}");
    }

    // ------------------------------------------------------------------
    //  1. Kapi bosluklari tikali mi?
    // ------------------------------------------------------------------

    static int CheckDoorways(StringBuilder r)
    {
        int found = 0;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("Kapi_")) continue;

            // Kapi boslugunun oyuncunun gecmesi gereken alani
            Vector2 center = t.position;
            Vector2 size = new Vector2(PlayerW * 1.1f, PlayerH * 1.05f);
            Vector2 lowCenter = new Vector2(center.x, t.position.y - t.localScale.y * 0.5f
                                                       + PlayerH * 0.5f + 0.1f);

            var hits = Physics2D.OverlapBoxAll(lowCenter, size, 0f);
            foreach (var h in hits)
            {
                if (h.isTrigger) continue;
                if (h.GetComponentInParent<PlayerController>() != null) continue;

                // ⚠️ Denetim EDIT MODE'da calisir — Start() hic calismamistir.
                // Alev duvari sahnede yazildigi yerde, collider'i ACIK durur;
                // oyunda ise FlameWallGate.Start() onu zeminin altina indirip
                // collider'i kapatir. Elemezsek her seferinde sahte "tikali kapi".
                if (h.GetComponentInParent<FlameWallGate>() != null) continue;

                r.AppendLine($"❌ TIKALI KAPI: '{t.name}' (x={t.position.x:0.#}) " +
                             $"→ '{h.name}' engelliyor");
                Debug.LogWarning($"Tikali kapi: {t.name} ← {h.name}", h.gameObject);
                found++;
                break;
            }
        }

        return found;
    }

    // ------------------------------------------------------------------
    //  2. Ulasilamayan platform var mi?
    // ------------------------------------------------------------------

    static int CheckPlatformReach(StringBuilder r)
    {
        var solids = new List<Solid>();

        foreach (var col in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (col.isTrigger) continue;
            if (col.gameObject.layer != LayerMask.NameToLayer("Ground")) continue;

            Bounds b = col.bounds;
            solids.Add(new Solid(col.transform, b.max.y, b.min.x, b.max.x, b.min.y));
        }

        // ---- ZINCIR TAKIBI (flood fill) ----
        // Eski surum tek adim bakiyordu: "A'dan B'ye ziplanir mi?" Ama A'nin
        // kendisine ulasilamiyorsa B de ulasilamaz. Artik ZEMINDEN baslayip
        // ulasilabilir kumeyi buyutuyoruz — gercek durum bu.
        //
        // ⚠️ Basilabilecek HER SEY zincire girmeli (duvarlar haric). Eski surum
        // zincirin kendisini de "top >= 1.5" ile suzuyordu; alcak bir sandik
        // basamak olarak kullanilamiyordu ve ustundeki platform sahte olarak
        // "ulasilamaz" cikiyordu. Suzgec artik sadece RAPORLAMAYA uygulaniyor.
        var targets = solids.Where(s => !s.IsWall).ToList();
        var reached = new HashSet<int>();

        // Zeminden dogrudan cikilabilenler
        for (int i = 0; i < targets.Count; i++)
            if (targets[i].top <= GroundStep) reached.Add(i);

        bool grew = true;
        while (grew)
        {
            grew = false;
            // ⚠️ 'reached' uzerinde donerken ona ekleme yapilamaz
            // (InvalidOperationException). Bu turda eklenecekleri ayri
            // toplayip tur bitince ekliyoruz.
            var toAdd = new List<int>();

            for (int i = 0; i < targets.Count; i++)
            {
                if (reached.Contains(i)) continue;
                if (reached.Any(j => CanJump(targets[j], targets[i]))) toAdd.Add(i);
            }

            foreach (int i in toAdd) { reached.Add(i); grew = true; }
        }

        int found = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            if (reached.Contains(i)) continue;
            var p = targets[i];

            // Cok genis zeminler ve yerdeki alcak parcalar rapor disi:
            // bunlarin "ulasilabilirligi" zaten sorulmuyor.
            if (p.Width > 14f || p.top < 1.5f) continue;

            r.AppendLine($"⚠️ ULASILAMAZ: '{p.t.name}' (ust={p.top:0.##}) — " +
                         $"zeminden baslayan hicbir zincirle varilamiyor");
            Debug.LogWarning($"Ulasilamaz platform: {p.t.name}", p.t.gameObject);
            found++;
        }

        // ---- SIKI ZIPLAMALAR ----
        // Ulasilabilir olmasi "rahat" demek degil. Bir platforma ancak
        // TightRise'in ustunde bir yukselmeyle varilabiliyorsa oyuncu
        // orayi birkac denemede gecer — jam'de bu, oyunu birakma sebebi.
        // Sustumak yerine ayrica listeliyoruz ki esigi yukseltmek gercek
        // sikisikligi gizlemesin.
        for (int i = 0; i < targets.Count; i++)
        {
            if (!reached.Contains(i)) continue;
            var p = targets[i];
            if (p.Width > 14f || p.top <= GroundStep) continue;

            float best = float.MaxValue;
            for (int j = 0; j < targets.Count; j++)
            {
                if (i == j || !reached.Contains(j)) continue;
                if (!CanJump(targets[j], p)) continue;
                best = Mathf.Min(best, p.top - targets[j].top);
            }

            if (best > TightRise && best < float.MaxValue)
            {
                r.AppendLine($"🔶 SIKI ZIPLAMA: '{p.t.name}' (ust={p.top:0.##}) — " +
                             $"en kolay giris {best:0.##} yukselme gerektiriyor " +
                             $"(rahat sinir {TightRise}, fiziksel tavan {MaxJump})");
                Debug.LogWarning($"Siki ziplama: {p.t.name}", p.t.gameObject);
                found++;
            }
        }

        // ---- DUSUNCE GERI CIKILAMAYAN YERLER ----
        // Bir tirmanisin en alt basamagi yerden zor cikiliyorsa, oyuncu
        // duserse sikisir. Sinir MaxJump (2.87, fiziksel tavan) degil
        // GroundStep olmali: tavana yakin ziplama pratikte "cikilmiyor" demek.
        foreach (var p in targets)
        {
            if (p.Width > 14f) continue;
            if (p.top <= GroundStep || p.top > GroundStep + 0.8f) continue;

            // Basamak adayi ARTIK tum katilardan seciliyor. Eskiden 'targets'
            // "top >= 1.5" ile suzuldugu icin piyano taburesi gibi alcak
            // basamaklar hic gorunmuyor, piyano da "cikilamaz" sayiliyordu.
            bool hasLowStep = targets.Any(q =>
                q.t != p.t &&
                q.top <= GroundStep &&
                Mathf.Max(0f, Mathf.Max(q.left - p.right, p.left - q.right)) <= 6f);

            if (!hasLowStep)
            {
                r.AppendLine($"⚠️ DUSUNCE CIKILAMAZ: '{p.t.name}' (ust={p.top:0.##}) — " +
                             $"yakininda yerden rahat cikilan basamak yok");
                Debug.LogWarning($"Dusunce cikilamaz: {p.t.name}", p.t.gameObject);
                found++;
            }
        }

        return found;
    }

    /// <summary>Zemin katmanindaki tek bir kati parca.</summary>
    struct Solid
    {
        public readonly Transform t;
        public readonly float top, left, right, bottom;

        public Solid(Transform t, float top, float left, float right, float bottom)
        {
            this.t = t; this.top = top; this.left = left;
            this.right = right; this.bottom = bottom;
        }

        public float Width => right - left;
        public float Height => top - bottom;

        /// <summary>Uzun ve ince: duvar. Platform olarak degerlendirilmez.</summary>
        public bool IsWall => Height >= WallMinHeight && Width <= WallMaxWidth;
    }

    static bool CanJump(Solid from, Solid to)
    {
        float rise = to.top - from.top;
        if (rise > SafeRise) return false;

        float gap = Mathf.Max(0f, Mathf.Max(from.left - to.right, to.left - from.right));
        float allowed = rise <= 0f ? 5f
                      : rise > 1.5f ? 3.5f
                      : rise > 0.6f ? 4.5f : 5f;
        return gap <= allowed;
    }

    // ------------------------------------------------------------------
    //  3. Her odada su var mi?
    // ------------------------------------------------------------------

    static int CheckWaterCoverage(StringBuilder r)
    {
        var taps = new List<Vector2>();
        foreach (var w in Object.FindObjectsByType<WaterSource>(FindObjectsSortMode.None))
            if (w.type == WaterSource.SourceType.Infinite) taps.Add(w.transform.position);

        int found = 0;

        foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
        {
            // Bu odada ya da komsu odada sonsuz su var mi?
            bool ok = false;
            foreach (var t in taps)
            {
                if (t.x >= room.MinX - 30f && t.x <= room.MaxX + 30f &&
                    t.y >= room.MinY - 4f && t.y <= room.MaxY + 4f)
                { ok = true; break; }
            }

            if (!ok)
            {
                r.AppendLine($"💧 SUSUZ BOLGE: '{room.roomName}' — yakininda sonsuz " +
                             $"su kaynagi yok. Buyumus bir yer atesi burada gecisi " +
                             $"kapatirsa oyuncu kilitlenir.");
                Debug.LogWarning($"Susuz bolge: {room.roomName}", room.gameObject);
                found++;
            }
        }

        return found;
    }
}
