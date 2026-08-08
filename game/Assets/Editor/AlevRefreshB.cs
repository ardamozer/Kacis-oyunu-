using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// TEK TUSLA TAZELEME — F5
///
/// Menu: ALEV → Kisi B — Sahneyi Tazele (F5)
///
/// NEDEN VAR: kodda her degisiklikten sonra ayni dort adim tekrarlaniyordu:
///   prefablari yeniden kur → sahnedeki eskileri sil → yenilerini surukle
///   → finali yeniden kur
/// Bu, dakikada bir yapilan bir is ve her seferinde bir adim unutuluyor.
///
/// ⚠️ EN ONEMLI OZELLIK: DUZENI KORUR.
/// Silmeden once sahnedeki her dusmanin/kopegin konumunu not eder, yeni
/// ornekleri AYNI konumlara koyar. Yani saatlerce ugrasip yerlestirdiginiz
/// seviye kaybolmaz — sadece prefablar tazelenir.
///
/// Prefab varligini ustune yazmak sahnedeki orneklerin baglantisini bazen
/// bozuyor (fileID'ler degisiyor). Sil-yeniden koy bu belirsizligi tamamen
/// ortadan kaldiriyor.
/// </summary>
public static class AlevRefreshB
{
    /// <summary>
    /// _F5 = ciplak F5 tusu. (% Ctrl, # Shift, & Alt)
    /// Unity fare imleci Scene/Game/Hierarchy uzerindeyken yakalar.
    /// </summary>
    [MenuItem("ALEV/Kisi B — Sahneyi Tazele _F5", false, 24)]
    public static void Refresh()
    {
        // 1) Mevcut duzeni not al.
        var wallMonsters = CapturePositions<WallMonster>();
        var floorFires = CapturePositions<FloorFire>();
        var dogs = CapturePositions<DogCompanion>();

        bool hadEnding = Object.FindFirstObjectByType<EndingSequence>(FindObjectsInactive.Include) != null;

        // 2) Eskileri sil. Kopegin cocugu olan TrappedFire de gider,
        //    ayrica silmeye gerek yok.
        DestroyAll<WallMonster>();
        DestroyAll<FloorFire>();
        DestroyAll<DogCompanion>();

        // 3) Prefablari yeniden kur (sesler ve bilesenler dahil).
        AlevPrefabSetupB.BuildPrefabs();

        // 4) Yenilerini ESKI KONUMLARA koy.
        Transform holder = GetHolder();

        int wm = Restore("Assets/Prefabs/Enemies/WallMonster.prefab", wallMonsters, holder);
        int ff = Restore("Assets/Prefabs/Enemies/FloorFire.prefab", floorFires, holder);
        int dg = Restore("Assets/Prefabs/Dog/Karabas.prefab", dogs, holder);

        // 5) Final sahnesi zaten kuruluysa onu da tazele. Kurulu degilse
        //    dokunmuyoruz — kullanici henuz oraya gelmemis olabilir.
        if (hadEnding) AlevPrefabSetupB.BuildEnding();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<b>[ALEV-B]</b> Tazelendi — {wm} duvar canavari, {ff} yer atesi, " +
                  $"{dg} Karabas eski konumlarinda yeniden kuruldu." +
                  (hadEnding ? " Final sahnesi de yenilendi." : "") +
                  "\nPlay'e basabilirsin.");
    }

    /// <summary>
    /// Sahnede hic B objesi yoksa tazelemenin koyacagi bir sey olmaz.
    /// O durumda ilk kurulumu yapan komutlari hatirlatalim.
    /// </summary>
    [MenuItem("ALEV/Kisi B — Sahneyi Tazele _F5", true)]
    static bool RefreshValidate()
    {
        return !EditorApplication.isPlaying;
    }

    // ------------------------------------------------------------------

    struct Placed
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    static List<Placed> CapturePositions<T>() where T : Component
    {
        var list = new List<Placed>();

        foreach (T c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Sadece KOK objeleri say. TrappedFire gibi cocuklar ebeveyniyle
            // birlikte gelir; ayrica kaydedersek ikinci kez kurulur.
            if (c.transform.parent != null &&
                c.transform.parent.GetComponentInParent<DogCompanion>() != null)
            {
                continue;
            }

            list.Add(new Placed
            {
                position = c.transform.position,
                rotation = c.transform.rotation,
                scale = c.transform.localScale
            });
        }

        return list;
    }

    static void DestroyAll<T>() where T : Component
    {
        foreach (T c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null) continue;

            // Kopegin icindeki atesi ayrica silmeye calisma — kopekle gider.
            if (c.transform.parent != null &&
                c.transform.parent.GetComponentInParent<DogCompanion>() != null)
            {
                continue;
            }

            Object.DestroyImmediate(c.gameObject);
        }
    }

    static int Restore(string prefabPath, List<Placed> spots, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return 0;

        foreach (Placed p in spots)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = p.position;
            go.transform.rotation = p.rotation;

            // Olcegi geri koymuyoruz: FireFlicker ve DogCompanion olcegi
            // her karede kendileri oynatiyor, kaydedilen deger o anki
            // titreme degeriydi — prefab varsayilani dogru olan.
        }

        return spots.Count;
    }

    static Transform GetHolder()
    {
        GameObject existing = GameObject.Find("KisiB_Objeleri");
        if (existing != null) return existing.transform;

        return new GameObject("KisiB_Objeleri").transform;
    }
}
