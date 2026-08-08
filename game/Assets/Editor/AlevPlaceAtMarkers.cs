using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// KISI B'NIN PREFABLARINI SPAWNMARKER'LARIN UZERINE YERLESTIRIR.
///
/// Menu: ALEV → Kisi B — Prefablari Isaretlere Yerlestir
///
/// NEDEN VAR: AlevLevelBuilder 25+ SpawnMarker koyuyor. Bunlari elle
/// doldurmak 25+ surukle-birak demek ve her biri yanlis konum / eksik
/// ayar riski tasiyor. Tek tikta, dogru prefabi, dogru konuma koyar.
///
/// KIM CALISTIRIR: MainScene Kisi A'nin sahnesi, sahne sahipligi kurali
/// geregi Kisi B onu acmaz. Bu komutu MainScene acikken KISI A calistirir.
/// Kisi B kendi TestScene_B'sinde de calistirabilir.
///
/// Tekrar calistirilabilir: yerlestirilen isaret siliniyor, dolayisiyla
/// ikinci calistirmada ayni sey iki kez konmaz.
/// </summary>
public static class AlevPlaceAtMarkers
{
    const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";
    const string DogPrefabFolder = "Assets/Prefabs/Dog";

    [MenuItem("ALEV/Kisi B — Prefablari Isaretlere Yerlestir", true)]
    static bool PlaceAllValidate() => !EditorApplication.isPlaying;

    [MenuItem("ALEV/Kisi B — Prefablari Isaretlere Yerlestir", false, 22)]
    public static void PlaceAll()
    {
        GameObject wallMonster = Load(EnemyPrefabFolder + "/WallMonster.prefab");
        GameObject floorFire = Load(EnemyPrefabFolder + "/FloorFire.prefab");
        GameObject karabas = Load(DogPrefabFolder + "/Karabas.prefab");

        if (wallMonster == null || floorFire == null || karabas == null)
        {
            EditorUtility.DisplayDialog(
                "Once prefablari kur",
                "Prefablar bulunamadi.\n\n" +
                "Once: ALEV → Kisi B — Prefablari Kur",
                "Tamam");
            return;
        }

        SpawnMarker[] all = Object.FindObjectsByType<SpawnMarker>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        // ⚠️ ONCE YERLESTIRILECEK BIR SEY VAR MI DIYE BAK — SONRA SIL.
        // Bu sira hayati: isaretler onceki calistirmada tuketilmis olabilir.
        // Once silip sonra "koyacak sey yok" dersek sahnedeki her seyi
        // silip yerine hicbir sey koymamis oluruz. (Bir kez basimiza geldi.)
        int placeable = 0;
        foreach (SpawnMarker m in all)
        {
            if (m.kind == SpawnMarker.Kind.WallMonster ||
                m.kind == SpawnMarker.Kind.FloorFire ||
                m.kind == SpawnMarker.Kind.Dog)
            {
                placeable++;
            }
        }

        if (placeable == 0)
        {
            EditorUtility.DisplayDialog(
                "Yerlestirilecek isaret yok",
                "Sahnede duvar canavari / yer atesi / kopek isareti bulunamadi.\n\n" +
                "Muhtemelen onceki calistirmada tuketildiler.\n" +
                "Isaretleri geri getirmek icin:\n\n" +
                "    ALEV → Evi Kur\n\n" +
                "Sahnedeki mevcut objelere DOKUNULMADI.",
                "Tamam");
            return;
        }

        // Artik silmek guvenli: yerine koyacagimiz sey oldugunu biliyoruz.
        // Temizlik sart, yoksa Evi Kur isaretleri geri getirdiginde ayni
        // noktalara ikinci bir set biniyor ve sahne kopyalarla doluyor.
        int cleared = ClearPreviouslyPlaced();

        int wm = 0, ff = 0, dog = 0;
        var skipped = new List<string>();
        Transform holder = GetOrCreateHolder();

        foreach (SpawnMarker marker in all)
        {
            Vector3 pos = marker.transform.position;

            switch (marker.kind)
            {
                case SpawnMarker.Kind.WallMonster:
                {
                    GameObject go = Place(wallMonster, pos, holder);
                    // Duvardan cikis yonunu otomatik bul: hangi tarafta duvar
                    // varsa ters tarafa cikar. Elle 10 tane ayarlamaktan iyi.
                    var monster = go.GetComponent<WallMonster>();
                    if (monster != null) monster.facing = InferFacing(pos);
                    wm++;
                    break;
                }

                case SpawnMarker.Kind.FloorFire:
                    Place(floorFire, pos, holder);
                    ff++;
                    break;

                case SpawnMarker.Kind.Dog:
                    Place(karabas, pos, holder);
                    dog++;
                    break;

                case SpawnMarker.Kind.CoveringFire:
                    // Karabas prefabinin ICINDE zaten var (TrappedFire cocugu).
                    // Ayri obje koymak iki ates ustuste demek olurdu.
                    skipped.Add("CoveringFire — Karabas prefabinin icinde zaten var, atlandi");
                    continue;   // isareti SILME, bilgi olarak kalsin

                case SpawnMarker.Kind.Boss:
                    skipped.Add("Boss — ayri komutla: ALEV → Kisi B — Finali Sahneye Kur");
                    continue;

                default:
                    // PlayerStart / FlameWall / DogSilhouette Kisi A'nin isi.
                    skipped.Add(marker.kind + " — Kisi A'nin isareti, dokunulmadi");
                    continue;
            }

            Object.DestroyImmediate(marker.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        var sb = new StringBuilder();

        if (cleared > 0)
            sb.Append("<b>[ALEV-B]</b> ").Append(cleared)
              .Append(" eski obje temizlendi.\n");

        sb.Append("<b>[ALEV-B]</b> Yerlestirildi — ")
          .Append(wm).Append(" duvar canavari, ")
          .Append(ff).Append(" yer atesi, ")
          .Append(dog).Append(" Karabas.");

        if (skipped.Count > 0)
        {
            sb.Append("\n\nDokunulmayanlar:");
            foreach (string s in Dedupe(skipped)) sb.Append("\n  • ").Append(s);
        }

        Debug.Log(sb.ToString());
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Daha once yerlestirilmis (ya da elle surukLENMIS) tum Kisi B
    /// objelerini siler. Komutun tekrar tekrar calistirilabilmesini saglar.
    ///
    /// Bilesen tipine gore ariyoruz, isme gore degil: elle surukLENEN
    /// kopyalar "Karabas (1)" gibi adlar aliyor ve isim eslesmesi kaciriyordu.
    /// </summary>
    static int ClearPreviouslyPlaced()
    {
        var doomed = new List<GameObject>();

        Collect<WallMonster>(doomed);
        Collect<FloorFire>(doomed);
        Collect<DogCompanion>(doomed);

        foreach (GameObject go in doomed)
            if (go != null) Object.DestroyImmediate(go);

        return doomed.Count;
    }

    static void Collect<T>(List<GameObject> into) where T : Component
    {
        foreach (T c in Object.FindObjectsByType<T>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null) continue;

            // Karabas'in icindeki ates ebeveyniyle birlikte gidecek,
            // ayrica silmeye calisirsak null referans olur.
            if (c.transform.parent != null &&
                c.transform.parent.GetComponentInParent<DogCompanion>() != null)
            {
                continue;
            }

            if (!into.Contains(c.gameObject)) into.Add(c.gameObject);
        }
    }

    /// <summary>
    /// Canavarin duvardan hangi yone cikacagini konumdan cikarir.
    /// Solunda duvar varsa saga (+1), saginda duvar varsa sola (-1) cikar.
    /// Ikisi de varsa/yoksa varsayilan -1.
    /// </summary>
    static int InferFacing(Vector3 pos)
    {
        int ground = LayerMask.GetMask("Ground");
        const float probe = 1.5f;

        bool wallLeft = Physics2D.Raycast(pos, Vector2.left, probe, ground);
        bool wallRight = Physics2D.Raycast(pos, Vector2.right, probe, ground);

        if (wallLeft && !wallRight) return 1;
        if (wallRight && !wallLeft) return -1;
        return -1;
    }

    static GameObject Place(GameObject prefab, Vector3 pos, Transform parent)
    {
        // InstantiatePrefab (Instantiate degil): prefab baglantisi korunur,
        // sonra prefabta yapilan degisiklik sahnedeki hepsine yansir.
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        return go;
    }

    static Transform GetOrCreateHolder()
    {
        GameObject existing = GameObject.Find("KisiB_Objeleri");
        if (existing != null) return existing.transform;

        return new GameObject("KisiB_Objeleri").transform;
    }

    static GameObject Load(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static IEnumerable<string> Dedupe(List<string> items)
    {
        var seen = new HashSet<string>();
        foreach (string i in items)
            if (seen.Add(i)) yield return i;
    }
}
