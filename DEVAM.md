# ALEV — Devir Notu

> Yeni bir sohbete geçerken bu dosyayı okut. Doğrulanmış durum + yapılacaklar.
> Tarih: jam 2. günü, ~23:20

---

## DOĞRULANMIŞ DURUM (tahmin değil, ölçüldü)

| | Durum |
|---|---|
| **Derleme** | ✅ Temiz. Son derlemede 0 hata. |
| **WebGL build** | ✅ **Başarılı** — 22:47→22:55, 8 dk 16 sn |
| **Build çıktısı** | ✅ `Builds/WebGL/` — 12 MB, index.html yerinde |
| **Yüklemeye hazır zip** | ✅ `Builds/alev-webgl.zip` — 11 MB, **düz slash'lı** |
| **MainScene** | ✅ 515 obje, zincirin tamamı kurulu |
| **Git** | ✅ Her şey main'de |

**Yani jam'in en büyük teknik riski kapandı: build hattı çalışıyor.**

### Bilinen kurulum

- Unity **6000.5.7f1**, 2D Built-In, Linear color space
- WebGL: Gzip + **Decompression Fallback açık** (itch.io için doğru)
- Build listesi: `Assets/Scenes/MainScene.unity` (tek sahne)
- Log yeri: `game/Logs/Editor.log` ← eski `%LOCALAPPDATA%` yolunu okumayın, bayat

---

## HEMEN YAPILACAKLAR (öncelik sırası)

### 1. Zip'i itch.io'ya yükle ⏰ ŞİMDİ
`Builds/alev-webgl.zip` hazır bekliyor.

1. itch.io → projeyi düzenle → **Uploads**
2. Eski zip'i **Delete**
3. Yeni zip'i yükle → **"This file will be played in the browser"** ✅ *(yeni dosyada sıfırlanır)*
4. Viewport **960 × 600**, Visibility **Draft**
5. Save → sayfayı `Ctrl+Shift+R` ile aç → **gerçekten oyna**

**Siyah ekran gelirse:** ilk deneyecek şey `Project Settings → Player → Other Settings → Color Space` → **Gamma**. Sonra yeniden build.

⚠️ Zip'i PowerShell'in `Compress-Archive`'ıyla yapmayın — ters slash yazıyor, itch.io dosyaları bulamıyor (bu hata bir kere yaşandı). Sağ tık → "Sıkıştırılmış klasöre gönder" çalışır.

### 2. Baştan sona tam tur
Antre → Salon → Mutfak → Koridor → Çocuk odası → Karabaş → Arka koridor → Arka kapı → final ekranı.

Test tuşları: `1` köpeğin ölümü · `2` boss sahnesi · `E` sev · `R` kurtar · `T` takip

**Bakılacak tek şey: zincir kopuyor mu.** Grafik değil.

### 3. Denge ayarı
Oynanabilirlik doğrulandıktan sonra: can, su tüketimi, düşman canları, ateş yayılma hızı.

### 4. itch.io sayfası
⚠️ **Havlama sesi CC BY 4.0 — atıf ZORUNLU.** Hukuki gereklilik, nezaket değil.
Kredi metni `ASSETS.md` sonunda hazır, kopyala-yapıştır.

---

## ALEV MENÜSÜ — komut referansı

| Komut | Ne yapar |
|---|---|
| `Test Sahnesini Kur` | Player + HUD + CameraRig. **Önce bu.** |
| `Evi Kur` | 7 odalı evi sıfırdan kurar. **`Level`'ı siler** — elle yaptıklarınız gider |
| `Kisi B — Prefablari Kur` | Düşman/Karabaş prefabları (bir kez yeter) |
| `Kisi B — Prefablari Isaretlere Yerlestir` | Prefabları işaretlere oturtur |
| `Kisi B — Finali Sahneye Kur` | Boss, hayalet, tasma, ölüm tahtası |
| `Sahneyi Onar` | Bozuk referansları düzeltir, fazla ölüm tahtasını siler |
| `Seviyeyi Denetle` | Ulaşılamaz platform / tıkalı kapı / susuz bölge tarar |
| `Sahne Raporu Cikar` | `SahneRaporu.txt` — Claude'a okutulacak koordinat dökümü |
| `Yapi Taslarini Uret` | Elle tasarım için prefab paleti |

**Sıfırdan kurulum sırası:** Test Sahnesini Kur → Evi Kur → Prefablari Kur → Isaretlere Yerlestir → Finali Sahneye Kur → Sahneyi Onar → `Ctrl+S`

---

## OYUN YAPISI

```
[Antre] [Salon] [Mutfak] [Koridor] [Çocuk od.] [Arka koridor] [Arka kapı]
  0-34   34-102  102-152  152-188    188-226      226-294       294-336
 giriş   h20     su bol   alçak 6.5   KARABAŞ      ÖLÜM(264)    BOSS+çıkış
         MAVI    YEŞİL      MOR
```

Düz dizilim, kat yok, hep ileri. Karabaş kurtulunca x=188'de alev duvarı yükselir,
geri dönüş kapanır.

---

## DEĞİŞTİRİLMEMESİ GEREKEN KARARLAR

Bunlar acı çekilerek bulundu, geri almayın:

1. **Zıplama matematiği:** `jumpForce 15` + `gravityScale 4` → max yükselme **2.87**.
   Platform aralıkları: yükselme 2.0 → yatay boşluk max 3.5 · 1.0 → 4.5 · düz → 5.0.
   **Her tırmanışın ilk basamağı yerden ≤ 2.2 olmalı**, yoksa düşmek tuzak olur.

2. **Renk hiyerarşisi:** sadece 3 şey parlak olmalı — platformlar, ateş, anahtarlar.
   Dekoru parlatınca ekran okunmaz oluyor. Platformların üst kenarındaki parlak
   şerit **işlevsel**, dekoratif değil.

3. **Anahtarlar asla geride olmaz.** Her anahtar kilidinin bir önceki odasında ve
   ana yoldan görünür. "Geri dön ara" yanan ev oyununda aciliyeti öldürür.

4. **Kilitlenme kuralları:** her bölgede erişilebilir sonsuz su kaynağı; her yer
   ateşinin yanından geçilebilmeli; Koridor kasıtlı susuz ama orada yer ateşi YOK.

5. **Ölüm sahnesi:** tahta uyarısız düşer (`DropNow(false)`). Oyun boyunca "ünlem
   gördüysen kaç" öğretiliyor; ölümde ünlem çıkarsa oyuncu kendini suçlar ve duygu
   öfkeye döner. Köpek oyuncunun **önüne geçmeli** — yanında ölürse kaza, yerine
   geçerse fedakârlık okunur.

6. **Sıradışılık koruması:** `EndingTriggerZone` önkoşulları — ölüm 3 anahtar
   ister, boss ölümün gerçekleşmesini bekler. Kaldırmayın.

---

## AÇIK RİSKLER

| Risk | Not |
|---|---|
| Color Space **Linear** | WebGL2'de çalışır ama siyah ekranın 1 numaralı sebebi. Yedek: Gamma |
| Sesler yer tutucu | Havlama gerçek (CC BY), diğerleri sentez |
| Elle oda tasarımı yapılmadı | Blockout hâlâ script üretimi. Vakit kalırsa `Yapi Taslarini Uret` + elle döşeme |
| Asset'ler indirilmedi | Hâlâ renkli dikdörtgen. `ASSETS.md`'de liste hazır |

---

## İŞ BÖLÜMÜ

- **Kişi A:** oyuncu, su sistemi, HUD, seviye, kamera, anahtarlar, final ekranı
- **Kişi B:** düşmanlar, Karabaş, EndingSequence, sesler
- **Sahne sahipliği:** `MainScene` → A · `TestScene_B` → B. Kimse diğerininkini açmaz.
- **Git:** tek branch `main`, push'tan önce `git pull`.
  ⚠️ `.meta` dosyalarını ikisi birden push'lamasın — GUID çakışması referansları kırar.

---

## YENİ SOHBETE VERİLECEK ÖZET

> Game jam'deyiz, 2 kişilik takım, Unity 6000.5.7f1, 2D platformer.
> Oyun: itfaiyeci yanan evde köpeği (Karabaş) kurtarıyor, köpek sonda onu
> kurtarırken ölüyor, hayaleti boss'u yok ediyor.
> Repo: https://github.com/ardamozer/Kacis-oyunu-
> Durum ve yapılacaklar `DEVAM.md`'de. Build çalışıyor, zip hazır.
> Sıradaki iş: itch.io'ya yükle, baştan sona tur at, dengeyi ayarla.
