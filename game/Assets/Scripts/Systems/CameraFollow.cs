using UnityEngine;

/// <summary>
/// Kamerayi oyuncuya yumusak sekilde takip ettirir.
///
/// ⚠️ Bu script KAMERANIN KENDISINE DEGIL, kameranin PARENT'i olan
/// "CameraRig" objesine takilir. Kamera cocuk olarak icinde durur ve
/// CameraShake onu sarsar. Ikisi ayni objede olursa birbirini ezer.
///
///   CameraRig      ← CameraFollow  (takip eder)
///     └ Main Camera ← CameraShake  (sarsar)
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    [Tooltip("Bos birakin — Tag='Player' olan objeyi kendi bulur.")]
    public Transform target;

    [Header("Takip")]
    [Tooltip("Kac saniyede hedefe yetissin. Kucuk = sert, buyuk = tembel. 0.15-0.3 iyi.")]
    public float smoothTime = 0.2f;

    [Tooltip("Kameranin oyuncuya gore kaymasi. Y'yi biraz yukari almak " +
             "onu gormeyi kolaylastirir.")]
    public Vector2 offset = new Vector2(0f, 1f);

    [Header("Olu Bolge")]
    [Tooltip("Oyuncu bu kutunun icinde kaldigi surece kamera kipirdamaz. " +
             "Kucuk ziplamalarda kameranin titremesini onler.")]
    public Vector2 deadZone = new Vector2(1.5f, 1.2f);

    [Header("Sinirlar")]
    [Tooltip("Acikken kamera bu dikdortgenin disina cikmaz — evin disini " +
             "gostermesini engeller. Seviyeyi kurduktan sonra ayarlayin.")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-20f, -10f);
    public Vector2 maxBounds = new Vector2(20f, 10f);

    Vector3 velocity;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Ilk karede kayarak gelmesin, dogrudan yerine otursun.
        if (target != null)
        {
            Vector3 p = DesiredPosition(target.position);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = DesiredPosition(target.position);

        // ---- Olu bolge ----
        // Hedef, kameranin merkezindeki kutunun icindeyse o eksende kamera durur.
        float x = transform.position.x;
        float y = transform.position.y;

        if (Mathf.Abs(desired.x - x) > deadZone.x * 0.5f)
            x = desired.x - Mathf.Sign(desired.x - x) * deadZone.x * 0.5f;

        if (Mathf.Abs(desired.y - y) > deadZone.y * 0.5f)
            y = desired.y - Mathf.Sign(desired.y - y) * deadZone.y * 0.5f;

        Vector3 goal = new Vector3(x, y, transform.position.z);

        // ---- Sinirlar ----
        if (useBounds)
        {
            goal.x = Mathf.Clamp(goal.x, minBounds.x, maxBounds.x);
            goal.y = Mathf.Clamp(goal.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }

    Vector3 DesiredPosition(Vector3 targetPos)
    {
        return new Vector3(targetPos.x + offset.x, targetPos.y + offset.y, transform.position.z);
    }

    void OnDrawGizmosSelected()
    {
        // Olu bolge
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(deadZone.x, deadZone.y, 0.1f));

        // Sinirlar
        if (!useBounds) return;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
        Vector3 center = (Vector3)((minBounds + maxBounds) * 0.5f);
        Vector3 size = (Vector3)(maxBounds - minBounds);
        Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.1f));
    }
}
