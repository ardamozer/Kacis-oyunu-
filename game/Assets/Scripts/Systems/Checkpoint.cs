using UnityEngine;

/// <summary>
/// Oyuncu buraya degince olum noktasi burasi olur.
///
/// Odalarin girisine konur. Gorunmez — oyuncuya "checkpoint aldin"
/// demiyoruz, cunku bu oyunda checkpoint bir odul degil, sadece
/// cezanin makul olmasi. Gostermek gerilimi bosuna kiriyor.
///
/// Otomatik yerlestirme: ALEV → Checkpoint'leri Odalara Kur
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Console'da hangi checkpoint oldugu gorunsun diye.")]
    public string checkpointName = "Checkpoint";

    [Tooltip("Oyuncunun dogacagi nokta. Bos birakilirsa bu objenin konumu.\n" +
             "Zeminin BIRAZ USTUNDE olmali, yoksa oyuncu zemine gomulu dogar.")]
    public Transform spawnPoint;

    void Reset()
    {
        // Inspector'dan elle eklenirse collider dogru kurulsun.
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 8f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Vector3 p = spawnPoint != null ? spawnPoint.position : transform.position;
        CheckpointManager.Set(p, checkpointName);
    }

    void OnDrawGizmos()
    {
        Vector3 p = spawnPoint != null ? spawnPoint.position : transform.position;

        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.7f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 8f, 0.1f));
        Gizmos.DrawWireSphere(p, 0.4f);
    }
}
