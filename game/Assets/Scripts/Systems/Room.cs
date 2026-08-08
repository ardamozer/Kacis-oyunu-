using UnityEngine;

/// <summary>
/// Bir oda = bir ekran.
///
/// Yukseklik kameranin gorus yuksekligiyle AYNI (12 birim, ortho size 6).
/// Boylece kamera dikeyde hic oynamiyor ve odanin disi asla gorunmuyor.
/// Genislik biraz daha fazla (26) — yatayda hafif kaydirma olur, bu iyi:
/// oda tamamen sabit olsa hareket hissi kaybolur.
/// </summary>
public class Room : MonoBehaviour
{
    [Tooltip("Odanin adi — sadece Hierarchy ve hata ayiklama icin.")]
    public string roomName = "Oda";

    [Tooltip("Yukseklik kameranin gorus yuksekligine esit olmali (ortho size x 2).")]
    public Vector2 size = new Vector2(26f, 12f);

    public Vector2 Center => transform.position;
    public float MinX => transform.position.x - size.x * 0.5f;
    public float MaxX => transform.position.x + size.x * 0.5f;
    public float MinY => transform.position.y - size.y * 0.5f;
    public float MaxY => transform.position.y + size.y * 0.5f;

    public bool Contains(Vector3 p)
    {
        return p.x >= MinX && p.x <= MaxX && p.y >= MinY && p.y <= MaxY;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0.1f));
    }
}
