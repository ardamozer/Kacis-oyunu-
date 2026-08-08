using UnityEngine;

/// <summary>
/// ⚠️ SADECE TEST ICIN — Kisi B'nin TestScene_B sahnesinde kullanilir.
/// Ana sahneye KOYULMAZ. Kisi A'nin gercek WaterGun'i saat 5-8'de gelecek.
///
/// Neden var: WaterGun olmadan duvar canavarinin suyla olmesini test edemem.
/// Bu, WaterGun'in yapacagi seyin en basit taklidi:
///   sol fare / F tusu basili → onune bir kutu at → icindeki
///   IWaterDamageable'lara TakeWater cagir.
///
/// Sinif adi bilerek "TestWaterSprayer" — Kisi A'nin "WaterGun" sinifiyla
/// asla cakismaz, o gelince bu script'i objeden cikarip gecebilirim.
/// </summary>
public class TestWaterSprayer : MonoBehaviour
{
    [Header("Sikma")]
    [Tooltip("Suyun ulastigi mesafe.")]
    public float range = 4f;
    [Tooltip("Su konisinin yuksekligi.")]
    public float width = 1.2f;
    [Tooltip("Saniyede verilen su hasari. PLAN.md: ~20.")]
    public float waterPerSecond = 20f;

    [Header("Gorsel")]
    [Tooltip("Sikarken Scene penceresinde mavi kutu cizilir. Game penceresinde gorunmez.")]
    public bool drawDebugBox = true;

    PlayerController controller;
    bool isSpraying;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        isSpraying = Input.GetMouseButton(0) || Input.GetKey(KeyCode.F);
        if (!isSpraying) return;

        int facing = controller != null ? controller.FacingDirection : 1;

        // Karakterin onunde bir dikdortgen alan.
        Vector2 center = (Vector2)transform.position + new Vector2(facing * range * 0.5f, 0f);
        Vector2 size = new Vector2(range, width);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        foreach (Collider2D hit in hits)
        {
            IWaterDamageable target = hit.GetComponent<IWaterDamageable>();
            if (target != null) target.TakeWater(waterPerSecond * Time.deltaTime);
        }
    }

    void OnDrawGizmos()
    {
        if (!drawDebugBox || !isSpraying) return;

        int facing = controller != null ? controller.FacingDirection : 1;
        Vector2 center = (Vector2)transform.position + new Vector2(facing * range * 0.5f, 0f);

        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.4f);
        Gizmos.DrawCube(center, new Vector3(range, width, 0.1f));
    }
}
