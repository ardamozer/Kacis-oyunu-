# Asset Seti — ALEV

Hepsi **pixel art**, hepsi **ticari kullanıma açık**, hepsi indirilip `Assets/Art/` altına konacak.

> ⚠️ **Bu listeye sadık kalın.** Başka paket eklemeyin. Jam'de asset gezerek 4 saat kaybetmek
> en klasik hatadır. Eksik bir şey olursa renkli kare koyup geçin.

---

## 1. İtfaiyeci — `char-fireman`

**https://laredgames.itch.io/char-fireman**

- 32x32 px, pixel art, 38 sprite
- **14 animasyon:** Run, Walk, Crouch, **Jump**, Skid, Hold Front, Hold Up, Swim, Climb,
  Duck, Spin, **Shoot**, Slide, Kick
- Ücretsiz (name your own price)
- Lisans: kişisel + ticari kullanım serbest, değiştirmek serbest, **kredi zorunlu değil**

**Neden bu:** `Shoot` animasyonu doğrudan su tabancası için. `Jump` yer ateşi ezme için.
`Crouch` dumandan eğilme mekaniği eklerseniz hazır. Aradığımız her animasyon pakette var.

---

## 2. Karabaş — `Pet Dogs Pack`

**https://luizmelo.itch.io/pet-dogs-pack**

- 6 köpek ırkı, her birinde **11 animasyon**
- Animasyonlar: idle, walk, run, **bark**, lying down, sitting, sleeping, licking,
  itching, stretching
- Yandan görünüm pixel art
- **Lisans: CC0** — en temiz lisans, kredi bile zorunlu değil
- Ücretsiz

**Hangi ırk:** **Saint Bernard (43x29 px)** kullanın.
- En büyük siluet → yanan evin kaosunda gözden kaybolmaz (okunabilirlik kritik)
- Saint Bernard zaten gerçek hayatta bir **kurtarma köpeği** — biz onu kurtarıyoruz,
  sonra o bizi kurtarıyor. Hoş bir tersine çevirme.
- Alternatif: Great Dane (39x34)

**Animasyon eşleştirmesi:**
| Oyundaki an | Animasyon |
|---|---|
| Uzaktan duyulan havlama | `bark` |
| Sıkışmış, çıkamıyor | `lying down` |
| Kurtarıldıktan sonra sevinç | `bark` + `stretching` |
| Peşinizden gelirken | `walk` / `run` |
| `E` ile sevme | `sitting` + `licking` |
| Ölüm sahnesi | `lying down` → sprite kaybolur |
| Hayalet | `walk`, mavi tint + %50 saydam |

---

## 3. Yanan ev (yandan görünüm) — `Platformer House Interior`

**https://penzilla.itch.io/house-interior**

- **Yandan görünüm** ev içi tileset (bu nadir — çoğu ev tileset'i üstten bakış)
- 8x8 px temel tile, 16x16+ mobilya
- **97 ev tile'ı + 44 mobilya**, oda / kat / merdiven / asansör yapılabilir
- Ücretsiz (name your own price)
- Lisans: royalty-free ticari kullanım, **ticari projede kredi isteniyor**

**Neden bu:** Yandan görünüm ev içi tileset bulmak zor. Mobilyalar da duvar canavarları için
lazım — **kitaplık, tablo, dolap** var, canavarlar bunlardan çıkacak.

### Alternatif B (ücretsiz, asset gerektirmiyor): SİLUET EV ⭐

Aslında bu oyun için **daha iyi** olabilir. Yandan görünüm mobilyalı ev tileset'i
gerçekten nadir — ama sizin eviniz zaten **karanlık ve yanıyor.**

- Duvarlar ve zeminler: **düz koyu renk dikdörtgenler.** Unity'de Square sprite, siyaha yakın
  koyu kahve/gri. Tileset gerekmiyor.
- Üstüne sadece **mobilya sprite'ları** koyun (kitaplık, koltuk, dolap) — bunlar
  penzilla'dan ya da başka bir mobilya paketinden tek tek alınabilir.
- Ateşin turuncu ışığı zaten sahnenin %90'ını taşıyor. Duvarlar siluet olarak okunuyor.

**Avantajları:**
- Asset bağımlılığı neredeyse sıfır, stil uyumsuzluğu riski yok
- Kat planını çizmek 10 dakika sürer, tile döşemekle uğraşmazsınız
- "Stilize" durur, eksik durmaz — *Limbo* ve *INSIDE* bu görünümle ödül aldı
- Ateş ve karakterler koyu zemin üzerinde **çok daha okunaklı** olur

**Ne zaman bunu seçin:** penzilla'nın tile'larını döşemek 1 saatten uzun sürerse
ya da stil uyuşmazsa. Karar anı: **saat 14-16 arası, seviye kurarken.**

### Alternatif C: Kenney platformer paketleri (CC0)

**https://kenney.nl/assets/new-platformer-pack** — 440 asset, CC0, hiç lisans derdi yok.
Ev içi değil genel platform tile'ları, ama Alternatif B ile birleştirilebilir:
yapıyı Kenney tile'larıyla, atmosferi karanlık + ateşle kurarsınız.

---

## 4. Ateş — ücretsiz

### Önerilen: `Pixel art fires FX spritesheets`

**https://piposchpatz.itch.io/pixel-art-fires-fx-spritesheets**

- **4 farklı ateş varyasyonu**, spritesheet halinde, arkaplansız versiyonlar dahil
- Ücretsiz (name your own price)
- 5.0/5, üretken yapay zekâ kullanılmamış
- Lisans: **CC BY 4.0** → ticari kullanım serbest, **kredi vermek zorunlu**
  (zaten herkese kredi vereceğiz, sorun değil)

**Neden bu:** 4 varyasyon = duvar canavarı alevi, yer ateşi, boss alevi ve arkaplan
ateşi için ayrı görünüm. Tek tip ateş bütün ekranda tekrarlanınca ucuz durur.

### Yedek: `Pixel Fire`

**https://elska.itch.io/pixel-fire**

- 4 kare, tek ateş, pixel art
- Ücretsiz, ticari kullanım serbest, **kredi zorunlu değil**
- Yapay zekâ kullanılmamış

Daha basit ama en az sürtünmeli seçenek. Yukarıdaki açılmazsa bunu alın.

### ⚠️ Ücretsiz paketlerde eksik olan şey ve çözümü

Ücretsiz ateşlerde sadece **döngü** animasyonu var; "tutuşma" ve "sönme" yok.
Yer ateşi mekaniğiniz bunlara ihtiyaç duyuyor. Kodla taklit edin:

- **Tutuşma:** ateşin `localScale`'ini 0'dan 1'e büyütün (~0.3 sn)
- **Sönme:** 1'den 0'a küçültün + gri duman partikülü + "pssss" sesi

Toplam ~10 satır kod, çizilmiş animasyondan neredeyse ayırt edilemez.
Ölçek animasyonu insan gözüne çok ikna edici gelir.

---

## Eksikler (asset gerekmiyor)

| İhtiyaç | Çözüm |
|---|---|
| Su püskürtme | Unity **Particle System**, mavi kareler. Asset gereksiz. |
| UI (kalp, su barı) | Renkli dikdörtgen + Image fillAmount. Ya da Kenney "UI Pack - Pixel Adventure" (CC0) |
| Duman | Particle System, gri, yukarı hareketli, saydamlığı azalan |
| Ses efektleri | https://sfxr.me — 5 dakikada üretilir |
| Havlama sesi | https://freesound.org — **lisansı not alın** |

### Şu an projede olan sesler (yer tutucu)

`ALEV → Kisi B — Sesleri Uret` komutu `Assets/Audio/Generated/` altına üç
WAV yazıyor: `DogWhine`, `DogBark`, `FireExtinguish`.

**Bunlar kodla sentezlendi, indirilmedi — telif sorunu yok, itch.io
kredilerinde anılmaları gerekmiyor.** Ama yer tutucular: havlama ve
inilti sentezle ikna edici olmuyor, insan kulağı hayvan sesini çok iyi
tanıyor.

Gerçeklerini freesound.org'dan alıp **aynı isimlerle** üzerine yazın —
hiçbir kod değişmesi gerekmiyor. O zaman **lisansı buraya not edin**,
itch.io sayfasında kredilenmesi gerekecek.

---

## ⚠️ Unity import ayarları — atlanırsa pixel art bulanık görünür

İndirdiğiniz **her PNG** için, Inspector'da:

| Ayar | Değer |
|---|---|
| **Filter Mode** | **Point (no filter)** |
| **Compression** | **None** |
| **Pixels Per Unit** | **Hepsinde AYNI değer** (16 önerilir) |

**Pixels Per Unit'in hepsinde aynı olması şart.** Farklı olursa itfaiyeci ile ev farklı
piksel yoğunluğunda görünür ve "yamalı bohça" hissi verir — asset karıştırmanın en büyük riski budur.

PPU 16'da: itfaiyeci 2 birim boyunda, ev tile'ları 0.5 birim → itfaiyeci **4 tile boyunda**.
Odaları **6-8 tile yüksekliğinde** yapın.

## 🎨 Stil birleştirme numarası

4 farklı sanatçıdan asset kullanıyoruz. Bunları tek bir oyun gibi göstermenin ucuz yolu:

**Her şeyin üstüne global bir turuncu/sıcak renk tonu ve vinyet koyun.**

Kamera altına tam ekran yarı saydam turuncu bir sprite koymak yeterli. Yanan ev oyunu
olduğu için zaten tematik, ve stil farklılıklarını şaşırtıcı derecede iyi gizliyor.
15 dakikalık iş, en büyük görsel riski ortadan kaldırıyor.

---

## 📋 itch.io sayfasına yazılacak kredi metni

Jam sayfasında **Credits** başlığı altına bunu koyun. 2 dakika sürer,
diskalifiye olmanızı engeller.

```
Assets:
- Firefighter character — laredgames (https://laredgames.itch.io/char-fireman)
- Dog sprites — LuizMelo, CC0 (https://luizmelo.itch.io/pet-dogs-pack)
- House interior tileset — penzilla (https://penzilla.itch.io/house-interior)
- Fire animations — piposchpatz, CC BY 4.0 (https://piposchpatz.itch.io/pixel-art-fires-fx-spritesheets)
- Sound effects — sfxr (https://sfxr.me)
```

Ücretsiz kullandığınız paketlerin çoğu kredi zorunlu tutmuyor ama **penzilla ticari projede
kredi istiyor** ve hepsine kredi vermek hem doğrusu hem de jam jürilerinin baktığı bir şey.
