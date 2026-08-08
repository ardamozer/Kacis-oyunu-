# ALEV — Oynanış Analizi ve Plan

> `SahneRaporu.txt` ve kod okunarak yazıldı, tahmin yok.
> Jam saati ~23. Kalan ~20 saat, gerçekçi çalışma ~12-15 saat.

---

## Şu an elimizde ne var

| | Sayı |
|---|---|
| Harita genişliği | **336 birim** (~7 ekran) |
| Oda | 7 |
| Duvar canavarı | 9 |
| Yer ateşi | 13 |
| Su kaynağı | 6 |
| Düşen tahta | 4 |
| Anahtar / kilit | 3 |

İskelet sağlam: odalar, kilitler, anahtarlar, escort, final hepsi çalışıyor.
Sorun içerikte değil, **oyuncunun ne yapmak zorunda olduğunda.**

---

## Teşhis — 5 sorun, önem sırasıyla

### 1. 🔴 Ölünce en baştan başlıyorsun

`PlayerHealth.Die()` → `SceneManager.LoadScene(buildIndex)`.

Harita 336 birim. Boss'ta (x=328) ölen oyuncu **5-8 dakikalık ilerlemesini
kaybedip x=6'dan** başlıyor. Jam oyununda oyuncu sana 2 dakika sabır
tanıyor; bir kez ölüp baştan başlayan çoğu kişi ikinci denemede kapatıyor.

**Bu, seviye tasarımındaki hiçbir sorundan daha büyük.** En iyi parkuru
kursak bile oyuncu göremeden çıkacak.

### 2. 🔴 Su tabancası opsiyonel — yani ana mekanik hiç kullanılmıyor

- 9 duvar canavarı: PLAN.md gereği **öldürülmek zorunda değil**, kaçarak geçilir
- 13 yer ateşi: küçükken **ezilerek** söndürülür, su gerekmez
- 336 birimde **6 su kaynağı** — su asla bitmiyor

Sonuç: oyuncu hiç su sıkmadan oyunu bitirebilir. "Sınırlı su" fantezisi
hiç hissedilmiyor çünkü hiçbir şey suyu harcamaya **zorlamıyor**.

Tek zorunlu su kullanımı Karabaş'ı örten ateş. Oyunun tamamında bir tane.

### 3. 🟡 Yoğunluk düşük ve dengesiz

| Oda | Genişlik | Düşman | Not |
|---|---|---|---|
| Antre | 34 | 1 | ~5 sn yürüyüş, neredeyse boş |
| Salon | **68** | 2 | 9 platformlu tırmanış ✅ iyi |
| Mutfak | 50 | 3 | dengeli |
| Koridor | 36 | 3 | yoğun ✅ |
| ÇocukOdası | 38 | 1 | kurtarma odası, kasıtlı sakin ✅ |
| ArkaKoridor | **68** | 7 | escort |
| ArkaKapı | 42 | 5 | final |

Antre ve Salon'da oyuncu **çok yürüyüp az şey yapıyor.**

### 4. 🟡 Escort bölümünde oyuncunun işi yok

ArkaKoridor'da 6 yer ateşi var ve **Karabaş hepsini kendi söndürüyor.**
Oyuncu sadece yürüyor ve izliyor. Sonra köpek ölüyor.

Bağın derinleşmesi gereken bölüm mekanik olarak boş.

### 5. 🟡 Mekanikler hiç birleşmiyor

Duvar canavarı ve yer ateşi oyunun hiçbir yerinde **aynı anda** bir problem
oluşturmuyor. Boşluk + canavar + ateş üçlüsü hiç kurulmamış. Her engel
tek başına ve kolay.

---

## Plan — getiri/saat sırasına göre

### S — Bunlar olmadan diğerleri boşa gider

#### S1. Oda başı checkpoint (~1.5 saat) · Kişi A
Oyuncu öldüğünde **bulunduğu odanın başında** doğsun, sahne başında değil.

En basit hali: her odanın girişine bir `Checkpoint` trigger'ı. Oyuncu
değince son checkpoint konumu kaydedilir. `PlayerHealth.Die()` sahneyi
yeniden yüklemek yerine oyuncuyu oraya taşır, canı doldurur.

Odalar zaten `Room` bileşeniyle tanımlı — checkpoint'i odanın sol
kenarına otomatik koyan bir editor komutu 10 dakikalık iş.

⚠️ Sahne yeniden yüklenmediği için sıfırlanması gerekenler: sönmüş
ateşler (geri gelmeli mi? **hayır** — ilerleme korunmalı), alınan
anahtarlar (**korunmalı**), Karabaş'ın durumu (**korunmalı**).
Yani "sadece oyuncuyu taşı + canı doldur" doğru davranış.

#### S2. Alev perdesi — suyu zorunlu kıl (~1.5 saat) · Kişi B
Geçidi kapatan, **sadece suyla** sönen dikey alev. Ezilemez, kaçılamaz.

Bu tek ekleme zinciri kuruyor:
su zorunlu → su biter → musluk aramak anlamlı → canavarın çaldığı can ve
su acıtır → **bütün ekonomi canlanır.**

Yerleşim: her odada **1 tane**, geçişten hemen önce. 7 oda = 7 perde.
Toplam su maliyeti ~7 × 25 = 175 birim; depo 100, yani oyuncu yol boyunca
**en az 2 kez doldurmak zorunda.** Fantezi ilk kez gerçek oluyor.

### A — Oyunu "yürüyüş"ten "oyun"a çeviren şeyler

#### A1. Canavarları boşlukların üstüne yerleştir (~2 saat, kod yok) · Kişi A
Şu an canavarlar düz zeminde, yanlarından koşup geçiliyor. Alevi **atlaman
gereken boşluğun üstünden** geçecek şekilde konumlandırılırsa:
zamanlama problemi doğuyor → ya bekle ya söndür → **karar** çıkıyor.

Sıfır kod. Sadece konum. En yüksek getirili yerleştirme değişikliği.

#### A2. Escort'ta oyuncuya görev ver (~1 saat) · Kişi B
Karabaş, **aktif bir duvar canavarının önünde durur ve havlar.** Oyuncu
canavarı söndürene kadar ilerlemez.

PLAN.md madde 7 zaten bunu istiyor ("bir kez oyuncu köpeği korusun") ama
kesilecekler listesine düşmüştü. Escort'un tek mekanik anı bu olur ve
ölümden hemen önce "onu ben koruyordum" duygusunu kurar.

#### A3. Oda başı zorluk eğrisi (~2 saat, kod yok) · Kişi A
Her oda **tek bir şey** öğretsin, sonraki onu birleştirsin:

| Oda | Öğrettiği | Eklenecek |
|---|---|---|
| 1 Antre | yürü, zıpla, köpeği duy | Canavar alevi yolu kessin ama zararsız mesafede — dodge'ı öğret |
| 2 Salon | dikey hareket + **su ilk kez gerekli** | Tırmanışın ortasındaki canavar (68,12) alevi platformlar arasına atsın |
| 3 Mutfak | küçük ateşi ez, suyu doldur | Bir ateş **büyümüş** başlasın: "büyük = su" dersi |
| 4 Koridor | **su kıtlığı** | 3 canavar, su kaynağı yok. Alçak tavan kaçmayı engelliyor ✅ |
| 5 Çocuk odası | duygu | Değişiklik yok, kasıtlı sakin ✅ |
| 6 ArkaKoridor | köpeği koru | A2 |
| 7 ArkaKapı | her şey bir arada | Boşluk + canavar + ateş üçlüsü, sonra boss |

### B — Vakit kalırsa

- **Antre'yi kısalt** (34 → ~22 birim). Açılışta 5 saniye boş yürüyüş,
  oyuncuyu ilk 10 saniyede kaybetme riski.
- **Salon'a bir düşman daha** — 68 birimde 2 düşman az.
- Düşen tahtaları koridorlara yay (4 tane var, kod hazır, sadece yerleşim).

---

## Yapılmayacaklar — bilerek

- **Yeni mekanik yok.** Var olanları zorunlu kılmak, yenisini eklemekten
  her zaman ucuz ve güvenli.
- **Boss dövüşü yok.** PLAN.md kararı, doğru karar.
- **Skor / toplanabilir yok.** Oyunun tonu buna uymuyor.

---

## Sıra

1. ⏰ **Önce WebGL build** — kırmızı çizgi, her şeyin önünde
2. S1 + S2 (paralel: A checkpoint, B alev perdesi)
3. A1 + A2
4. A3 yerleştirme geçişi
5. Baştan sona 3 tur oyna, dengeyi ayarla
