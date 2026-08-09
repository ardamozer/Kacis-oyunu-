# ALEV — Devir Notu

> Yeni sohbete geçerken **önce bu dosyayı**, sonra `TEST_LISTESI.md`'yi okut.
> Güncelleme: jam 2. gün · ⏰ **BİTİŞ: 9 Ağustos 15:00**
>
> ⚠️ Daha önce burada "bitiş ~18:00" yazıyordu, **yanlıştı** — 3 saat fazla
> sanıyorduk. Plan yaparken 15:00'e göre hesaplayın.

---

## OYUN NE

2D platformer. İtfaiyecisin, yanan eve girip **Karabaş** adlı köpeği
kurtarıyorsun. Sonda köpek seni kurtarırken ölüyor; hayaleti çıkıştaki dev
alevi söndürüyor. Duygusal çekirdek bu karşılıklılık.

- Unity **6000.5.7f1**, 2D Built-In, Linear color space
- Repo: https://github.com/ardamozer/Kacis-oyunu-
- Tek branch `main`, iki kişi push'luyor

---

## DOĞRULANMIŞ DURUM

| | Durum |
|---|---|
| Derleme | ✅ Temiz |
| WebGL build | ✅ Çalışıyor (~8 dk, 12 MB) |
| `MainScene` | ✅ Zincirin tamamı kurulu, ~515 obje |
| Git | ✅ Her şey main'de |
| itch.io | ⚠️ Yüklendi ama **eski build** — son düzeltmeleri içermiyor |

**Seviye:** 7 oda, düz dizilim, 336 birim, kat yok.

```
[Antre] [Salon] [Mutfak] [Koridor] [Çocuk od.] [Arka koridor] [Arka kapı]
  0-34   34-102  102-152  152-188    188-226      226-294       294-336
 giriş    MAVİ    YEŞİL     MOR      KARABAŞ     ÖLÜM(264)     BOSS(328)
```

Karabaş kurtulunca x=188'de alev duvarı yükselir; geri dönüş kapanır ve
duvar oyuncuyu **kovalamaya başlar**.

---

## ✅ ÜÇ BUG + ATEŞ DUVARI RİSKİ — KODDA ÇÖZÜLDÜ, OYNANARAK DOĞRULANMADI

Dört maddenin dördü de kodda kapandı; derleme temiz (Unity'nin kendi Roslyn'iyle
`Assets/Scripts` derlendi, **0 hata**). Ama hiçbiri **oynanarak** doğrulanmadı —
sıradaki turun asıl işi bu.

### 1. Anahtar göstergesi ✅ — kök neden kanıtlandı

Teşhis sahne dosyasından okundu, tahmin değil: `MainScene`'de GameHUD'un
`inventory: {fileID: 0}` ve `keyIcons: []` idi. **Anahtar ikonları sahnede hiç
yoktu** — `AlevSceneSetup.CreateHUD`'a anahtar desteği HUD kurulduktan sonra
eklenmiş. Kodda ikonu gizleyen satır yoktu çünkü gizlenecek şey de yoktu.

`GameHUD.Awake` artık eksiği çalışma anında kapatıyor: `inventory`/`tank`/`health`
Player tag'inden bulunuyor, üç ikon `AlevSceneSetup` ile birebir aynı koordinatta
(46 + i×46, −160, 34×34) üretiliyor.

⚠️ **Sahneyi `Test Sahnesini Kur` ile baştan kurarak düzeltmeye çalışmayın** —
kamerayı ortho 6'ya çeker, odalar 5.5'e göre tasarlandı. Düzeltme bilerek
koda konuldu.

### 2. Karabaş olmayan canavar için duruyor ✅ — zaten çözülmüştü

`stopAtMonsters` da `"Canavar yolu kesti"` string'i de tüm `.cs` dosyalarında
**yok**; Antigravity oturumu (`bdde557`) temizlemiş. O log fiziksel olarak
basılamaz. DEVAM.md'de "açık" görünmesinin sebebi gözlemin değişiklikten önce
yapılmış olması. Kod değişikliği gerekmedi.

### 3. Ölüm tetikleniyor ama köpek ölmüyor ✅ — eski teşhis YANLIŞTI

> Önceki not "`EndingSequence.dog` alanı boş" diyordu. **Değil.** Sahnede
> `dog: {fileID: 1112691541}` dolu ve Karabaş prefab örneğine çözülüyor;
> `deathDebris`, `collarPickup`, `bossObject` da doğru bağlı. Oraya vakit
> harcamayın.

Kod yolu baştan sona izlendiğinde en olası sebep şu (**hipotez — oynanarak
doğrulanmadı**): ölüm sahnesi 4.35 saniye sürüyor, o sırada oyuncu ölürse
`Die()` → `ReloadScene()` çalışıyor ve checkpoint yoksa **sahnenin tamamı
yeniden yükleniyor** — sahne yarıda kesiliyor, Karabaş canlı geri geliyor.
Yani 3. bug ile aşağıdaki ateş duvarı riski **aynı problemin iki yüzü.**

İki önlem alındı:
- `PlayerHealth.TakeDamage` kontrol alınmışken hasarı hiç işlemiyor (aşağıya bak)
- `EndingSequence.Start` `dog` boşsa sahnedeki Karabaş'ı kendi buluyor

Ayrıca rutine **5 faz log'u** eklendi: `[Final] Olum 1/5` … `5/5`. Hipotez
yanlışsa bir sonraki tur zincirin hangi adımda koptuğunu tek bakışta söyler.
`4/5` yerine kırmızı `ATLANDI` görürseniz köpek referansı gerçekten boştur.

### 4. Ateş duvarı ara sahnede öldürüyordu ✅

Ölüm sahnesi 4.35 sn, oyuncunun kontrolü kapalı; duvar 1.5 b/sn ilerleyip
0.8 sn'de bir 2 hasar veriyordu, can 3. Oyunun duygusal zirvesinde,
oyuncu tuşa basamazken öldürüyordu.

Çözüm DEVAM.md'nin kendi önerisi: **tek bayrak, `PlayerController.CanControl`.**
- `FlameWallGate` — `CanControl == false` iken ne ilerliyor ne hasar veriyor
- `PlayerHealth.TakeDamage` — `CanControl == false` iken hiçbir hasarı işlemiyor

İkincisi daha geniş: sadece duvarı değil, **her hasar kaynağını** kapsıyor
(canavar, yer ateşi, boss mermisi) ve ölüm + boss + hayalet sahnelerinin
üçünü birden koruyor.

Yan etki kontrol edildi: sahnede `RoomCamera.freezePlayerDuringTransition: 0`,
yani `CanControl` şu an **yalnızca** ara sahnelerde kapanıyor. Oda geçişlerine
etkisi yok. ⚠️ O kutuyu açarsanız oyuncu oda geçişlerinde de (0.3 sn)
dokunulmaz olur — zararsız ama bilin.

### Değişen dosyalar

| Dosya | Ne yapıldı |
|---|---|
| `Systems/GameHUD.cs` | `Awake` + `SelfHeal` + `BuildKeyIcons` — eksik anahtar UI'ı üretiliyor |
| `Player/PlayerHealth.cs` | `TakeDamage`: ara sahnede hasar yok |
| `Systems/FlameWallGate.cs` | `CutsceneActive` — ara sahnede ilerleme ve hasar durur |
| `Ending/EndingSequence.cs` | `dog` otomatik bulunuyor + 5 faz log'u |

Dördü de **ana klasörde** (`Kacis-oyunu-`), `main` üzerinde, henüz commit'lenmedi.

---

## ÖNCEKİ OTURUMDA YAPILANLAR (Antigravity, 04:00–07:18)

| Dosya | Ne oldu |
|---|---|
| `Dog/DogCompanion.cs` | Köpek canavarlara dokunmuyor; yer ateşi avcısı (8 birim yarıçap, 2 zıplamada söndürür); boşta oyuncunun etrafında ±2.5 gezinir |
| `Systems/LockedDoor.cs` | **Oda temizliği şartı**: canlı `WallMonster` veya büyümüş `FloorFire` varken kapı açılmıyor (`roomScanRadius = 20`) |
| `Systems/FlameWallGate.cs` | Duvar yükseldikten sonra **sağa kovalıyor** (1.5 b/sn, `maxChaseX 300`, 2 hasar / 0.8 sn) |
| `Editor/AlevLevelBuilder.cs` | `death.requiredKeyCount = 3 → 0`, tetikleyici yüksekliği 4 → 20 |
| `Editor/AlevSceneFixer.cs` | 🆕 `ALEV → Sahneyi Guncelle` — sahneyi baştan kurmadan hızlı düzeltme |

**Neden anahtar şartı kaldırıldı:** boss hiç gelmiyordu. Ölüm tetiği 3
anahtar istiyordu; sağlanmayınca köpek ölümü olmuyor, boss da onun
önkoşulu olduğu için hiç açılmıyordu.

> ⚠️ Sıra artık kilitli kapılarla zorlanıyor. Anahtar toplama oyun için
> önemliyse başka bir mekanizma gerekir.

---

## ÖNCEKİ OTURUMLARDA YAPILANLAR (Claude, Kişi B)

- **Düşmanlar:** `WallMonster` (oyuncuya nişan alır), `FloorFire`
  (küçükken ezilir, büyüyünce sadece su), `FlameProjectile`, `FireFlicker`
- **Karabaş:** takip, zemin takibi (ışın), ışınlanma, sevme + kalpler,
  açılış havlaması, mesafeye göre inleme
- **Final:** `EndingSequence` — köpek oyuncunun yerine atılır, uyarısız
  tahta düşer, çarpma duyulur, **sonra** sessizlik, tasma kalır; boss;
  hayalet
- **Checkpoint:** oda başı. ⚠️ **Sahne yeniden YÜKLENMİYOR** — yüklense
  anahtarlar sıfırlanır ve oyuncu kilitli kapının önünde anahtarsız kalır
- **Alev perdesi:** 5 adet, sadece suyla söner, su ekonomisini zorunlu kılar.
  Yerleştirme komutu kilitlenme denetimi yapar (perdeden önce aynı segmentte
  sonsuz su yoksa perde koymaz)
- **Basamak yardımı:** `PlayerController.maxStepHeight = 0.55`
- **Sesler:** havlama + inleme freesound, diğerleri kodla sentez
- **Sprite'lar:** ev/platform/ateş hâlâ kodla üretiliyor. **Karabaş artık
  gerçek pixel art** — LuizMelo Pet Dogs Pack (CC0), Saint Bernard,
  11 sayfa / 54 kare, 8 animasyon. İtfaiyeci hâlâ dikdörtgen.

### İndirilen asset boru hattı (Karabaş'ta kuruldu, itfaiyeci için aynısı)

| Adım | Nasıl |
|---|---|
| Klasör | `Assets/Art/Imported/<konu>/` — PNG'yi oraya bırak, başka bir şey yapma |
| Import ayarı | Otomatik: `AlevImportedArtSettings` (PPU 32, Point, sıkıştırmasız, mipmapsız) |
| Dilimleme | `ALEV → Kopek Sayfalarini Dilimle` — 100×100 ızgara, pivot **(0.5, 0.35)** |
| Bağlama | `ALEV → Kopek Animasyonlarini Bagla` — 54 kareyi `DogAnimator`'a dağıtır |

⚠️ **PPU 32, kodla üretilenlerin 16'sı değil.** Klasöre bağlı olmasının sebebi:
`Art/Generated/` içindeki `Flame`, `Glow`, `Vignette` bilerek **PPU 100**;
onlara 16 dayatılsaydı alevler ve vinyet 6 kat büyürdü. Neden 32: kareler
100×100 ama görünen gövde ~29 piksel → 16'da 1.81 birim (oyuncu 1.60, köpek
adamdan uzun), 32'de 0.91 birim.

⚠️ **Pivot ayaklarda olmalı.** `DogCompanion.groundOffset`'i
`sprite.bounds.extents.y`'den hesaplıyor ve o hesap karenin dolu olduğunu
varsayıyor; 35 piksellik saydam alt boşluk yüzünden köpek 1.56 birim havada
duruyordu. Pivot ayaklara alınıp `groundOffsetOverride = 0` verildi.
Ek fayda: sevme animasyonundaki ezilme artık ayakları yerde tutuyor.

⚠️ **Kare sıralaması sayısal olmalı.** Alt-asset adları `..._0` … `_9`;
alfabetikte `_10`, `_2`'den önce gelir ve 10 kareli `Idle` tökezleyerek oynar.

---

## ALEV MENÜSÜ

| Komut | Ne yapar |
|---|---|
| `Evi Kur` | 7 odalı evi sıfırdan kurar. **`Level`'ı siler.** Kamerayı 5.5'e alır, atmosferi kurar |
| `Sahneyi Guncelle (Hizli Duzeltme)` | Baştan kurmadan düzeltir — havadaki canavarları indirir, tetik boyutları, anahtar şartı |
| `Kisi B — Prefablari Kur` | Düşman/Karabaş prefabları |
| `Kisi B — Prefablari Isaretlere Yerlestir` | Prefabları işaretlere oturtur (önce eskileri temizler) |
| `Kisi B — Finali Sahneye Kur` | Boss, hayalet, tasma, ölüm tahtası. **Karabaş sahnedeyken çalıştır** |
| `Checkpoint'leri Odalara Kur` | 7 odanın girişine checkpoint |
| `Alev Perdelerini Kur` | 5 perde + kilitlenme denetimi |
| `Sahneyi Onar` | Bozuk referanslar, fazla ölüm tahtası |
| `Seviyeyi Denetle` | Ulaşılamaz platform / tıkalı kapı / susuz bölge |
| `Sahne Raporu Cikar` | `SahneRaporu.txt` — koordinat dökümü, AI'a okutmak için |
| `Kisi B — Sahneyi Tazele (F5)` | Prefabları tazeler, konumları korur |
| `Atmosfer Katmanini Kur` | Sıcak turuncu katman + vinyet. Mevcut objeleri yeniden kullanır, silmez |
| `Kopek Sayfalarini Dilimle` | `Art/Imported/Dog/` altını 100×100 diler, pivot ayaklara |
| `Kopek Animasyonlarini Bagla` | 54 kareyi `Karabas.prefab`'daki `DogAnimator`'a dağıtır |

**Tam kurulum sırası:**
`Evi Kur` → `Prefablari Isaretlere Yerlestir` → `Finali Sahneye Kur` →
`Checkpoint'leri Odalara Kur` → `Alev Perdelerini Kur` → `Sahneyi Onar` → `Ctrl+S`

⚠️ **`Test Sahnesini Kur`'u en sona çalıştırmayın** — kamerayı ortho 6'ya
çeker, oysa odalar 5.5'e göre tasarlandı.

---

## DEĞİŞTİRİLMEMESİ GEREKEN KARARLAR

Acı çekilerek bulundu:

1. **Zıplama:** `jumpForce 15` + `gravityScale 4` → max yükselme **2.87**.
   Boşluk: yükselme 2.0 → 3.5 · 1.0 → 4.5 · düz → 5.0.
   Her tırmanışın ilk basamağı yerden **≤ 2.2**.
2. **Renk hiyerarşisi:** sadece platform, ateş, anahtar parlak. Dekoru
   parlatınca ekran okunmaz oluyor.
3. **Anahtar asla geride olmaz** — kilidin bir önceki odasında ve görünür.
4. **Kilitlenme:** her bölgede erişilebilir sonsuz su; Koridor kasıtlı
   susuz ama orada yer ateşi YOK.
5. **Ölüm sahnesi:** tahta **uyarısız** düşer (`DropNow(false)`). Oyun boyunca
   "ünlem gördüysen kaç" öğretiliyor; ölümde ünlem çıkarsa oyuncu kendini
   suçlar ve duygu öfkeye döner. Köpek oyuncunun **yerine geçmeli**.
6. **Kamera:** oda bazlı (`RoomCamera`), ortho **5.5**. `floorBias = 1.5`
   olmadan oyuncu ekranın en altında kalıyor (oda 11 = görüş 11 → kamera
   ortaya kilitleniyor).
7. **Checkpoint sahneyi yeniden yüklemez.** Yüklerse anahtarlar sıfırlanır.
8. **Ara sahnede oyuncu hasar almaz** (`CanControl == false` iken
   `PlayerHealth.TakeDamage` hiçbir şey yapmaz). Bu "kolaylaştırma" değil:
   kontrol alınmışken oyuncu kaçamıyor, öldüğünde `ReloadScene` sahneyi
   yeniden yükleyip ara sahneyi yarıda kesiyor ve Karabaş canlı geri geliyor.
   Kaldırırsanız ölüm sahnesi rastgele kırılmaya başlar.

9. **Salon kulesi: T4–T7 hepsi 5 genişlikte, x = 54/62 zigzagı.**
   Boşlukları 3.0'a getiriyor. Eskiden T5 dardı (3.5) ve T6 sola kaymıştı
   (x=53); boşluklar 4.25 / 4.75 / 4.5 çıkıyor, kule T4'ten sonra
   **aşılamıyordu** ve kule ana yolda olduğu için oyun orada kilitleniyordu.
   `Salon_T1` de kaldırıldı: üstü 2.15'ti, `Salon_Koltuk` ise 2.20 — koltuk
   daha yüksek olduğu için T1'e hiç basılamıyordu. Kulenin ilk basamağı
   artık koltuğun kendisi.

10. **Koridor'un alçak tavanı kaldırıldı, `Basamak1` silindi.**
    Tavan oynanabilir yüksekliği 6.5'e indiriyordu; `Koridor_Raf`ın üstü
    4.45, oyuncu 1.6 → rafta kafayla tavan arası 0.45 kalıyor ve **orada
    zıplanamıyordu**. `DevrikDolap` (üst 1.80) ile `Basamak1` (üst 1.75)
    yan yanaydı ve ikincisi daha alçaktı: oyuncu tırmandığını sanıp
    aşağı iniyordu. Dolap 166 → 168.55'e kaydırıldı.
    Atmosfer, oynanabilirlikten önce gelemez.

11. **Çocuk odasında sonsuz su var (`Cocuk_Radyator`, x=191).**
    Onsuz Mutfak küvetinden (146) Arka koridor musluğuna (240) kadar
    94 birim kurak kalıyor — hem haritanın en uzun kurak şeridi, hem
    Karabaş'ın kurtarıldığı yer, hem de x=188'de geri dönüş kapanıyor.
    Bu su kaynağı ayrıca Çocuk odası alev perdesinin yerleşmesini sağlıyor
    (perde komutu, önünde su yoksa perde koymuyor).

12. **Atmosfer katmanı: turuncu %10, vinyet %40.** Eskiden %25 / %60'tı.
    O değerler düz renkli dikdörtgenler varken doğruydu — 4 farklı asset
    paketini birleştirmek için konmuştu. Gerçek pixel art gelince aynı tint
    sprite'ın **kendi paletini eziyor**. Vinyet düşüşünün ayrı faydası:
    %60'ta ekran kenarları çok kararıp kenardan gelen canavarlar geç fark
    ediliyordu. **%20'nin üstüne çıkmayın.**

### `Seviyeyi Denetle` artık güvenilir — çıktısını ciddiye alın

Araç bir dönem 23 uyarının 18'ini uyduruyordu ve gerçek kopukluğu gizliyordu.
Üç hata düzeltildi:
- **Edit Mode körlüğü:** `Start()` çalışmadığı için alev duvarı sahnedeki
  yerinde, collider'ı açık duruyor ve sahte "TIKALI KAPI" veriyordu.
  Artık `FlameWallGate` taşıyan collider'lar atlanıyor.
- **Tek sabit iki iş yapıyordu:** `SafeRise` hem "zeminden ilk basamak" hem
  "platformdan platforma" için kullanılıyordu. Ayrıldı:
  `GroundStep 2.25` / `SafeRise 2.4` / `TightRise 2.0`.
- **Süzgeç yanlış yerdeydi:** yükseklik eşiği zincirin kendisine
  uygulanıyordu, alçak basamaklar görünmüyordu. Artık sadece raporlamaya.
  Duvarlar (uzun + ince) platform taramasından çıkarıldı.

Yeni `🔶 SIKI ZIPLAMA` satırı **bilgi**, hata değil: 2.0'ın üstünde yükselme
gerektiren girişleri bildirir (fiziksel tavan 2.87).

---

## SIRADAKİ İŞLER (öncelik sırası)

**Kritik eşik: 3. madde bitince jam kaybedilemez hale gelir** — çalışan oyun,
eksiksiz ve yasal bir sayfa. Sonrası kâr. 4'e, 3 bitmeden başlamayın.

1. **Tam tur — hiçbiri oynanarak doğrulanmadı.** Tek turda:
   - Mavi anahtarı al → HUD'da üç kare; alınan **parlıyor**, diğerleri sönük
   - x≈264 → Console'da `[Final] Olum 1/5` … `5/5` akıyor, köpek gizleniyor,
     yerde tasma kalıyor
   - Ölüm sahnesi boyunca ateş duvarı **duruyor** ve can eksilmiyor
   - Karabaş: yürürken yürüyor, ateşe atlarken havlıyor, kurtulunca oturuyor
   - **Kaçış bölümü** — duvar kovalarken checkpoint'e ölmeden varılıyor mu
2. Turda çıkanları düzelt
3. **Build + yükle + krediler + 3 ekran görüntüsü**
4. İtfaiyeci sprite'ı — sadece ≥2.5 saat kaldıysa
5. **Son 1 saat: kod değiştirme.** Son build, yükle, teslim et.

### Build ve zip

`Builds/WebGL/` diskte **var**, `.gitignore`'lu. Build ~8 dk.

⚠️ Zip'i `Compress-Archive` ile **yapmayın** — PowerShell 5.1 girdi adlarını
ters slash'la yazıyor, itch.io dosyaları bulamıyor. Bir kez yaşandı.
Doğrusu: girdi adlarını elle `/` ile normalleştiren bir betik (bu jam'de
yazıldı) ya da sağ tık → "Sıkıştırılmış klasöre gönder".

Yüklemeden sonra **"This file will be played in the browser"** kutusunu
işaretlemeyi unutmayın — her yeni dosyada sıfırlanıyor. Viewport 960×600.

⚠️ **Build almadan önce çıktının tarihini kontrol edin.** Bir kez eski build
klasöründen zip yapılıp yüklendi; sayfada 3 saat eski sürüm durdu.

### itch.io sayfası

Bütün metinler **`ITCH_SAYFASI.md`**'de hazır: başlık, tagline, açıklama,
kontroller, tag'ler, krediler.

⚠️ **Havlama sesi CC BY 4.0 — atıf ZORUNLU** (Paulo_Gonçalves).
⛔ `ASSETS.md` ve `EV_ASSETLERI.md` sonundaki kredi bloklarını **kullanmayın** —
indirilmemiş asset paketlerini listeliyorlar ve zorunlu atıf onlarda yok.
Karabaş için LuizMelo satırı da eklendi (CC0, zorunlu değil ama doğrusu).

### Product Name hâlâ "game"

Yükleme ekranında ve tarayıcı sekmesinde "ALEV" değil "game" yazıyor.
Player Settings → Product Name. Bir sonraki build'den **önce** yapın,
tek başına build almaya değmez.

---

## İŞ BÖLÜMÜ

- **Kişi A:** oyuncu, su, HUD, seviye, kamera, anahtarlar, final ekranı
- **Kişi B:** düşmanlar, Karabaş, `EndingSequence`, sesler
- **Sahne:** `MainScene` → A · `TestScene_B` → B
- ⚠️ `.meta` dosyalarını ikisi birden push'lamasın — GUID çakışması
  referansları kırar. Bu jam'de **üç kez** yaşandı.

---

## YENİ SOHBETE VERİLECEK AÇILIŞ

> Game jam'deyiz, 2 kişilik takım, Unity 6000.5.7f1, 2D platformer.
> Repo: https://github.com/ardamozer/Kacis-oyunu-
> `DEVAM.md` ve `TEST_LISTESI.md`'yi oku, durumu oradan al.
> Üç bug ve ateş duvarı riski kodda çözüldü ama **oynanarak doğrulanmadı** —
> sıradaki iş bu, sonra yeni build.
> Kod değiştirmeden önce sahnedeki gerçek değerleri doğrula: script
> varsayılanını değiştirmek zaten serileştirilmiş prefab örneklerini
> değiştirmiyor, bu jam'de defalarca yanılttı. Aynı sebeple **teşhisi
> `MainScene.unity`'den okuyun** — dosya text-serialized, alanların gerçekten
> bağlı olup olmadığı orada görünüyor. Bu turda iki teşhis böyle çürütüldü.
