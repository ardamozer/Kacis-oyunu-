using UnityEngine;

/// <summary>
/// EVDEN CIKIS — oyunun kazanma noktasi.
///
/// Giris kapisinin oldugu yere konur. Oyuncu buraya varinca final ekrani
/// baslar.
///
/// ⚠️ Boss cozulmeden tetiklenmemeli. Boss hala aktifken oyuncu bir sekilde
/// buraya ulasirsa oyun "kazandin" der ve final sahnesi mahvolur.
/// Kontrol: EndingSequence'in boss objesi hala aktif mi?
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EscapeTrigger : MonoBehaviour
{
    [Header("Onkosul")]
    [Tooltip("Bos birakin — sahnedeki EndingSequence'i kendi bulur.")]
    public EndingSequence ending;

    [Tooltip("Acikken, boss hala aktifse cikis calismaz. Acik birakin.")]
    public bool requireBossResolved = true;

    [Header("Final")]
    [Tooltip("Bos birakin — sahnedeki GameEnding'i bulur, yoksa kendi olusturur.")]
    public GameEnding gameEnding;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake()
    {
        if (ending == null) ending = FindFirstObjectByType<EndingSequence>();
        if (gameEnding == null) gameEnding = FindFirstObjectByType<GameEnding>();

        if (gameEnding == null)
        {
            var go = new GameObject("GameEnding");
            gameEnding = go.AddComponent<GameEnding>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (requireBossResolved && ending != null &&
            ending.bossObject != null && ending.bossObject.activeSelf)
        {
            Debug.Log("[EscapeTrigger] Boss hala aktif — cikis henuz acilmadi.");
            return;
        }

        Debug.Log("[EscapeTrigger] Oyuncu evden cikti. Final basliyor.");
        gameEnding.Finish();
    }

    void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
