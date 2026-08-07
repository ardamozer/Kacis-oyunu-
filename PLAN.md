# ALEV — Game Jam Planı (2 kişi / 43 saat)

## Oyun özeti

Yandan görünüm 2D platformer. Bir itfaiyecisiniz, yanan eve girip **Karabaş** adlı köpeği
kurtarmaya çalışıyorsunuz.

- Duvarlardan **ateşli canavarlar** (yanan kitaplar, tablolar) çıkıp size alev fışkırtıyor.
  Kaçarak dodge'lar ya da su tabancasıyla söndürüp öldürürsünüz.
- Yerde **yanan noktalar** var, üzerlerinde dolan bir bar. Bar dolmadan üstüne zıplayıp
  ezerseniz söner. Dolarsa canavara dönüşüp size saldırır.
- Ateş size değerse ekran sarsılır, ekranın kenarlarından alevler girer.
- Köpeği bulursunuz — **sıkışmış durumda**, alevi söndürüp onu kurtarırsınız.
- Birlikte çıkışa yürürsünüz. Karabaş yoldaki yer ateşlerinin üstüne atlayıp söndürür.
- Karabaş sizi kurtarırken ölür.
- Çıkışta devasa bir alev sizi durdurur. Vurursunuz, **hiç azalmaz.** Ölmek üzereyken
  Karabaş'ın hayaleti gelip onu söndürür ve çıkarsınız.

**Ton:** Sevimli / çizgi film. Ölüm sahnesi sessiz ve yumuşak, kanlı değil.

**Neden köpek:** Oyunun finali "sadakat ölümden sonra da sürüyor" üzerine kurulu.
Bu köpek arketipi. Ayrıca peşinizden gelmesi doğal (escort AI'a hikâye örtüsü),
yanan evde siluet olarak daha okunaklı.

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
2. **Her odada erişilebilir en az bir sonsuz kaynak olsun.**
3. **Yer ateşleri zıplayarak öldürülür, su gerektirmez.** Su bitse bile ilerlenebilir.
4. **Duvar canavarları öldürülmek zorunda değil**, kaçarak geçilebilir.

Yani su bitmesi = "zorlaştı", asla "bitti" değil.

### Güzel detay (vakit kalırsa)

Su %20'nin altına düşünce Karabaş en yakın musluğa doğru havlar. Hem faydalı hem sevimli.

---

## Karabaş'la bağ kurma (oyunun asıl değeri — kesinlikle kesilmeyecek)

1. **Baştan duyulsun.** Oyun başlar başlamaz uzaktan havlama. Yaklaştıkça sesi yükselir.
   Oyuncu daha köpeği görmeden onu arar.
2. **Havlama inlemeye dönsün.** Yaklaştıkça umutsuzlaşan bir ses. Gerilim bedava gelir.
3. **KURTARMA ANI — sıkışmış olsun.** Devrilmiş yanan bir dolabın / çökmüş kirişin arkasında,
   çıkamıyor. **Oyuncu suyu sıkıp alevi söndürür, köpek serbest kalır.**
   Bulma anı pasif bir tetikleyici değil, **oynanan bir an** olmalı.
   İlişki "ben onu kurtardım" ile başlar → sonda o sizi kurtarınca **karşılıklılık** tamamlanır.
4. **İsmi olsun.** Tasmasında künye: **KARABAŞ**. Oyunun neredeyse tek yazısı bu.
5. **Sevme tuşu: `E`.** Köpeği sever, kuyruk sallar, kalp partikülü çıkar.
   **Hiçbir mekanik işlevi yok.** Oyunun en çok konuşulacak detayı bu olacak.
6. **İlk kez ateş söndürdüğünde kamera bir an ona odaklansın.**
7. **Bir kez oyuncu köpeği korusun.** Karabaş'ın yanında ateş çıkar, oyuncu söndürür,
   köpek size sokulur.

## Karabaş'ın ölümü — kurallar

- **Köpek oyuncuyu kurtarırken ölmeli.** Oyuncunun hatasıyla değil.
  Yoksa "haksızlık" hissi olur ve oyuncu oyuna kızar.
- Oyuncunun arkasında görmediği bir ateş çıkar. Karabaş her zamanki gibi atlar,
  ama bu ateş çok büyük.
- Kontrol oyuncudan alınır, **3 saniye, tüm sesler kesilir.**
- Alev yükselir, köpek görünmez olur, alev söner. Yerde sadece **tasma** kalır.
- Oyuncu tasmayı alır. Boss sahnesinde hayalet gelirken tasma parlar.
- Kanlı / grafik olmayacak.

---

## Boss = dövüş DEĞİL, sahne

Boss'u AI'lı bir dövüş olarak yapmayın (8+ saat, ve zaten öldürülemiyor).

**Yapılacak:**
1. Çıkış koridoru, dev alev kapıyı kapatır
2. Oyuncu su sıkar → **can barı hiç azalmaz**
3. Boss düzenli aralıklarla alev atar, oyuncunun canı erir
4. Oyuncunun canı 1 kalbe düşünce → **otomatik sahne tetiklenir**
5. Tasma parlar, Karabaş'ın mavi hayaleti ekranı geçer, boss söner, kapı açılır

**Boss sprite'ı:** Duvar canavarının sprite'ı 4 kat büyütülmüş + koyu kırmızı renk. Yeni asset yok.
**Hayalet sprite'ı:** Köpek sprite'ı mavi/beyaz tint + %50 saydam. Yeni asset yok.

---

# TEKNİK KURALLAR — İKİ KİŞİ DE OKUSUN

## Unity

- ## 🔒 SÜRÜM KİLİTLENDİ: **`6000.5.7f1`**
  İkiniz de **birebir bu sürümü** kurun. Farklı sürüm projeyi açtığında dosyaları
  dönüştürür ve diğerinde proje bozulur — jam'de takımları durduran klasik hata budur.

  Hub'ın listesi boş geliyorsa: **Install Editor → Archive sekmesi** → açılan
  arşiv sayfasından `6000.5.7f1` bulup **"Install with Unity Hub"** butonuna basın.

  Kurulum ekranında işaretleyin: **Web Build Support** ✅ · **Windows Build Support** ✅
  Dil paketleri / Android / iOS / Mac / Linux ❌
- Template: **2D (Built-In Render Pipeline)**. URP değil.
- Proje konumu: `D:\Kacis oyunu\Game` (repo kökü `D:\Kacis oyunu`)
- Modüller: Web Build Support ✅ / Windows Build Support ✅ / dil paketleri ❌

## Proje ayarları (Kişi A bir kere yapar)

`Edit → Project Settings → Editor`:
- **Asset Serialization → Force Text**
- **Version Control → Visible Meta Files**

`Edit → Project Settings → Player → Other Settings`:
- **Active Input Handling → Both** (eski `Input.GetAxisRaw` çalışsın diye)

## ⚠️ EN ÖNEMLİ KURAL: SAHNE SAHİPLİĞİ

Unity'de aynı sahne dosyasını iki kişi düzenleyip birleştirmeye çalışırsa **merge cehennemi**
olur ve saatlerce kaybedersiniz.

| Sahne | Sahibi | Diğeri |
|---|---|---|
| `Scenes/MainScene.unity` | **Kişi A** | ❌ AÇMASIN BİLE |
| `Scenes/TestScene_B.unity` | **Kişi B** | ❌ AÇMASIN BİLE |

**Kişi B düşmanları ve köpeği kendi test sahnesinde geliştirir, PREFAB olarak kaydeder.**
Kişi A o prefabları alıp MainScene'e yerleştirir. Kimse diğerinin sahnesine dokunmaz.

## Klasör yapısı

```
Game/Assets/
  Scripts/
    Shared/      GameContracts.cs      <- ORTAK, DEĞİŞMEZ
    Player/      Kişi A
    Systems/     Kişi A (GameManager, UI, Kamera, Ses)
    Enemies/     Kişi B
    Dog/         Kişi B
    Ending/      Kişi B
  Prefabs/
    Player/      Kişi A
    Enemies/     Kişi B
    Dog/         Kişi B
  Scenes/
    MainScene.unity      Kişi A
    TestScene_B.unity    Kişi B
  Art/           ortak (Kenney assetleri)
  Audio/         ortak
```

**Kural: kimse diğerinin klasöründeki dosyayı düzenlemez.** Bir şey lazımsa konuşun.

## Tag ve Layer'lar (saat 1'de ikisi de oluştursun)

Tag: `Player` · `Enemy` · `WaterSource` · `Dog`
Layer: `Ground` · `Player` · `Enemy`

## ORTAK SÖZLEŞME — `Assets/Scripts/Shared/GameContracts.cs`

Repoda hazır. **Kimse tek başına değiştirmesin.**

```csharp
public interface IWaterDamageable { void TakeWater(float amount); }
public interface IStompable      { void OnStomped(); }
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

public class PlayerController : MonoBehaviour
{
    public int FacingDirection { get; }   // +1 sağ, -1 sol
    public bool IsGrounded { get; }
    public bool CanControl { get; set; }  // final sahnesinde false yapılır
}
```

### Kişi B'nin yazacağı, Kişi A'nın çağıracağı imzalar

```csharp
public class DogCompanion : MonoBehaviour
{
    public void OnPetted();               // A'nın E tuşu bunu çağırır
    public void OnRescued();              // sıkıştığı yerden kurtulunca A tetikler
    public void StartFollowing();         // takibi başlatır
}

public class EndingSequence : MonoBehaviour
{
    public void TriggerDogDeath();        // A tetikler
    public void TriggerBossScene();       // A tetikler
}
```

---

## Git kuralları

- Tek branch: **`main`**. İkiniz de main'e push'layın.
  (Farklı dosyalarda çalıştığınız için çakışma olmaz. Branch/merge öğrenmek jam'de zaman kaybı.)
- **Her push'tan önce `git pull`.** İstisnasız.
- Sık commit atın — 30-45 dakikada bir. Küçük commit = kolay geri dönüş.
- `Library/` klasörü repoya GİRMEMELİ — `.gitignore` bunu hallediyor.
- Git LFS'e gerek yok, 2D sprite'lar küçük.

```bash
git pull
git add .
git commit -m "kopek takip sistemi"
git push
```

---

# ZAMAN PLANI

Saatler jam başlangıcından itibaren. Uyku bloklarını **gerçekten uyuyun**,
jam'i uykusuzluk kaybettirir.

## Kişi A — Oyuncu, Sistemler, Seviye

| Saat | İş |
|---|---|
| 0-1 | Unity kurulum, proje oluştur, git repo, `GameContracts.cs`, tag/layer |
| 1-5 | `PlayerController`: yürüme, zıplama, yön. **His ayarı burada.** ✅ hazır |
| 5-8 | `WaterGun` + `WaterTank`: su sıkma, su barı, `IWaterDamageable` çağrısı |
| 8-10 | `WaterSource`: musluk / akvaryum / küvet doldurma |
| 10-12 | `PlayerHealth`: 3 kalp, hasar, ekran sarsıntısı, kenar alev efekti |
| 12-14 | **UYKU** |
| 14-18 | `MainScene`: evin kat planı, odalar, platformlar, asset yerleştirme |
| 18-21 | UI: su barı, kalpler, `E` sevme tuşu, kamera takibi |
| 21-24 | B'nin prefablarını sahneye yerleştir, seviyeyi baştan sona oynanır yap |
| **24-26** | **İLK WEBGL BUILD + itch.io'ya yükle.** ⚠️ Ertelemeyin. |
| 26-28 | **UYKU** |
| 28-33 | Ses (havlama, ateş çıtırtısı, su, müzik), partiküller, cila |
| 33-38 | B ile birlikte denge ayarı, bug avı, tutorial |
| 38-41 | Final build, itch.io sayfası, ekran görüntüleri, açıklama, asset kredileri |
| 41-43 | Yedek süre / son bug'lar |

## Kişi B — Düşmanlar, Karabaş, Final

| Saat | İş |
|---|---|
| 0-1 | Unity kurulum, repoyu klonla, `TestScene_B` oluştur, `GameContracts.cs`'i oku |
| 1-4 | `WallMonster`: duvardan çıkma, alev fırlatma, `TakeWater` ile ölme → **prefab** |
| 4-8 | `FloorFire`: dolan bar, `OnStomped` ile ölme, dolunca `FireMonster`'a dönüşme → **prefab** |
| 8-10 | `FireMonster`: doğan canavarın oyuncuya saldırısı → **prefab** |
| 10-12 | **UYKU** |
| 12-16 | `DogCompanion`: takip (basit `MoveTowards`), ateş söndürme, `OnPetted`, havlama |
| 16-19 | Sıkışmış köpek + kurtarma anı, kamera odağı, `OnRescued` → `StartFollowing` |
| **19-24** | **`EndingSequence`: köpeğin ölümü + boss sahnesi + hayalet + final ekranı** ⚠️ |
| 24-26 | **UYKU** |
| 26-30 | Final sahnesi cilası: tasma parlaması, sessizlik anı, ekran beyaza patlaması |
| 30-33 | Düşman ses efektleri, ölüm partikülleri |
| 33-38 | A ile birlikte denge ayarı, bug avı |
| 38-41 | Test, test, test. Baştan sona 5 kez oynayın. |
| 41-43 | Yedek süre |

---

## KIRMIZI ÇİZGİLER

1. **Saat 26'da itch.io'da oynanabilir bir build OLACAK.** Yarım olsa bile.
   Unity WebGL build'i ilk seferde sık sık patlar, bunu saat 40'ta keşfetmeyin.
2. **Final sahnesi (köpeğin ölümü + boss) saat 24'te kaba haliyle bitmiş olacak.**
   Klasik jam felaketi: bütün dövüşü yapıp finali yetiştirememek.
   Final bu oyunun tek varlık sebebi.
3. **Asset seçimi saat 2'de biter.** kenney.nl'den TEK paket.
   ⚠️ Pakette **köpek sprite'ı olduğundan emin olun.**
4. **Yeni özellik eklemek yasak.** Saat 20'den sonra fikir gelirse not defterine yazın.
5. **Uyku bloklarını atlamayın.** Saat 35'te uykusuz yazılan kod, saat 38'de silinen koddur.
6. **Hayvan kararı kilitlendi: KÖPEK.** Bu tartışmaya geri dönmek yok.

## Kesilecekler listesi (zaman daralırsa bu sırayla kesin)

1. Su %20'de Karabaş'ın musluğa havlaması
2. Oyuncunun köpeği koruduğu sahne
3. `FireMonster` (yer ateşi dolunca hiç canavar doğmasın, sadece hasar versin)
4. Duvar canavarı çeşitliliği (tek tip yeter)
5. Müzik (ses efektleri kalsın, müzik gitsin)

**ASLA kesilmeyecekler:** sevme tuşu, havlama sesi, sıkışmış köpeği kurtarma anı,
Karabaş'ın ölümü, hayalet sahnesi.
