using UnityEditor;
using UnityEngine;

public static class AlevSceneFixer
{
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
