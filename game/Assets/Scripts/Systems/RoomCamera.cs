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

    [Tooltip("Kameranin odanin ALTINDAN ne kadar tasabilecegi.\n\n" +
             "⚠️ NEDEN GEREKLI: standart oda 11 birim, kameranin gorus\n" +
             "yuksekligi de 11 (ortho 5.5). Kamera oda sinirlari icinde\n" +
             "tutulunca Y'de tam ortaya (5.5) KILITLENIYOR. Oyuncu y~1'de\n" +
             "durdugu icin ekranin en alt %9'unda kaliyor, ustteki 7 birim\n" +
             "bombos tavan oluyor — oyun 'kaymis' gorunuyor.\n\n" +
             "Bu deger kadar asagi tasmaya izin verince kamera oyuncuya\n" +
             "yaklasiyor. Zemin slabi y=-1'e kadar dolu, yani 1-2 birim\n" +
             "tasmak bosluk gostermiyor.\n\n" +
             "0 = eski davranis (kamera odaya tam kilitli).")]
    public float floorBias = 1.5f;

    /// <summary>Su an gosterilen oda.</summary>
    public Room CurrentRoom { get; private set; }

    Camera cam;
    Room[] rooms;
    PlayerController controller;

    float transitionTimer;
    Vector3 transitionFrom;
    float velocityX, velocityY;

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
            Vector2 p = DesiredPos(CurrentRoom);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
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

        Vector2 goal2 = DesiredPos(CurrentRoom);
        float goalX = goal2.x;
        float goalY = goal2.y;

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
            // ---- Oda icinde takip ----
            // Kucuk odalarda (yukseklik = ekran yuksekligi) dikey hic oynamaz.
            // Buyuk odalarda (Salon, Cocuk odasi — iki kat yuksek) kayar,
            // ama gorus alani oda sinirini asmaz.
            float x = Mathf.SmoothDamp(transform.position.x, goalX, ref velocityX, followSmooth);
            float y = Mathf.SmoothDamp(transform.position.y, goalY, ref velocityY, followSmooth);
            transform.position = new Vector3(x, y, transform.position.z);
        }
    }

    /// <summary>
    /// Kameranin bu odadaki konumu: oyuncuyu takip eder ama gorus alani
    /// odanin disina tasmayacak sekilde kisitlanir.
    ///
    /// Oda ekran kadarsa eksen kilitlenir (hic oynamaz), buyukse kayar.
    /// Boylece kucuk odalar sabit cerceve, Salon gibi buyuk odalar
    /// gezilebilir alan gibi hissettirir.
    /// </summary>
    Vector2 DesiredPos(Room room)
    {
        float halfH = (cam != null && cam.orthographic) ? cam.orthographicSize : 7f;
        float halfW = (cam != null && cam.orthographic) ? halfH * cam.aspect : 12f;

        float minX = room.MinX + halfW, maxX = room.MaxX - halfW;

        // Alt siniri floorBias kadar gevsetiyoruz. Oda yuksekligi ekran
        // yuksekligine ESITKEN (standart oda 11, ortho 5.5) minY == maxY
        // oluyor ve kamera ortaya kilitleniyor; oyuncu ekranin dibinde,
        // ustte 7 birim bos tavan kaliyordu.
        float minY = room.MinY + halfH - Mathf.Max(0f, floorBias);
        float maxY = room.MaxY - halfH;

        float x = minX > maxX ? room.Center.x : Mathf.Clamp(target.position.x, minX, maxX);

        // Oyuncuyu ekranin ortasina almaya calisiyoruz, sonra odaya sigdiriyoruz.
        float y = minY > maxY
            ? room.Center.y - Mathf.Max(0f, floorBias)
            : Mathf.Clamp(target.position.y, minY, maxY);

        return new Vector2(x, y);
    }

    Room FindRoomFor(Vector3 pos)
    {
        if (rooms == null) return null;
        foreach (var r in rooms)
            if (r != null && r.Contains(pos)) return r;
        return null;
    }
}
