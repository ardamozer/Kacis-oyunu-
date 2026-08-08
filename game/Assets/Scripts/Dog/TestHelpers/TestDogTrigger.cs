using UnityEngine;

/// <summary>
/// ⚠️ SADECE TEST ICIN — Kisi B'nin TestScene_B sahnesinde kullanilir.
///
/// Kisi A'nin gercek tetikleyicileri (sikisma-kurtarma etkilesimi, E sevme
/// tusu sistemi) henuz yok. DogCompanion'in sozlesme metodlarini (OnPetted,
/// OnRescued, StartFollowing) elle tetiklemek icin bu script var.
///
/// DogCompanion objesinin USTUNE eklenir. Player yakinken:
///   E — OnPetted()      (sevme, her zaman calisir)
///   R — OnRescued()     (sikismis atesi sondurdugunu simule eder)
///   T — StartFollowing()(kurtarma animasyonu bittigini simule eder)
///
/// Kisi A gercek sistemleri push'layinca bu script DogCompanion'dan
/// cikarilir, hicbir gercek kod degismez.
/// </summary>
[RequireComponent(typeof(DogCompanion))]
public class TestDogTrigger : MonoBehaviour
{
    [Tooltip("Bu mesafenin disindaysa tus tepki vermez — gercek oyunda da yakinlik sarti olacak.")]
    public float interactRange = 3f;

    DogCompanion dog;
    Transform player;

    void Awake()
    {
        dog = GetComponent<DogCompanion>();
    }

    void Start()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null) player = found.transform;
    }

    void Update()
    {
        if (player == null) return;
        if (Vector3.Distance(player.position, transform.position) > interactRange) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            dog.OnPetted();
            Debug.Log("[TEST] OnPetted() cagrildi.");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            dog.OnRescued();
            Debug.Log("[TEST] OnRescued() cagrildi.");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            dog.StartFollowing();
            Debug.Log("[TEST] StartFollowing() cagrildi.");
        }
    }
}
