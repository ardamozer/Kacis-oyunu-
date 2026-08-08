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

            // ---- Zemini bul ----
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(x, room.MaxY), Vector2.down, room.size.y + 4f, groundMask);

            float groundY = hit.collider != null ? hit.point.y : room.MinY;

            const float height = 5f;

            GameObject go = new GameObject("AlevPerdesi_" + room.roomName);
            go.transform.SetParent(holder, false);
            go.transform.position = new Vector3(x, groundY + height * 0.5f, 0f);
            go.transform.localScale = new Vector3(0.9f, height, 1f);

            go.layer = LayerMask.NameToLayer("Enemy");   // WaterGun sadece bu layer'i tariyor
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(1f, 0.4f, 0.08f, 0.92f);
            sr.sortingOrder = 3;

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
