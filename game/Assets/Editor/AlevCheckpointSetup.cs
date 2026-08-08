using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Her odanin girisine bir Checkpoint koyar.
///
/// Menu: ALEV → Checkpoint'leri Odalara Kur
///
/// Elle kurmak 7 oda x (obje olustur + collider + konum + isim) demek;
/// biri unutulursa oyuncu o odada olunce cok geriye atiliyor ve bunu
/// fark etmek zor. Kod hepsini ayni sekilde kuruyor.
///
/// Dogma noktasi odanin sol kenarindan biraz iceride ve ZEMININ USTUNDE:
/// asagi isin atip gercek zemini buluyoruz, yoksa oyuncu havada dogup
/// dusuyor ya da zemine gomuluyor.
/// </summary>
public static class AlevCheckpointSetup
{
    const string HolderName = "Checkpointler";

    [MenuItem("ALEV/Checkpoint'leri Odalara Kur", true)]
    static bool Validate() => !EditorApplication.isPlaying;

    [MenuItem("ALEV/Checkpoint'leri Odalara Kur", false, 5)]
    public static void Build()
    {
        Room[] rooms = Object.FindObjectsByType<Room>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (rooms.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Oda bulunamadi",
                "Sahnede Room bileseni tasiyan obje yok.\n\n" +
                "Once evi kurun: ALEV → Evi Kur",
                "Tamam");
            return;
        }

        int groundMask = LayerMask.GetMask("Ground");
        if (groundMask == 0)
        {
            EditorUtility.DisplayDialog(
                "Ground layer yok",
                "'Ground' layer'i tanimli degil; dogma noktasi zemine oturtulamaz.",
                "Tamam");
            return;
        }

        // Eskileri temizle — komut tekrar calistirilabilir olsun.
        GameObject old = GameObject.Find(HolderName);
        if (old != null) Object.DestroyImmediate(old);

        Transform holder = new GameObject(HolderName).transform;

        // Soldan saga sirala: isimlendirme ve okuma kolay olsun.
        var sorted = new List<Room>(rooms);
        sorted.Sort((a, b) => a.MinX.CompareTo(b.MinX));

        var log = new StringBuilder();
        int placed = 0;
        int floating = 0;

        foreach (Room room in sorted)
        {
            // Odanin sol kenarindan 3 birim iceride — tam kenarda olursa
            // oyuncu bir onceki odadayken tetikleyebiliyor.
            float x = room.MinX + 3f;

            // ⚠️ Tek Raycast YETMIYOR: odalarin TAVANI da Ground layer'inda
            // (Tavan_Antre, Koridor_AlcakTavan...). Yukaridan atilan isin
            // once tavana carpiyor ve oyuncu catida doguyordu.
            // Butun temaslari topla, EN ALTTAKINI zemin say.
            RaycastHit2D[] hits = Physics2D.RaycastAll(
                new Vector2(x, room.MaxY + 1f), Vector2.down, room.size.y + 8f, groundMask);

            float lowest = float.PositiveInfinity;
            foreach (RaycastHit2D h in hits)
                if (h.point.y < lowest) lowest = h.point.y;

            float y;
            if (!float.IsPositiveInfinity(lowest))
            {
                y = lowest + 1f;      // oyuncu ~1.6 boyunda, merkezi bu kadar yukarida
            }
            else
            {
                y = room.MinY + 1.5f;
                floating++;
                log.Append("\n  ⚠ ").Append(room.roomName)
                   .Append(" — zemin bulunamadi, tahmini konum kullanildi");
            }

            GameObject go = new GameObject("CP_" + room.roomName);
            go.transform.SetParent(holder, false);
            go.transform.position = new Vector3(x, y, 0f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            // Yuksek kutu: oyuncu ust platformdan gecse de yakalansin.
            col.size = new Vector2(1.5f, room.size.y);
            col.offset = new Vector2(0f, room.size.y * 0.4f);

            Checkpoint cp = go.AddComponent<Checkpoint>();
            cp.checkpointName = room.roomName;

            placed++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<b>[ALEV]</b> {placed} checkpoint kuruldu (odalarin girisine)." +
                  (floating > 0 ? "\nUYARI — " + floating + " tanesinde zemin bulunamadi:" + log : "") +
                  "\nArtik olunce bulundugun odanin basinda devam ediyorsun.");
    }
}
