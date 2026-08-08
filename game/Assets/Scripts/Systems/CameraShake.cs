using UnityEngine;

/// <summary>
/// Kamerayi sarsar. Jam'de en yuksek "his kazanci / emek" oranina sahip efekt.
/// Ates hasari, patlama, cokme gibi anlarda cagirin.
///
/// Kullanim:  CameraShake.Instance.Shake(0.25f, 0.4f);
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Tooltip("Ates yaklastikca surekli hafif sarsinti. 0 = kapali.")]
    public float ambientMagnitude = 0f;

    Vector3 basePosition;
    float duration;
    float magnitude;
    float timer;

    void Awake()
    {
        Instance = this;
        basePosition = transform.localPosition;
    }

    /// <param name="dur">Sarsinti suresi (sn). 0.2-0.4 arasi iyi.</param>
    /// <param name="mag">Siddet (dunya birimi). 0.1 hafif, 0.5 sert.</param>
    public void Shake(float dur, float mag)
    {
        // Devam eden bir sarsintiyi ezme — daha siddetli olan kazansin.
        if (timer > 0f && mag < magnitude) return;

        duration = dur;
        magnitude = mag;
        timer = dur;
    }

    void LateUpdate()
    {
        Vector3 offset = Vector3.zero;

        if (timer > 0f)
        {
            timer -= Time.deltaTime;

            // Sarsinti sonuna dogru sonsun — aniden kesilirse ucuz durur.
            float strength = magnitude * (duration <= 0f ? 0f : timer / duration);
            offset += (Vector3)Random.insideUnitCircle * strength;
        }

        if (ambientMagnitude > 0f)
        {
            offset += (Vector3)Random.insideUnitCircle * ambientMagnitude;
        }

        transform.localPosition = basePosition + offset;
    }
}
