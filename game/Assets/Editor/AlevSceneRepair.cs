using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// SAHNE ONARIMI — menuden: ALEV → Sahneyi Onar
///
/// Sahne raporunda bulunan uc sorunu duzeltir. Evi yeniden kurmaya
/// GEREK YOK — elle yaptiginiz hicbir sey silinmez.
///
///   1. Olum tahtasini olum tetikleyicisinin ustune tasir
///      (Kisi B'nin kurulumu onu Cocuk odasina, Karabas'in ustune
///       koymustu; tetikleyici ise arka koridordaydi — tahta ekran
///       disinda dusuyor, oyuncu hicbir sey gormuyordu)
///
///   2. [FlameWall] isaretine gercek alev duvari kurar
///      (geri donusun kapandigi an hic gerceklesmiyordu)
///
///   3. [DogSilhouette] isaretine kopek mamasi kurar
///
/// Tekrar calistirilabilir — zaten duzgun olani atlar.
/// </summary>
public static class AlevSceneRepair
{
    [MenuItem("ALEV/Sahneyi Onar", false, 5)]
    public static void Repair()
    {
        int fixes = 0;

        fixes += FixDeathDebris();
        fixes += BuildFlameWall();
        fixes += BuildDogBowl();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (fixes == 0)
            Debug.Log("<b>[ALEV Onarim]</b> Duzeltilecek bir sey bulunamadi.");
        else
            Debug.Log($"<b>[ALEV Onarim]</b> {fixes} duzeltme yapildi. <b>CTRL+S yapin.</b>");
    }

    // ------------------------------------------------------------------
    //  1. Olum tahtasini dogru yere tasi
    // ------------------------------------------------------------------

    static int FixDeathDebris()
    {
        var trigger = Object.FindObjectsByType<EndingTriggerZone>(FindObjectsSortMode.None)
                            .FirstOrDefault(z => z.trigger == EndingTriggerZone.What.DogDeath);
        if (trigger == null)
        {
            Debug.LogWarning("[ALEV Onarim] Kopegin olumu tetikleyicisi bulunamadi, atlandi.");
            return 0;
        }

        // Sahnedeki TUM FallingDebris'ler (pasif olanlar dahil)
        var all = Object.FindObjectsByType<FallingDebris>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);

        // EndingSequence hangisine bagli?
        var ending = Object.FindFirstObjectByType<EndingSequence>();
        FallingDebris wired = ending != null ? ending.deathDebris : null;

        FallingDebris target = wired
            ?? all.FirstOrDefault(d => d.name.Contains("OLUM") || d.name.Contains("Death"));

        if (target == null)
        {
            Debug.LogWarning("[ALEV Onarim] Olum tahtasi bulunamadi, atlandi.");
            return 0;
        }

        float wantX = trigger.transform.position.x;
        float wantY = trigger.transform.position.y + 7.4f;   // tavana yakin

        if (Mathf.Abs(target.transform.position.x - wantX) < 2f)
        {
            Debug.Log("[ALEV Onarim] Olum tahtasi zaten dogru yerde.");
        }
        else
        {
            Undo.RecordObject(target.transform, "Olum tahtasini tasi");
            Vector3 before = target.transform.position;
            target.transform.position = new Vector3(wantX, wantY, 0f);
            Debug.Log($"[ALEV Onarim] Olum tahtasi tasindi: " +
                      $"({before.x:0.#},{before.y:0.#}) → ({wantX:0.#},{wantY:0.#})", target);
        }

        // Erken dusmesin
        if (target.autoTrigger)
        {
            Undo.RecordObject(target, "autoTrigger kapat");
            target.autoTrigger = false;
            Debug.Log("[ALEV Onarim] Olum tahtasinin autoTrigger'i kapatildi " +
                      "(yoksa oyuncu yaklasinca kendi kendine duserdi).");
        }

        // EndingSequence'e bagli degilse bagla
        if (ending != null && ending.deathDebris != target)
        {
            Undo.RecordObject(ending, "deathDebris bagla");
            ending.deathDebris = target;
            Debug.Log("[ALEV Onarim] EndingSequence → Death Debris baglandi.");
        }

        // Ayni isi yapan fazladan olum tahtalarini SIL.
        // Ikisi ayni noktada dusunce goruntu bozuluyordu. EndingSequence
        // hangisine bagliysa o kalir, digerleri gider.
        foreach (var d in all.ToList())
        {
            if (d == target) continue;
            if (!d.name.Contains("OLUM") && !d.name.Contains("Death")) continue;

            Debug.Log($"[ALEV Onarim] Fazladan olum tahtasi silindi: '{d.name}'");
            Undo.DestroyObjectImmediate(d.gameObject);
        }

        return 1;
    }

    // ------------------------------------------------------------------
    //  2. Alev duvari
    // ------------------------------------------------------------------

    static int BuildFlameWall()
    {
        if (Object.FindFirstObjectByType<FlameWallGate>() != null)
        {
            Debug.Log("[ALEV Onarim] Alev duvari zaten var.");
            return 0;
        }

        var marker = Object.FindObjectsByType<SpawnMarker>(FindObjectsSortMode.None)
                           .FirstOrDefault(m => m.kind == SpawnMarker.Kind.FlameWall);
        if (marker == null)
        {
            Debug.LogWarning("[ALEV Onarim] [FlameWall] isareti yok, atlandi.");
            return 0;
        }

        Sprite flame = AlevLevelBuilder.GetOrCreateFlameSprite();
        Vector3 p = marker.transform.position;

        var root = new GameObject("AlevDuvari");
        root.transform.position = new Vector3(p.x, p.y + 1.6f, 0f);
        Undo.RegisterCreatedObjectUndo(root, "Alev duvari");

        // Gecisi kapatan kati engel
        var blocker = root.AddComponent<BoxCollider2D>();
        blocker.size = new Vector2(2.6f, 5f);

        // Uc alev yan yana — tek buyuk sprite yerine, daha ikna edici
        for (int i = 0; i < 3; i++)
        {
            var f = new GameObject($"Alev_{i + 1}");
            f.transform.SetParent(root.transform, false);
            f.transform.localPosition = new Vector3((i - 1) * 0.85f, i == 1 ? 0.4f : 0f, 0f);
            f.transform.localScale = new Vector3(i == 1 ? 2.2f : 1.6f, i == 1 ? 5.4f : 4.2f, 1f);

            var sr = f.AddComponent<SpriteRenderer>();
            sr.sprite = flame;
            sr.sortingOrder = 3;          // bu ates ONDE olmali, engel oldugunu gostersin
            f.AddComponent<AmbientFire>();
        }

        root.AddComponent<FlameWallGate>();

        Debug.Log($"[ALEV Onarim] Alev duvari kuruldu: x={p.x:0.#}. " +
                  "Karabas kurtulunca yukselecek.", root);
        return 1;
    }

    // ------------------------------------------------------------------
    //  3. Kopek mamasi
    // ------------------------------------------------------------------

    static int BuildDogBowl()
    {
        if (GameObject.Find("KopekMamasi") != null) return 0;

        var marker = Object.FindObjectsByType<SpawnMarker>(FindObjectsSortMode.None)
                           .FirstOrDefault(m => m.kind == SpawnMarker.Kind.DogSilhouette);
        if (marker == null) return 0;

        Sprite square = AlevSceneSetup.GetOrCreateSquareSprite();
        Vector3 p = marker.transform.position;

        var bowl = new GameObject("KopekMamasi");
        bowl.transform.position = new Vector3(p.x, 0.35f, 0f);
        bowl.transform.localScale = new Vector3(1.4f, 0.5f, 1f);
        var sr = bowl.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.55f, 0.42f, 0.22f);
        sr.sortingOrder = 1;
        Undo.RegisterCreatedObjectUndo(bowl, "Kopek mamasi");

        var tag = new GameObject("Tasma");
        tag.transform.SetParent(bowl.transform, false);
        tag.transform.localPosition = new Vector3(1.4f, 0f, 0f);
        tag.transform.localScale = new Vector3(0.5f, 1.2f, 1f);
        var tsr = tag.AddComponent<SpriteRenderer>();
        tsr.sprite = square;
        tsr.color = new Color(0.65f, 0.25f, 0.20f);
        tsr.sortingOrder = 1;

        Debug.Log($"[ALEV Onarim] Kopek mamasi kuruldu: x={p.x:0.#}", bowl);
        return 1;
    }
}
