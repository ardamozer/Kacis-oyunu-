# ALEV — Game Jam Planı (2 kişi / 43 saat)

## Oyun özeti

Yandan görünüm 2D platformer. Bir itfaiyecisiniz, yanan eve girip **Pamuk** adlı kediyi
kurtarmaya çalışıyorsunuz.

- Duvarlardan **ateşli canavarlar** (yanan kitaplar, tablolar) çıkıp size alev fışkırtıyor.
  Kaçarak dodge'lar ya da su tabancasıyla söndürüp öldürürsünüz.
- Yerde **yanan noktalar** var, üzerlerinde dolan bir bar. Bar dolmadan üstüne zıplayıp
  ezerseniz söner. Dolarsa canavara dönüşüp size saldırır.
- Ateş size değerse ekran sarsılır, ekranın kenarlarından alevler girer.
- Kediyi bulunca birlikte çıkışa yürürsünüz. Kedi yoldaki yer ateşlerinin üstüne atlayıp söndürür.
- Kedi sizi kurtarırken ölür.
- Çıkışta devasa bir alev sizi durdurur. Vurursunuz, **hiç azalmaz.** Ölmek üzereyken
  Pamuk'un hayaleti gelip onu söndürür ve çıkarsınız.

**Ton:** Sevimli / çizgi film. Ölüm sahnesi sessiz ve yumuşak, kanlı değil.

---

## Su sistemi (sınırlı su + toplama)

- **Su deposu: 100 birim.** Sıkarken saniyede ~15 birim gider → kesintisiz ~7 saniye.
- Ekranın sol üstünde mavi bar. %25'in altında bar kırmızı yanıp söner + uyarı sesi.

### Su kaynakları

| Kaynak | Dolum | Tekrar kullanım |
|---|---|---|
| **Musluk** (mutfak, banyo) | Saniyede 30 birim, yanında durdukça dolar | ✅ Sonsuz |
| **Akvaryum** | Anında %100 | ❌ Tek kullanımlık (kırılır) |
| **Su bidonu** | Anında %50 | ❌ Tek kullanımlık |
| **Küvet** | Saniyede 50 birim | ✅ Sonsuz |

### KİLİTLENME ÖNLEME — bu kurallara uyun

1. **Musluk ve küvet sonsuz kullanılabilir.** Oyuncu asla susuz kalamaz, geri dönüp doldurur.
2. **Her odada en az bir sonsuz kaynak erişilebilir olsun.**
3. **Yer ateşleri zıplayarak öldürülür, su gerektirmez.** Su bitse bile ilerlenebilir.
4. **Duvar canavarları öldürülmek zorunda değil**, kaçarak geçilebilir.

Yani su bitmesi = "zorlaştı", asla "bitti" değil.

### Güzel detay (vakit kalırsa)

Su %20'nin altına düşünce Pamuk en yakın musluğa doğru miyavlar. Hem faydalı hem sevimli.

---

## Kediyle bağ kurma (oyunun asıl değeri — kesinlikle kesilmeyecek)

1. **Baştan duyulsun.** Oyun başlar başlamaz uzaktan miyavlama. Yaklaştıkça sesi yükselir.
   Oyuncu daha kediyi görmeden onu arar.
2. **İsmi olsun.** Tasmasında künye: **PAMUK**. Oyunun neredeyse tek yazısı bu.
3. **Sevme tuşu: `E`.** Kediyi sever, kalp partikülü çıkar, kedi mırlar.
   **Hiçbir mekanik işlevi yok.** Oyunun en çok konuşulacak detayı bu olacak.
4. **Kedi ilk kez ateş söndürdüğünde kamera bir an ona odaklansın.**
5. **Bir kez oyuncu kediyi korusun.** Kedinin yanında ateş çıkar, oyuncu söndürür, kedi sokulur.

## Kedinin ölümü — kurallar

- **Kedi oyuncuyu kurtarırken ölmeli.** Oyuncunun hatasıyla değil. Yoksa "haksızlık" hissi olur.
- Oyuncunun arkasında görmediği bir ateş çıkar. Kedi her zamanki gibi atlar, ama bu ateş çok büyük.
- Kontrol oyuncudan alınır, **3 saniye, tüm sesler kesilir.**
- Alev yükselir, kedi görünmez olur, alev söner. Yerde sadece **tasma** kalır.
- Oyuncu tasmayı alır. Boss sahnesinde hayalet gelirken tasma parlar.

---

## Boss = dövüş DEĞİL, sahne

Boss'u AI'lı bir dövüş olarak yapmayın (8+ saat, ve zaten öldürülemiyor).

**Yapılacak:**
1. Çıkış koridoru, dev alev kapıyı kapatır
2. Oyuncu su sıkar → **can barı hiç azalmaz**
3. Boss düzenli aralıklarla alev atar, oyuncunun canı erir
4. Oyuncunun canı 1 kalbe düşünce → **otomatik sahne tetiklenir**
5. Tasma parlar, Pamuk'un mavi hayaleti ekranı geçer, boss söner, kapı açılır

**Boss sprite'ı:** Duvar canavarının sprite'ı 4 kat büyütülmüş + koyu kırmızı renk. Yeni asset yok.
**Hayalet sprite'ı:** Kedi sprite'ı mavi/beyaz tint + %50 saydam. Yeni asset yok.

---

# TEKNİK KURALLAR — İKİ KİŞİ DE OKUSUN

## Unity

- **Sürüm: `6000.0.x LTS`** — ikiniz de BİREBİR aynı sürümü kurun. Sürüm numarasını
  konuşup sabitleyin, buraya yazın: `Unity 6000.0.____`
- Template: **2D (Built-In Render Pipeline)**. URP değil.
- Proje konumu: **D: sürücüsü** (C: dolu).
- Modüller: Web Build Support ✅ / Windows Build Support ✅ / dil paketleri ❌

## Proje ayarları (Kişi A bir kere yapar)

`Edit → Project Settings → Editor`:
- **Asset Serialization → Force Text**
- **Version Control → Visible Meta Files**

## ⚠️ EN ÖNEMLİ KURAL: SAHNE SAHİPLİĞİ

Unity'de aynı sahne dosyasını iki kişi düzenleyip birleştirmeye çalışırsa **merge cehennemi**
olur ve saatlerce kaybedersiniz.

| Sahne | Sahibi | Diğeri |
|---|---|---|
| `Scenes/MainScene.unity` | **Kişi A** | ❌ AÇMASIN BİLE |
| `Scenes/TestScene_B.unity` | **Kişi B** | ❌ AÇMASIN BİLE |

**Kişi B düşmanları ve kediyi kendi test sahnesinde geliştirir, PREFAB olarak kaydeder.**
Kişi A o prefabları alıp MainScene'e yerleştirir. Kimse diğerinin sahnesine dokunmaz.

## Klasör yapısı

```
Assets/
  Scripts/
    Shared/      GameContracts.cs      <- ORTAK, saat 1'de yazılır, sonra DEĞİŞMEZ
    Player/      Kişi A
    Systems/     Kişi A (GameManager, UI, Kamera, Ses)
    Enemies/     Kişi B
    Cat/         Kişi B
    Ending/      Kişi B
  Prefabs/
    Player/      Kişi A
    Enemies/     Kişi B
    Cat/         Kişi B
  Scenes/
    MainScene.unity      Kişi A
    TestScene_B.unity    Kişi B
  Art/           ortak (Kenney assetleri)
  Audio/         ortak
```

**Kural: kimse diğerinin klasöründeki dosyayı düzenlemez.** Bir şey lazımsa konuşun.

## Tag ve Layer'lar (saat 1'de ikisi de oluştursun)

Tag: `Player` · `Enemy` · `WaterSource` · `Cat`
Layer: `Ground` · `Player` · `Enemy`

## ORTAK SÖZLEŞME — `Assets/Scripts/Shared/GameContracts.cs`

Bu dosyayı saat 1'de birlikte yazın. **Sonra kimse tek başına değiştirmesin.**
İkiniz de bu imzalara göre kod yazacaksınız, böylece parçalar ilk denemede birleşir.

```csharp
using UnityEngine;

/// Suyla söndürülebilen her şey (duvar canavarı, yer ateşi, doğmuş canavar, boss)
public interface IWaterDamageable
{
    void TakeWater(float amount);
}

/// Zıplayarak ezilebilen şeyler (yer ateşi)
public interface IStompable
{
    void OnStomped();
}
```

### Kişi A'nın yazacağı, Kişi B'nin çağıracağı imzalar

```csharp
public class PlayerHealth : MonoBehaviour
{
    public void TakeDamage(int amount);   // düşmanlar bunu çağırır
    public int CurrentHealth { get; }     // final sahnesi bunu okur
}

public class WaterTank : MonoBehaviour
{
    public float Current { get; }
    public float Max { get; }
    public bool TrySpend(float amount);   // su yeterse harcar, true döner
    public void Refill(float amount);
}
```

### Kişi B'nin yazacağı, Kişi A'nın çağıracağı imzalar

```csharp
public class CatCompanion : MonoBehaviour
{
    public void OnPetted();               // A'nın E tuşu bunu çağırır
    public void StartFollowing();         // kedi bulununca A tetikler
}

public class EndingSequence : MonoBehaviour
{
    public void TriggerCatDeath();        // A tetikler
    public void TriggerBossScene();       // A tetikler
}
```

---

## Git kuralları

- Tek branch: **`main`**. İkiniz de main'e push'layın.
  (Farklı dosyalarda çalıştığınız için çakışma olmaz. Branch/merge öğrenmek jam'de zaman kaybı.)
- **Her push'tan önce `git pull`.** İstisnasız.
- Sık commit atın — 30-45 dakikada bir. Küçük commit = kolay geri dönüş.
- `.gitignore` olarak GitHub'ın **Unity** şablonunu kullanın. `Library/` klasörü repoya
  GİRMEMELİ (gigabaytlarca dosya).
- Git LFS'e gerek yok, 2D sprite'lar küçük.

```bash
git pull
git add .
git commit -m "kedi takip sistemi"
git push
```

---

# ZAMAN PLANI

Saatler jam başlangıcından itibaren. Uyku bloklarını **gerçekten uyuyun**, jam'i uykusuzluk kaybettirir.

## Kişi A — Oyuncu, Sistemler, Seviye

| Saat | İş |
|---|---|
| 0-1 | Unity kurulum, proje oluştur, git repo, `GameContracts.cs`, tag/layer |
| 1-5 | `PlayerController`: yürüme, zıplama, yön değiştirme. **His ayarı burada yapılır.** |
| 5-8 | `WaterGun` + `WaterTank`: su sıkma, su barı, `IWaterDamageable` çağrısı |
| 8-10 | `WaterSource`: musluk/akvaryum/küvet doldurma |
| 10-12 | `PlayerHealth`: 3 kalp, hasar, ekran sarsıntısı, kenar alev efekti |
| 12-14 | **UYKU** |
| 14-18 | `MainScene`: evin kat planı, odalar, platformlar, asset yerleştirme |
| 18-21 | UI: su barı, kalpler, `E` sevme tuşu, kamera takibi |
| 21-24 | B'nin prefablarını sahneye yerleştir, seviyeyi baştan sona oynanır yap |
| **24-26** | **İLK WEBGL BUILD + itch.io'ya yükle.** ⚠️ Ertelemeyin. |
| 26-28 | **UYKU** |
| 28-33 | Ses (miyavlama, ateş çıtırtısı, su, müzik), partiküller, cila |
| 33-38 | B ile birlikte denge ayarı, bug avı, tutorial |
| 38-41 | Final build, itch.io sayfası, ekran görüntüleri, açıklama, asset kredileri |
| 41-43 | Yedek süre / son bug'lar |

## Kişi B — Düşmanlar, Kedi, Final

| Saat | İş |
|---|---|
| 0-1 | Unity kurulum, repoyu klonla, `TestScene_B` oluştur, `GameContracts.cs`'i oku |
| 1-4 | `WallMonster`: duvardan çıkma animasyonu, alev fırlatma, `TakeWater` ile ölme → **prefab** |
| 4-8 | `FloorFire`: dolan bar, `OnStomped` ile ölme, dolunca `FireMonster`'a dönüşme → **prefab** |
| 8-10 | `FireMonster`: doğan canavarın oyuncuya saldırısı → **prefab** |
| 10-12 | **UYKU** |
| 12-16 | `CatCompanion`: takip (basit `MoveTowards`), ateş söndürme, `OnPetted`, miyavlama |
| 16-19 | Kediyi bulma anı, kamera odaklanması, `StartFollowing` |
| **19-24** | **`EndingSequence`: kedinin ölümü + boss sahnesi + hayalet + final ekranı** ⚠️ |
| 24-26 | **UYKU** |
| 26-30 | Boss sahnesi cilası: tasma parlaması, sessizlik anı, ekran beyaza patlaması |
| 30-33 | Düşman ses efektleri, ölüm partikülleri |
| 33-38 | A ile birlikte denge ayarı, bug avı |
| 38-41 | Test, test, test. Baştan sona 5 kez oynayın. |
| 41-43 | Yedek süre |

---

## KIRMIZI ÇİZGİLER

1. **Saat 26'da itch.io'da oynanabilir bir build OLACAK.** Yarım olsa bile.
   Unity WebGL build'i ilk seferde sık sık patlar, bunu saat 40'ta keşfetmeyin.
2. **Final sahnesi (kedi ölümü + boss) saat 24'te kaba haliyle bitmiş olacak.**
   Klasik jam felaketi: bütün dövüşü yapıp finali yetiştirememek. Final bu oyunun tek varlık sebebi.
3. **Asset seçimi saat 2'de biter.** kenney.nl'den TEK paket. Bir daha asset aramayın.
4. **Yeni özellik eklemek yasak.** Saat 20'den sonra fikir gelirse not defterine yazın, jam sonrası.
5. **Uyku bloklarını atlamayın.** Saat 35'te uykusuz yazılan kod, saat 38'de silinen koddur.

## Kesilecekler listesi (zaman daralırsa bu sırayla kesin)

1. Su %20'de kedinin musluğa miyavlaması
2. Oyuncunun kediyi koruduğu sahne
3. `FireMonster` (yer ateşi dolunca hiç canavar doğmasın, sadece hasar versin)
4. Duvar canavarı çeşitliliği (tek tip yeter)
5. Müzik (ses efektleri kalsın, müzik gitsin)

**ASLA kesilmeyecekler:** sevme tuşu, miyavlama sesi, kedinin ölümü, hayalet sahnesi.
