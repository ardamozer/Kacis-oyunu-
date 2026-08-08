using UnityEngine;

/// <summary>
/// Oda bazli kamera. Oyuncu hangi odadaysa kamera o odayi gosterir;
/// kapidan gecince yeni odaya kayar.
///
/// Neden takip eden kamera degil:
///   - Odanin disi ASLA gorunmez (zeminin alti, evin disi, bos alanlar)
///   - Cerceveleme her odada dogru, elle sinir ayarlamak gerekmez
///   - Her ekran oraninda calisir — itch.io'nun 960x600'unde de, 16:9'da da
///   - "Kapidan gectim, yeni bolum" hissi bedava gelir
///
/// CameraRig'e takilir; kamera onun cocugudur (CameraShake orada sarsar).
/// </summary>
public class RoomCamera : MonoBehaviour
{
    [Header("Hedef")]
    [Tooltip("Bos birakin — Tag='Player' olani kendi bulur.")]
    public Transform target;

    [Header("Oda gecisi")]
    [Tooltip("Yeni odaya kayma suresi. 0.25-0.4 iyi. " +
             "Cok kisa olursa sert, cok uzun olursa oyuncu bekler.")]
    public float transitionDuration = 0.3f;

    [Tooltip("Gecis sirasinda oyuncunun kontrolu kesilsin mi? " +
             "Kapali tutun — jam oyununda beklemek sinir bozar.")]
    public bool freezePlayerDuringTransition = false;

    [Header("Oda icinde takip")]
    [Tooltip("Oda icindeki yatay takibin yumusakligi.")]
    public float followSmooth = 0.15f;

    /// <summary>Su an gosterilen oda.</summary>
    public Room CurrentRoom { get; private set; }

    Camera cam;
    Room[] rooms;
    PlayerController controller;

    float transitionTimer;
    Vector3 transitionFrom;
    float velocityX;

    void Awake()
    {
        cam = GetComponentInChildren<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                target = p.transform;
                controller = p.GetComponent<PlayerController>();
            }
        }

        RefreshRooms();

        // Baslangicta dogru odaya ANINDA otur — kaymayla gelmesin.
        CurrentRoom = FindRoomFor(target != null ? target.position : Vector3.zero);
        if (CurrentRoom != null)
        {
            transform.position = new Vector3(DesiredX(CurrentRoom), CurrentRoom.Center.y,
                                             transform.position.z);
        }
    }

    /// <summary>Odalar sonradan eklendiyse listeyi tazeler.</summary>
    public void RefreshRooms()
    {
        rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (rooms == null || rooms.Length == 0) RefreshRooms();

        Room room = FindRoomFor(target.position);

        // Oyuncu hicbir odanin icinde degilse (bosluk, gecis ani) son odada kal.
        if (room == null) room = CurrentRoom;
        if (room == null) return;

        if (room != CurrentRoom)
        {
            CurrentRoom = room;
            transitionFrom = transform.position;
            transitionTimer = transitionDuration;

            if (freezePlayerDuringTransition && controller != null)
                controller.CanControl = false;
        }

        float goalX = DesiredX(CurrentRoom);
        float goalY = CurrentRoom.Center.y;

        if (transitionTimer > 0f)
        {
            // ---- Oda gecisi ----
            transitionTimer -= Time.deltaTime;
            float k = 1f - Mathf.Clamp01(transitionTimer / Mathf.Max(transitionDuration, 0.0001f));
            k = k * k * (3f - 2f * k);   // SmoothStep

            transform.position = new Vector3(
                Mathf.Lerp(transitionFrom.x, goalX, k),
                Mathf.Lerp(transitionFrom.y, goalY, k),
                transform.position.z);

            if (transitionTimer <= 0f && freezePlayerDuringTransition && controller != null)
                controller.CanControl = true;
        }
        else
        {
            // ---- Oda icinde yatay takip ----
            // Dikeyde HIC oynamiyoruz: oda yuksekligi = ekran yuksekligi,
            // yani zeminin alti veya tavanin ustu asla gorunmez.
            float x = Mathf.SmoothDamp(transform.position.x, goalX, ref velocityX, followSmooth);
            transform.position = new Vector3(x, goalY, transform.position.z);
        }
    }

    /// <summary>
    /// Kameranin bu odadaki yatay konumu: oyuncuyu takip eder ama
    /// gorus alani odanin disina tasmayacak sekilde kisitlanir.
    /// </summary>
    float DesiredX(Room room)
    {
        float halfW = (cam != null && cam.orthographic)
            ? cam.orthographicSize * cam.aspect
            : 10f;

        float min = room.MinX + halfW;
        float max = room.MaxX - halfW;

        // Oda ekrandan darsa ortala.
        if (min > max) return room.Center.x;

        return Mathf.Clamp(target.position.x, min, max);
    }

    Room FindRoomFor(Vector3 pos)
    {
        if (rooms == null) return null;
        foreach (var r in rooms)
            if (r != null && r.Contains(pos)) return r;
        return null;
    }
}
