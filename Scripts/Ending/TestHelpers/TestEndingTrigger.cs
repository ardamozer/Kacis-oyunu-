using UnityEngine;

/// <summary>
/// ⚠️ SADECE TEST ICIN — Kisi B'nin TestScene_B sahnesinde kullanilir.
///
/// Kisi A'nin gercek tetikleyicileri (oyuncu arkasindaki gizli ates tuzagi,
/// cikis koridoru trigger'i) henuz MainScene'de yok. EndingSequence'i elle
/// tetiklemek icin bu script var.
///
///   1 — TriggerDogDeath()
///   2 — TriggerBossScene()
///
/// Boss sahnesindeyken oyuncunun canini 1'e dusurmek icin normal sekilde
/// bossun alevine (veya TestWaterSprayer'la kendi atesine) maruz kalmak
/// yeterli — TestPlayerDummy.CurrentHealth zaten EndingSequence tarafindan
/// okunuyor, ekstra tus gerekmiyor.
/// </summary>
public class TestEndingTrigger : MonoBehaviour
{
    public EndingSequence ending;

    void Awake()
    {
        if (ending == null) ending = GetComponent<EndingSequence>();
    }

    void Update()
    {
        if (ending == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ending.TriggerDogDeath();
            Debug.Log("[TEST] TriggerDogDeath() cagrildi.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ending.TriggerBossScene();
            Debug.Log("[TEST] TriggerBossScene() cagrildi.");
        }
    }
}
