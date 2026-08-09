using UnityEditor;
using UnityEngine;

public static class AlevSceneFixer
{
    /// <summary>
    /// Boss'u KAPI TETIKLEMESINE gecirir.
    ///
    /// Boss artik x cizgisiyle degil, oyuncu cikis kapisindan gecmeye CALISINCA
    /// doguyor (EscapeTrigger). Bu komut sahnedeki eski x-tetigini kapatiyor,
    /// yoksa boss kapiya varilmadan erken cikardi.
    ///
    /// Ayri bir komut, cunku asagidaki Hizli Duzeltme requiredKeyCount'u da
    /// sifirliyor — onu calistirmak calisan anahtar korumasini sessizce kaldirir.
    /// </summary>
    [MenuItem("ALEV/Boss: Kapi Tetiklemesine Gec", false, 51)]
    public static void SwitchBossToDoorTrigger()
    {
        int zones = 0, dropped = 0;

        foreach (var zone in Object.FindObjectsByType<EndingTriggerZone>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (zone.trigger != EndingTriggerZone.What.BossScene) continue;

            zone.gameObject.SetActive(false);
            EditorUtility.SetDirty(zone);
            zones++;
            Debug.Log($"⛔ {zone.name}: kapatildi — boss artik cikis kapisinda doguyor.");
        }

        // HAVADA duran canavarlari yere indir.
        //
        // Su tabancasinin menzili 3.5; y=6'daki canavar ne yerden ne de odanin
        // en yuksek platformundan vurulabiliyor — oyuncu ates ediyor, hicbir sey
        // olmuyor. (Ayni hata ArkaKoridor'da da vardi, asagidaki Hizli
        // Duzeltme'de elle duzeltilmisti; ArkaKapi'daki 316,6 gozden kacmis.)
        foreach (var wm in Object.FindObjectsByType<WallMonster>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Vector3 p = wm.transform.position;
            if (p.y <= 4f) continue;

            Debug.Log($"⬇ {wm.name}: (x={p.x:0.#}, y={p.y:0.#}) havada duruyordu, yere indirildi.");
            wm.transform.position = new Vector3(p.x, 1f, p.z);
            EditorUtility.SetDirty(wm);
            dropped++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Tamam",
            $"Kapatilan eski boss tetigi: {zones}\n" +
            $"Yere indirilen havadaki canavar: {dropped}\n\n" +
            "Boss artik cikis kapisinda doguyor.\nCtrl+S ile kaydetmeyi unutmayin.", "Tamam");
    }

    [MenuItem("ALEV/Sahneyi Guncelle (Hizli Duzeltme)", false, 50)]
    public static void FixScene()
    {
        int fixedCount = 0;

        // 1. WallMonster'i dogru yere al (250, 6 -> 248, 1)
        foreach (var wm in Object.FindObjectsByType<WallMonster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (wm.transform.position.x >= 249f && wm.transform.position.x <= 251f && wm.transform.position.y > 5f)
            {
                wm.transform.position = new Vector3(248f, 1f, 0f);
                // Facing -1 olarak guncelle
                wm.facing = -1;
                fixedCount++;
                Debug.Log("✅ WallMonster konumu düzeltildi (248, 1).");
            }
        }

        // 2. Kopek olumu ve Boss tetikleyicilerini buyut ve anahtar sartini kaldir
        foreach (var zone in Object.FindObjectsByType<EndingTriggerZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var col = zone.GetComponent<BoxCollider2D>();
            if (col != null && col.size.y < 10f)
            {
                col.size = new Vector2(col.size.x, 20f);
                zone.transform.position = new Vector3(zone.transform.position.x, 10f, zone.transform.position.z);
            }
            
            if (zone.requiredKeyCount > 0)
            {
                zone.requiredKeyCount = 0;
            }
            fixedCount++;
            Debug.Log($"✅ {zone.name} güncellendi (Boyut: 20, Anahtar sarti kaldirildi).");
        }

        if (fixedCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Düzeltme Tamam", "Sahnedeki hatalı canavar konumu ve trigger boyutları düzeltildi!", "Tamam");
        }
        else
        {
            EditorUtility.DisplayDialog("Sonuç", "Düzeltilecek bir şey bulunamadı (Zaten düzeltilmiş olabilir).", "Tamam");
        }
    }
}
