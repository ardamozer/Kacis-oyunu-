using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Her odanin cikisina bir alev perdesi koyar — SU ULASILABILIRSE.
///
/// Menu: ALEV → Alev Perdelerini Kur
///
/// ⚠️ ASIL ISI YERLESTIRMEK DEGIL, KILITLENMEYI ONLEMEK.
/// Perde sadece suyla soniyor. Oyuncu susuz gelirse ve geri donup
/// dolduramiyorsa oyun COZULEMEZ hale gelir. Bu, PLAN.md'nin en cok
/// uyardigi hata tipi.
///
/// Kritik yer: x=188'deki alev duvari, Karabas kurtulunca yukseliyor ve
/// geri donusu KAPATIYOR. O noktadan sonraki her perdenin, ayni segment
/// icinde ve KENDINDEN ONCE bir su kaynagi olmali. Yoksa perde konmuyor.
///
/// Komut her odayi tek tek denetleyip Console'a rapor yaziyor:
/// hangi odaya kondu, hangisine neden konmadi.
/// </summary>
public static class AlevCurtainSetup
{
    const string HolderName = "AlevPerdeleri";
    const string CurtainSprite = "Assets/Art/Generated/WhiteSquare.png";

    [MenuItem("ALEV/Alev Perdelerini Kur", true)]
    static bool Validate() => !EditorApplication.isPlaying;

    [MenuItem("ALEV/Alev Perdelerini Kur", false, 6)]
    public static void Build()
    {
        Room[] rooms = Object.FindObjectsByType<Room>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (rooms.Length == 0)
        {
            EditorUtility.DisplayDialog("Oda yok",
                "Sahnede Room bileseni yok.\n\nOnce: ALEV → Evi Kur", "Tamam");
            return;
        }

        // ---- Su kaynaklarinin x konumlari ----
        var waters = new List<float>();
        foreach (WaterSource w in Object.FindObjectsByType<WaterSource>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Tek kullanimlik kaynaga guvenmiyoruz: oyuncu onu erken
            // harcamis olabilir ve geri geldiginde bos bulur.
            if (w.type == WaterSource.SourceType.Infinite)
                waters.Add(w.transform.position.x);
        }
        waters.Sort();

        // ---- Geri donusu kapatan noktalar ----
        // Bu x'lerin otesine gecen oyuncu geri donemez.
        var barriers = new List<float>();
        foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            CollectBarriers(go.transform, barriers);
        barriers.Sort();

        GameObject old = GameObject.Find(HolderName);
        if (old != null) Object.DestroyImmediate(old);

        Transform holder = new GameObject(HolderName).transform;

        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(CurtainSprite);
        AudioClip hiss = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/Generated/FireExtinguish.wav");
        int groundMask = LayerMask.GetMask("Ground");

        var sorted = new List<Room>(rooms);
        sorted.Sort((a, b) => a.MinX.CompareTo(b.MinX));

        var report = new StringBuilder();
        int placed = 0, skipped = 0;

        for (int i = 0; i < sorted.Count; i++)
        {
            Room room = sorted[i];

            // Son odanin cikisinda boss var, perde koymuyoruz.
            if (i == sorted.Count - 1)
            {
                report.Append("\n  – ").Append(room.roomName)
                      .Append(" — son oda, cikista boss var, perde yok");
                continue;
            }

            float x = room.MaxX - 2f;

            // ---- KILITLENME DENETIMI ----
            // Perdenin onunde, AYNI segment icinde sonsuz su var mi?
            float segmentStart = LastBarrierBefore(barriers, x);
            bool hasWater = false;
            foreach (float wx in waters)
            {
                if (wx < x && wx >= segmentStart) { hasWater = true; break; }
            }

            if (!hasWater)
            {
                skipped++;
                report.Append("\n  ✖ ").Append(room.roomName)
                      .Append(" — PERDE KONMADI: x=").Append(x.ToString("0"))
                      .Append(" oncesinde (x≥").Append(segmentStart.ToString("0"))
                      .Append(" segmentinde) sonsuz su kaynagi yok. ")
                      .Append("Susuz gelen oyuncu kilitlenirdi.");
                continue;
            }

            float groundY = FindFloor(x, room, groundMask);

            // ⚠️ ZEMINDEN TAVANA. Ilk surumde 5 birimdi ve oyuncu ustunden
            // atliyordu: odalarda 2.5 birim yuksekliginde mobilyalar var,
            // oyuncu ~3 birim ziplyor, yani 5.5 birime ulasiyor.
            // Perde "asilamaz" olacaksa gercekten asilamaz olmali.
            float height = Mathf.Max(6f, room.MaxY - groundY);

            GameObject go = new GameObject("AlevPerdesi_" + room.roomName);
            go.transform.SetParent(holder, false);
            go.transform.position = new Vector3(x, groundY + height * 0.5f, 0f);

            // Dekoratif aleslerden KALIN olsun — oyuncu bakinca "bu gecilmez"
            // demeli. Seviyede 23 dekoratif alev var ve hepsi gecilebilir;
            // perde onlardan ayirt edilemezse oyuncu hangisinin duvar
            // oldugunu bilemez.
            go.transform.localScale = new Vector3(1.6f, height, 1f);

            go.layer = LayerMask.NameToLayer("Enemy");   // WaterGun sadece bu layer'i tariyor
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            // Dekoratif alevler turuncu; perde neredeyse BEYAZ-SARI ve tam
            // opak. "Bu daha sicak, buna dokunulmaz" okunmali.
            sr.color = new Color(1f, 0.82f, 0.35f, 1f);
            sr.sortingOrder = 6;                       // her seyin onunde

            // Icine daha koyu bir cekirdek: duz tek renk dikdortgen
            // "alev" degil "sari duvar" gibi duruyor.
            GameObject core = new GameObject("Cekirdek");
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = new Vector3(0.55f, 0.97f, 1f);

            SpriteRenderer coreSr = core.AddComponent<SpriteRenderer>();
            coreSr.sprite = square;
            coreSr.color = new Color(1f, 0.45f, 0.05f, 1f);
            coreSr.sortingOrder = 7;

            // ⚠️ TRIGGER DEGIL: gecidi gercekten kapatmasi gerekiyor.
            go.AddComponent<BoxCollider2D>();

            FlameCurtain fc = go.AddComponent<FlameCurtain>();
            fc.maxHealth = 80f;
            fc.extinguishClip = hiss;

            FireFlicker flicker = go.AddComponent<FireFlicker>();
            flicker.verticalBias = 0.6f;   // dikey perde, yatayda oynasin

            placed++;
            report.Append("\n  ✔ ").Append(room.roomName)
                  .Append(" — x=").Append(x.ToString("0"));
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<b>[ALEV]</b> Alev perdesi: {placed} kuruldu, {skipped} atlandi." +
                  report +
                  (skipped > 0
                      ? "\n\nAtlanan odaya perde istiyorsaniz o segmente SONSUZ bir su " +
                        "kaynagi (musluk/kuvet) ekleyip komutu tekrar calistirin."
                      : ""));
    }

    /// <summary>
    /// Odanin GERCEK zeminini bulur.
    ///
    /// ⚠️ Tek bir Raycast YETMIYOR: odalarin tavani da Ground layer'inda
    /// (Tavan_Antre, Koridor_AlcakTavan, Tavan_CocukOdasi...). Yukaridan
    /// asagi atilan isin once TAVANA carpiyor ve perde tavanin ustune
    /// kuruluyordu — ekranda havada duran alevler bu yuzden cikti.
    ///
    /// Cozum: butun temaslari topla, EN ALTTAKINI zemin say.
    /// </summary>
    static float FindFloor(float x, Room room, int groundMask)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            new Vector2(x, room.MaxY + 1f), Vector2.down, room.size.y + 8f, groundMask);

        float lowest = float.PositiveInfinity;
        foreach (RaycastHit2D h in hits)
            if (h.point.y < lowest) lowest = h.point.y;

        return float.IsPositiveInfinity(lowest) ? room.MinY : lowest;
    }

    /// <summary>Verilen x'ten onceki son geri-donulmez nokta.</summary>
    static float LastBarrierBefore(List<float> barriers, float x)
    {
        float result = float.NegativeInfinity;
        foreach (float b in barriers)
            if (b < x && b > result) result = b;

        return result;
    }

    /// <summary>
    /// Geri donusu kapatan objeleri bulur. Isimle ariyoruz cunku bunlar
    /// Kisi A'nin seviye objeleri, ortak bir bilesenleri yok.
    /// </summary>
    static void CollectBarriers(Transform t, List<float> into)
    {
        if (t.name.Contains("AlevDuvari") || t.name.Contains("FlameWall"))
            into.Add(t.position.x);

        for (int i = 0; i < t.childCount; i++)
            CollectBarriers(t.GetChild(i), into);
    }
}
