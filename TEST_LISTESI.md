# ALEV — Test Listesi

> Son turda değişen **her şeyi** tek oyun turunda doğrular.
> Yanına oyunun x konumunu yazdım; Scene görünümünde `Player`'ı seçip
> Inspector'dan `Position X`'e bakarak nerede olduğunu görebilirsin.

---

## 0. Oynamadan önce — komutları çalıştır

`MainScene` açıkken sırayla:

```
ALEV → Evi Kur
ALEV → Kisi B — Prefablari Isaretlere Yerlestir
ALEV → Kisi B — Finali Sahneye Kur
ALEV → Checkpoint'leri Odalara Kur
ALEV → Alev Perdelerini Kur
ALEV → Sahneyi Onar
Ctrl+S
```

**Console'da görmen gerekenler:**

| Komut | Beklenen satır |
|---|---|
| Evi Kur | `Ev kuruldu — 7 oda, DUZ dizilim, 336 birim` |
| Isaretlere Yerlestir | `N eski obje temizlendi` + `9 duvar canavari, 13 yer atesi, 1 Karabas` |
| Checkpoint | `7 checkpoint kuruldu` |
| Alev Perdeleri | `5 kuruldu, 2 atlandi` — atlananlar ÇocukOdası ve ArkaKapı olmalı |

⚠️ Perde sayısı 5 değilse ya da atlananlar başka odaysa **dur ve söyle** —
su kaynağı yerleşimi değişmiş demektir, kilitlenme riski var.

**Kırmızı hata varsa oynamaya başlama, önce onu çöz.**

---

## 1. Scene görünümünde (Play'e basmadan) — atmosfer

- [ ] Ekran **sıcak turuncu** bir tonda, ama **kapkara değil**
- [ ] Kenarlar hafif koyu (vinyet), **merkez net görünüyor**
- [ ] Platformların üst kenarındaki parlak şerit hâlâ seçilebiliyor

❌ Ekran tamamen siyah/çok koyuysa: `Canvas → AmbientAtmosphere → Vignette
Color` alfasını düşür. Vinyet sprite'ı kaybolmuşsa düz karartma yapar.

---

## 2. Play — baştan sona tek tur

### Antre · x 0 → 34

- [ ] **x=6, oyun başlar başlamaz (~1.5 sn): uzaktan bir HAV sesi.**
      Köpeği görmeden varlığını duymalısın.
- [ ] Kamera karakteri takip ediyor
- [ ] **x≈21** duvar canavarı: yaklaşınca çıkıyor, alevi **senin hizanda** geçiyor
- [ ] **x≈32 ALEV PERDESİ:** kalın, sarı-beyaz, zeminden tavana.
      Dekoratif turuncu alevlerden **bariz farklı** görünmeli.
- [ ] Perdenin **üstünden atlayamıyorsun**
- [ ] Su sık → ~2 saniyede çöküyor, geçiş açılıyor

### Salon · x 34 → 102

- [ ] **x 46-49 — koltuk ile platform arası: TAKILMA YOK.**
      (Basamak yardımı buranın için eklendi, eski hata buydu.)
- [ ] Tırmanış: alt platformdan yukarı çıkabiliyorsun
- [ ] **x≈68, y≈12** canavar tırmanış hizasında ateş ediyor — tırmanırken tehdit
- [ ] **x≈62, y≈18** MAVİ ANAHTAR alınıyor
- [ ] **x≈88** canavar (eskiden 92'deydi, taşındı)
- [ ] x≈100 alev perdesi
- [ ] x=102 mavi kapı açılıyor

### Mutfak · x 102 → 152

- [ ] **x≈124 YER ATEŞİ ZATEN BÜYÜK BAŞLIYOR** ← yeni
      Üstüne zıpla → **sönmemeli.** Su sık → sönmeli.
      *(Bu odanın dersi: "büyük ateş ezilmez, su gerekir")*
- [ ] x≈142 ikinci ateş küçük başlıyor → **zıplayarak sönüyor**
- [ ] x≈120 musluk dolduruyor
- [ ] **x≈133 YEŞİL ANAHTAR** — x≈126'daki canavar onu koruyor
- [ ] x≈150 perde

### Koridor · x 152 → 188 — **su kıtlığı odası**

- [ ] Bu odada **su kaynağı yok** (kasıtlı)
- [ ] Üç canavar: **x≈166, 174, 184** *(eskiden 158/170/184'tü, taşındı)*
- [ ] x≈176 MOR ANAHTAR
- [ ] **x≈164'ten itibaren Karabaş'ın İNLEMESİ kısık duyulmaya başlamalı** ←yeni
      Yaklaştıkça yükselmeli. Çok sık inlememeli (~7 saniyede bir).
- [ ] x≈186 perde — **buraya susuz gelirsen geri dönüp Mutfak'tan doldurman gerekir**

### Çocuk odası · x 188 → 226 — **kurtarma**

- [ ] İnleme artık net duyuluyor
- [ ] **x≈196 Karabaş** — köpek silueti, kare değil
- [ ] Üstündeki ateşe su sık → sönüyor → **sevinç havlaması**
- [ ] ~1.2 sn sonra peşinden gelmeye başlıyor
- [ ] **`E` bas** → kalpler çıkıyor + havlama, köpek ezilip uzuyor
- [ ] `E`'ye üst üste bas → **ses her seferinde biraz farklı perdede** ←yeni
- [ ] **x=188'de arkanda alev duvarı yükseliyor**, geri dönüş kapanıyor

### Arka koridor · x 226 → 294 — **escort + ölüm**

- [ ] Karabaş büyümüş yer ateşlerinin üstüne atlayıp söndürüyor
- [ ] **x≈248 KRİTİK TEST:** Karabaş canavarın önünde **DURUYOR ve HAVLIYOR** ←yeni
      Sen canavarı söndürene kadar ilerlemiyor.
      *(Escort bölümünün tek mekanik anı bu — çalışmıyorsa söyle)*
- [ ] Canavarı söndür → köpek devam ediyor
- [ ] **x≈264 ÖLÜM SAHNESİ** otomatik tetikleniyor:
      köpek sana atılıyor → sen yana savruluyorsun → tahta **uyarısız** düşüyor
      → çarpma sesi duyuluyor → **sonra sessizlik** → yerde tasma

⚠️ Tahta düşerken **sana hasar vermemeli.** Veriyorsa `EndingSequence →
Push Distance`'ı 2'den 3'e çıkar.

### Arka kapı · x 294 → 336 — **final**

- [ ] **x≈314 yer ateşi büyük başlıyor** ←yeni
- [ ] x≈312 canavar
- [ ] **x≈328 BOSS:** su sık → beyaza çalıyor ama **ölmüyor** (kasıtlı)
- [ ] Canın 1 kalbe düşünce: tasma parlıyor → **hayalet köpek** ekranı geçiyor
      → boss çöküp sönüyor → çıkış açılıyor
- [ ] Hayalet **köpek şeklinde**, mavi ve yarı saydam ←yeni

---

## 3. Checkpoint testi — ayrı, kısa

Bu turdan **sonra** yap, akışı bozmasın.

1. Oyunu yeniden başlat, **Mutfak'a kadar git** (x≈130)
2. **Yeşil anahtarı al**
3. Büyümüş bir ateşin üstünde bekleyip **bilerek öl**
4. Kontrol et:
   - [ ] **Mutfak'ın başında** doğdun (x≈105), Antre'de değil
   - [ ] **Yeşil anahtar hâlâ sende** ← en önemlisi
   - [ ] Söndürdüğün ateşler sönmüş kalmış
   - [ ] Zeminde doğdun, havada veya çatıda değil

❌ Baştan doğuyorsan: `Checkpointler` objesi sahnede yok demektir.
❌ Anahtar kaybolduysa: sahne yeniden yükleniyor demektir — bunu bana söyle,
   ciddi.

---

## 4. Performans (WebGL için önemli)

- [ ] Play sırasında Game penceresi akıcı, takılma yok
- [ ] `Stats` panelini aç (Game penceresi sağ üst) → FPS makul mü

*(Atmosfer katmanı her karede Canvas'ı yeniden inşa ediyordu, düzeltildi —
bu yüzden bakıyoruz.)*

---

## Bulduklarını nasıl bildir

Her madde için: **hangi x konumu** + **ne bekliyordun** + **ne oldu**.
Console'da kırmızı varsa metni birebir yapıştır.
Görsel bir tuhaflıksa ekran görüntüsü — önceki iki hatayı tek bakışta
gösterdi.
