using UnityEngine;

/// <summary>
/// Yerlestirme isareti. Kisi B'nin prefablari geldiginde nereye
/// konacaklarini gosterir. Oyunda hicbir sey yapmaz, sadece Scene
/// penceresinde gorunur.
///
/// Prefab geldiginde: prefabi bu isaretin konumuna surukleyin,
/// sonra isareti silin.
/// </summary>
public class SpawnMarker : MonoBehaviour
{
    public enum Kind
    {
        WallMonster,   // duvar canavari — suyla olur
        FloorFire,     // yer atesi — kucukken ezilir, buyuyunce sadece su
        FloorFireGrown,// yer atesi — oyuna buyumus baslar
        Dog,           // Karabas
        CoveringFire,  // Karabas'i orten ates
        Boss,          // cikistaki dev alev
        DogSilhouette, // Antre'den gorunen siluet
        FlameWall,     // giris kapisini kapatan alev duvari
        PlayerStart
    }

    public Kind kind = Kind.WallMonster;

    [Tooltip("Bu isaret hakkinda not — neden burada.")]
    [TextArea(2, 4)]
    public string note;

    static Color ColorFor(Kind k) => k switch
    {
        Kind.WallMonster   => new Color(0.9f, 0.25f, 0.2f),
        Kind.FloorFire     => new Color(1f, 0.55f, 0.1f),
        Kind.FloorFireGrown=> new Color(1f, 0.35f, 0.1f),
        Kind.Dog           => new Color(0.4f, 0.85f, 0.4f),
        Kind.CoveringFire  => new Color(1f, 0.35f, 0.1f),
        Kind.Boss          => new Color(0.7f, 0.1f, 0.1f),
        Kind.DogSilhouette => new Color(0.3f, 0.6f, 0.4f),
        Kind.FlameWall     => new Color(1f, 0.45f, 0.15f),
        Kind.PlayerStart   => new Color(0.3f, 0.7f, 1f),
        _                  => Color.white
    };

    void OnDrawGizmos()
    {
        Gizmos.color = ColorFor(kind);
        Gizmos.DrawWireSphere(transform.position, 0.55f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.9f,
                        transform.position + Vector3.up * 0.9f);
    }
}
