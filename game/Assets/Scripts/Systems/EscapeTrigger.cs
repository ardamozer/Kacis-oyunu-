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

    void OnTriggerEnter2D(Collider2D other) => TryEscape(other);

    /// <summary>
    /// Kapiya dayandigi surece denemeye devam etsin. Boss burada dogdugu icin
    /// oyuncu zaten kapinin dibinde; sahne bitince tekrar cikip girmesini
    /// beklemek anlamsiz olurdu.
    /// </summary>
    void OnTriggerStay2D(Collider2D other) => TryEscape(other);

    void TryEscape(Collider2D other)
    {
        if (finished) return;
        if (!other.CompareTag("Player")) return;

        if (requireBossResolved && ending != null && !ending.BossResolved)
        {
            // ⭐ BOSS BURADA DOGAR.
            //
            // Eskiden boss bir x cizgisini gecince (x=300) kendiliginden
            // cikiyordu; oyuncu "yururken oldu" hissi aliyordu. Sonra "odayi
            // temizleyince" denendi ve ulasilamayan bir canavar yuzunden oyun
            // kilitlendi.
            //
            // Simdi tetik oyuncunun NIYETI: cikmaya calisiyor. Kapiya uzaniyor,
            // dev alev onu kesiyor. Sebep-sonuc oyuncunun kendi hamlesinden
            // dogdugu icin sahne "basima geldi" degil "yaptim" gibi okunuyor.
            if (!ending.BossActive)
            {
                Debug.Log("[EscapeTrigger] Oyuncu cikmaya calisti — BOSS BELIRIYOR.");
                ending.TriggerBossScene();
            }
            else
            {
                Debug.Log("[EscapeTrigger] Boss hala ayakta — cikis kapali.");
            }
            return;
        }

        finished = true;
        Debug.Log("[EscapeTrigger] Oyuncu evden cikti. Final basliyor.");
        gameEnding.Finish();
    }

    bool finished;

    void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
