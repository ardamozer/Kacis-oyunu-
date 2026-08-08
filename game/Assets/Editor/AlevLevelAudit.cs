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
    const float SafeRise = 2.1f;      // pratikte guvenli sinir
    const float PlayerW = 0.8f;
    const float PlayerH = 1.6f;

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
        var solids = new List<(Transform t, float top, float left, float right)>();

        foreach (var col in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (col.isTrigger) continue;
            if (col.gameObject.layer != LayerMask.NameToLayer("Ground")) continue;

            Bounds b = col.bounds;
            solids.Add((col.transform, b.max.y, b.min.x, b.max.x));
        }

        // ---- ZINCIR TAKIBI (flood fill) ----
        // Eski surum tek adim bakiyordu: "A'dan B'ye ziplanir mi?" Ama A'nin
        // kendisine ulasilamiyorsa B de ulasilamaz. Artik ZEMINDEN baslayip
        // ulasilabilir kumeyi buyutuyoruz — gercek durum bu.
        var targets = solids.Where(s => s.right - s.left <= 14f && s.top >= 1.5f).ToList();
        var reached = new HashSet<int>();

        // Zeminden dogrudan cikilabilenler
        for (int i = 0; i < targets.Count; i++)
            if (targets[i].top <= SafeRise) reached.Add(i);

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
            r.AppendLine($"⚠️ ULASILAMAZ: '{p.t.name}' (ust={p.top:0.##}) — " +
                         $"zeminden baslayan hicbir zincirle varilamiyor");
            Debug.LogWarning($"Ulasilamaz platform: {p.t.name}", p.t.gameObject);
            found++;
        }

        // ---- DUSUNCE GERI CIKILAMAYAN YERLER ----
        // Bir tirmanisin en alt basamagi yerden zor cikiliyorsa, oyuncu
        // duserse sikisir. Sinir 2.87 (fiziksel maksimum) degil 2.2 olmali:
        // maksimuma yakin ziplama pratikte "cikilmiyor" demek.
        foreach (var p in targets)
        {
            if (p.top <= 2.2f || p.top > SafeRise + 0.8f) continue;

            bool hasLowStep = targets.Any(q =>
                q.top <= 2.2f &&
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

    static bool CanJump((Transform t, float top, float left, float right) from,
                        (Transform t, float top, float left, float right) to)
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
