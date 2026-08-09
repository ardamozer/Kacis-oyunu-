using UnityEngine;

/// <summary>
/// NAMLUYU BAKILAN YONE AYNALAR.
///
/// ⚠️ NEDEN GEREKLI:
/// WaterGun.nozzle sabit bir cocuk obje ve PlayerController sadece
/// sprite'in flipX'ini ceviriyor — namlu yerinde kaliyor. Namlu govdeye
/// yakinken (local x 0.5) bu fark edilmiyordu. Ama su artik ELIN UCUNDAN
/// cikiyor (local x 0.94) ve orasi govdenin disi: aynalanmazsa sola
/// bakarken su SIRTTAN cikar.
///
/// Gorsel olmayan tarafi daha onemli: WaterGun.DamageTargetsInBeam hasar
/// kutusunu nozzle.position'dan basliyor. Namlu yanlis taraftayken kutu
/// da kayiyor, yani oyuncu gordugu yere degil baska yere hasar veriyor.
///
/// ⚠️ Update icinde; WaterGun ile arasindaki calisma sirasi garanti degil,
/// yani ilk karede bir kare gecikebilir. Surekli akan bir puskurtmede
/// gorunmez, ama tek atislik bir mekanige gecilirse burasi sabitlenmeli.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class NozzleFlip : MonoBehaviour
{
    [Tooltip("Aynalanacak namlu. Bos birakilirsa WaterGun'daki kullanilir.")]
    public Transform nozzle;

    PlayerController pc;
    float baseX;

    void Awake()
    {
        pc = GetComponent<PlayerController>();

        if (nozzle == null)
        {
            var gun = GetComponent<WaterGun>();
            if (gun != null) nozzle = gun.nozzle;
        }

        // Isaret degil BUYUKLUK saklaniyor; sahnede sola bakar halde
        // kaydedilmis olsa bile dogru calissin.
        if (nozzle != null) baseX = Mathf.Abs(nozzle.localPosition.x);
    }

    void Update()
    {
        if (nozzle == null || pc == null) return;

        Vector3 p = nozzle.localPosition;
        p.x = pc.FacingDirection < 0 ? -baseX : baseX;
        nozzle.localPosition = p;
    }
}
