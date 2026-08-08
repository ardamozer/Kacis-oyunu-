using UnityEngine;

// ============================================================================
//  ORTAK SOZLESME  —  Kisi A ve Kisi B ikisi de bu arayuzlere gore kod yazar.
//
//  ⚠️ BU DOSYAYI TEK BASINIZA DEGISTIRMEYIN.
//     Degismesi gerekiyorsa once takim arkadasinizla konusun. Buradaki bir
//     imza degisirse digerinin kodu derlenmez ve ikiniz de durursunuz.
// ============================================================================


/// <summary>
/// Suyla sondurulebilen her sey bunu uygular.
/// Ornek: duvar canavari, yer atesi, dogmus ates canavari, final boss'u.
///
/// Kisi A'nin WaterGun'i, isabet ettigi objede bu arayuzu arar ve TakeWater cagirir.
/// Kisi B dusmanlarinda bu metodu yazar; can azaltip sifira inince olum efektini oynatir.
///
/// Not: Boss bu arayuzu uygular AMA canini hic azaltmaz — oyuncunun
/// "olduremiyorum" hissetmesi gereken tek dusman odur.
/// </summary>
public interface IWaterDamageable
{
    /// <param name="amount">Bu karede gelen su hasari (WaterGun saniyede ~20 verir).</param>
    void TakeWater(float amount);
}


/// <summary>
/// Ustune ziplayarak ezilebilen seyler bunu uygular.
/// Ornek: yer atesi (bar dolmadan once ezilirse soner).
///
/// Kisi A'nin PlayerController'i, bir objenin USTUNE dustugunu algilayinca
/// OnStomped cagirir ve oyuncuyu zipkatir. Kisi B ezilme sonucunu yazar.
///
/// ONEMLI: Bu, su bitse bile oyunun ilerleyebilmesini saglayan yol.
/// Yer atesleri her zaman susuz oldurulebilir kalmali.
/// </summary>
public interface IStompable
{
    void OnStomped();
}
