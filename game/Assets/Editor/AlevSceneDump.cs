using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SAHNE RAPORU — menuden: ALEV → Sahne Raporu Cikar
///
/// Sahnedeki her seyi odalara gore gruplayip metin dosyasina yazar:
///   D:\Kacis oyunu\SahneRaporu.txt
///
/// Neden: ekran goruntusu "cirkin duruyor" sorularini cozer ama
/// "buraya ziplanmiyor", "burada bosluk var", "bu obje duvarin icinde"
/// gibi yapisal sorunlar icin KOORDINAT lazim. Bu rapor onu veriyor.
/// </summary>
public static class AlevSceneDump
{
    const float SafeRise = 2.1f;   // guvenli ziplama yuksekligi

    [MenuItem("ALEV/Sahne Raporu Cikar", false, 4)]
    public static void Dump()
    {
        var sb = new StringBuilder();
        sb.AppendLine("ALEV — SAHNE RAPORU");
        sb.AppendLine($"Sahne: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine(new string('=', 70));

        var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None)
                          .OrderBy(r => r.MinX).ToList();

        var solids = new List<(string name, Bounds b)>();
        var others = new List<(string name, string kind, Vector2 pos)>();

        foreach (var col in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (col.isTrigger) continue;
            if (col.gameObject.layer != LayerMask.NameToLayer("Ground")) continue;
            solids.Add((col.name, col.bounds));
        }

        Add<WaterSource>(others, "SU");
        Add<KeyPickup>(others, "ANAHTAR");
        Add<LockedDoor>(others, "KILIT");
        Add<FallingDebris>(others, "TAHTA");
        Add<AmbientFire>(others, "dekor-ates");
        Add<EndingTriggerZone>(others, "TETIK");
        Add<EscapeTrigger>(others, "CIKIS");
        Add<SpawnMarker>(others, "isaret");

        // Kisi B'nin siniflari — isimle yakala, derleme bagimliligi kurma
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,
                                                                   FindObjectsSortMode.None))
        {
            string t = mb.GetType().Name;
            if (t == "WallMonster" || t == "FloorFire" || t == "DogCompanion" ||
                t == "BossCore" || t == "TrappedFireCover")
            {
                string aktif = mb.gameObject.activeInHierarchy ? "" : " [PASIF]";
                others.Add((mb.name + aktif, t.ToUpper(), mb.transform.position));
            }
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            others.Add((player.name, "OYUNCU", player.transform.position));

        foreach (var room in rooms)
        {
            sb.AppendLine();
            sb.AppendLine($"### {room.roomName}   x {room.MinX:0.#}..{room.MaxX:0.#}   " +
                          $"yukseklik {room.size.y:0.#}");
            sb.AppendLine(new string('-', 70));

            var inRoom = solids.Where(s => s.b.center.x >= room.MinX && s.b.center.x <= room.MaxX)
                               .OrderBy(s => s.b.center.y).ThenBy(s => s.b.min.x).ToList();

            sb.AppendLine("  BASILABILIR YUZEYLER (ust yuzey yuksekligine gore):");
            foreach (var s in inRoom)
            {
                if (s.b.size.x > 40f) continue;              // zemin/tavan gibi devasa parcalar
                sb.AppendLine($"    ust={s.b.max.y,6:0.##}  x {s.b.min.x,7:0.#}..{s.b.max.x,-7:0.#} " +
                              $"gen={s.b.size.x,5:0.#}  {s.name}");
            }

            // ---- Ziplama analizi ----
            sb.AppendLine("  ZIPLAMA KONTROLU:");
            bool anyProblem = false;
            foreach (var p in inRoom)
            {
                if (p.b.size.x > 40f || p.b.max.y < 1.5f) continue;

                bool reachable = inRoom.Concat(solids).Any(q =>
                {
                    if (q.name == p.name && q.b.center == p.b.center) return false;
                    float rise = p.b.max.y - q.b.max.y;
                    if (rise <= 0f) return true;
                    if (rise > SafeRise) return false;
                    float gap = Mathf.Max(0f, Mathf.Max(q.b.min.x - p.b.max.x, p.b.min.x - q.b.max.x));
                    float allowed = rise > 1.5f ? 3.5f : (rise > 0.6f ? 4.5f : 5f);
                    return gap <= allowed;
                });

                if (!reachable)
                {
                    sb.AppendLine($"    ❌ ULASILAMAZ  {p.name}  (ust={p.b.max.y:0.##})");
                    anyProblem = true;
                }
            }
            if (!anyProblem) sb.AppendLine("    ✔ hepsi ulasilabilir");

            // ---- Icerik ----
            var stuff = others.Where(o => o.pos.x >= room.MinX && o.pos.x <= room.MaxX)
                              .OrderBy(o => o.pos.x).ToList();

            sb.AppendLine("  ICERIK:");
            foreach (var o in stuff)
            {
                if (o.kind == "dekor-ates") continue;         // cok fazla, ayri sayiliyor
                sb.AppendLine($"    ({o.pos.x,6:0.#},{o.pos.y,5:0.#})  {o.kind,-10} {o.name}");
            }
            int fireCount = stuff.Count(o => o.kind == "dekor-ates");
            sb.AppendLine($"    dekoratif ates sayisi: {fireCount}");

            // ---- Tavan altinda kalan var mi ----
            foreach (var o in stuff)
                if (o.pos.y > room.MaxY - 0.5f)
                    sb.AppendLine($"    ⚠️ TAVANIN USTUNDE: {o.name} (y={o.pos.y:0.#}, tavan={room.MaxY:0.#})");
        }

        // ---- Oda disinda kalan var mi ----
        sb.AppendLine();
        sb.AppendLine("### HICBIR ODAYA AIT OLMAYANLAR");
        sb.AppendLine(new string('-', 70));
        foreach (var o in others)
        {
            bool inAny = rooms.Any(r => o.pos.x >= r.MinX && o.pos.x <= r.MaxX &&
                                        o.pos.y >= r.MinY && o.pos.y <= r.MaxY);
            if (!inAny && o.kind != "dekor-ates" && o.kind != "isaret")
                sb.AppendLine($"    ⚠️ ({o.pos.x:0.#},{o.pos.y:0.#})  {o.kind} {o.name}");
        }

        string path = Path.Combine(
            Directory.GetParent(Application.dataPath).Parent.FullName, "SahneRaporu.txt");
        File.WriteAllText(path, sb.ToString());

        Debug.Log($"<b>[ALEV]</b> Sahne raporu yazildi:\n{path}\n" +
                  "Bu dosyayi Claude'a okutabilirsiniz.");
        EditorUtility.RevealInFinder(path);
    }

    static void Add<T>(List<(string, string, Vector2)> list, string kind) where T : Component
    {
        // Pasif objeler DAHIL — boss, hayalet ve tasma sahnede devre disi
        // basliyor, ilk surumde raporda hic gorunmuyorlardi.
        foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include,
                                                      FindObjectsSortMode.None))
            list.Add((c.name, kind, c.transform.position));
    }
}
