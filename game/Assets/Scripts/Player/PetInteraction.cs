using UnityEngine;

/// <summary>
/// `E` tusu — Karabas'i sevme.
///
/// Hicbir mekanik islevi YOK ve olmamali. Amaci tamamen duygusal:
/// oyuncu kopegi sevdikce ona baglanir, sonda oldugunde acitir.
/// PLAN.md'de "asla kesilmeyecekler" listesinde.
///
/// Kopegi bulma isini kendi yapar, Inspector'da bir sey baglamaniza gerek yok.
/// </summary>
public class PetInteraction : MonoBehaviour
{
    [Header("Sevme")]
    public KeyCode petKey = KeyCode.E;

    [Tooltip("Bu mesafeden yakinken sevilebilir.")]
    public float petRange = 2f;

    [Tooltip("Iki sevme arasindaki bekleme. Spam'i engeller.")]
    public float petCooldown = 0.6f;

    /// <summary>Kopek sevme menzilinde mi? UI ipucu ('E — Sev') bunu okuyabilir.</summary>
    public bool DogInRange { get; private set; }

    DogCompanion dog;
    PlayerController controller;
    float lastPetTime = -99f;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        // Kopek sahneye sonradan girebilir (sikismis halden kurtulunca),
        // o yuzden bulamadiysak her karede tekrar ariyoruz.
        if (dog == null) dog = FindFirstObjectByType<DogCompanion>();

        DogInRange = dog != null &&
                     Vector2.Distance(transform.position, dog.transform.position) <= petRange;

        if (!DogInRange) return;
        if (controller != null && !controller.CanControl) return;   // final sahnesinde kapali
        if (Time.time - lastPetTime < petCooldown) return;

        if (Input.GetKeyDown(petKey))
        {
            lastPetTime = Time.time;
            dog.OnPetted();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.6f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, petRange);
    }
}
