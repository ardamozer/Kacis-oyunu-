# Kişi B — Claude'a verilecek başlangıç promptu

> Aşağıdaki metnin tamamını kopyalayıp kendi Claude'una yapıştır.
> Öncesinde repoyu klonlamış ve Claude'u proje klasöründe açmış ol.

---

Selam. Bir game jam'deyiz, ilk jam'imiz, **43 saatimiz var** ve şu an yaklaşık ___ saat geçti.
2 kişilik takımız, ikimiz de Unity'de yeniyiz. Ben **Kişi B**'yim. Takım arkadaşım Kişi A.

Proje klasöründeki `PLAN.md` dosyasını oku — oyunun tasarımı, teknik kurallar, sözleşmeler
ve zaman planı orada. Benim sorumluluğum **Kişi B** satırları.

## Oyun

Yandan görünüm 2D platformer, Unity 6000.0.x LTS, 2D Built-In template.
Bir itfaiyeciyiz, yanan evde **Karabaş** adlı köpeği kurtarıyoruz. Su tabancamız var,
suyu sınırlı ve evdeki musluk/akvaryum/küvetten dolduruyoruz.

Köpeği sıkışmış halde buluruz, alevi söndürüp kurtarırız, birlikte çıkışa yürürüz.
Yolda Karabaş yerdeki ateşlerin üstüne atlayıp söndürerek bize yardım eder.
Sonunda **bizi kurtarırken ölür.** Çıkışta öldürülemeyen dev bir alev bizi durdurur ve
tam ölmek üzereyken **Karabaş'ın hayaleti** gelip bizi kurtarır.

**Oyunun bütün değeri o final sahnesinde. Geri kalan her şey ona hizmet ediyor.**

## Benim sorumluluğum

- `Assets/Scripts/Enemies/` — duvar canavarı, yer ateşi, doğan canavar
- `Assets/Scripts/Dog/` — Karabaş (takip, ateş söndürme, sevilme, sıkışma/kurtarılma)
- `Assets/Scripts/Ending/` — Karabaş'ın ölümü, boss sahnesi, hayalet, final
- `Assets/Prefabs/Enemies/`, `Assets/Prefabs/Dog/`
- `Assets/Scenes/TestScene_B.unity` — sadece benim test sahnem

## Uymam gereken katı kurallar

1. **`Scenes/MainScene.unity` dosyasına ASLA dokunmam.** Kişi A'nın. Açmam bile.
   Her şeyi kendi `TestScene_B.unity` sahnemde geliştirip **prefab** olarak kaydediyorum.
   Kişi A prefabları alıp ana sahneye yerleştiriyor.
2. **`Assets/Scripts/Player/` ve `Assets/Scripts/Systems/` klasörlerine dokunmam.** Kişi A'nın.
3. **`Assets/Scripts/Shared/GameContracts.cs` dosyasını tek başıma değiştirmem.**
   Değişmesi gerekiyorsa önce Kişi A ile konuşurum.
4. Kişi A'nın yazdığı/yazacağı şu imzalara göre kod yazarım (bazıları henüz olmayabilir,
   o zaman geçici stub yazıp kendi sahnemde test ederim):
   ```csharp
   PlayerHealth.TakeDamage(int amount)
   PlayerHealth.CurrentHealth
   WaterTank.TrySpend(float amount) / Refill(float amount) / .Current / .Max
   PlayerController.FacingDirection / .IsGrounded / .CanControl
   ```
5. Benim yazıp Kişi A'nın çağıracağı imzalar — **bunları değiştirirsem A'nın kodu kırılır**:
   ```csharp
   DogCompanion.OnPetted()
   DogCompanion.OnRescued()
   DogCompanion.StartFollowing()
   EndingSequence.TriggerDogDeath()
   EndingSequence.TriggerBossScene()
   ```
6. Düşmanlarım `IWaterDamageable` (suyla ölenler) ve `IStompable` (zıplayarak ezilenler)
   arayüzlerini uygular. İkisi de `GameContracts.cs`'te hazır.
7. **Git: tek branch `main`. Her push'tan önce `git pull`.** 30-45 dakikada bir commit.

## Yapım sıram (PLAN.md'deki Kişi B tablosu)

| Saat | İş |
|---|---|
| 1-4 | `WallMonster` — duvardan çıkar, alev fırlatır, suyla ölür → prefab |
| 4-8 | `FloorFire` — üstünde dolan bar, zıplanınca söner, dolarsa canavara dönüşür → prefab |
| 8-10 | `FireMonster` — doğan canavar, oyuncuya saldırır → prefab |
| 10-12 | UYKU |
| 12-16 | `DogCompanion` — takip, ateş söndürme, `OnPetted`, havlama |
| 16-19 | Sıkışmış köpek + kurtarma anı, kamera odağı, `OnRescued` → `StartFollowing` |
| **19-24** | **`EndingSequence` — köpeğin ölümü + boss + hayalet + final** ⚠️ en kritik |
| 24-26 | UYKU |
| 26-30 | Final sahnesi cilası |
| 30-33 | Ses efektleri, partiküller |
| 33-43 | Kişi A ile denge, bug avı, test |

## Tasarım kararları (bunları sorgulamadan uygula, ekipçe kararlaştırıldı)

- **Köpek takibi basit olacak.** Oyuncunun 2 birim gerisindeki noktaya `MoveTowards`.
  Zıplamasın, oyuncuya çarpmasın (collider trigger), asla kaybolmasın.
  Escort AI jam'de en çok bug üreten sistemdir, "gerçekçi" yapmaya çalışmayacağım.
- **Köpek senaryolu ölüm dışında ölümsüz.** Oyuncunun hatasıyla ölemez.
- **Ölüm sahnesi:** oyuncunun arkasında ateş çıkar, Karabaş atlar, ateş çok büyüktür.
  Kontrol alınır, 3 saniye, **tüm sesler kesilir**, alev söner, yerde tasma kalır. Kansız.
- **Boss dövüş değil, sahne.** Canı hiç azalmaz. Oyuncunun canı 1 kalbe düşünce
  hayalet sahnesi otomatik tetiklenir. AI, saldırı deseni, denge ayarı YOK.
- **Yeni asset aramayacağım:** boss = duvar canavarı sprite'ı ×4 büyük + koyu kırmızı.
  Hayalet = köpek sprite'ı mavi tint + %50 saydam.

## Senden istediklerim

- **Unity'de yeniyim.** Kod verirken dosyayı nereye koyacağımı, Inspector'da hangi alanı
  neye bağlayacağımı, prefabı nasıl oluşturacağımı adım adım söyle. "Rigidbody2D ekle" deme,
  "objeyi seç → Add Component → Rigidbody2D → Body Type'ı Kinematic yap" de.
- Kodu **çalışır ve basit** tut. Jam'deyiz, mimari güzelliği önemli değil, bitmesi önemli.
  Abstract factory, event bus, ScriptableObject mimarisi istemiyorum.
- Her parçayı **kendi test sahnemde tek başına test edebileceğim** şekilde yaz.
- Bir şey 30 dakikadan uzun sürüyorsa **daha basit bir yol öner.**
- Zaman planını hatırlat. Saat 19'a gelince final sahnesine geçmem gerektiğini söyle,
  ben düşmanları cilalamaya devam etmek istesem bile.
- **Kapsam bekçim ol.** Yeni özellik fikri verirsem "bu plana yok, jam sonrası" de.

## Şimdi ne yapacağız

`PLAN.md`'i oku, sonra `WallMonster`'dan başlayalım. Önce `TestScene_B` sahnesini kurup
düşmanı test edebileceğim minimum ortamı hazırlayalım (zemin + geçici bir oyuncu kutusu).
