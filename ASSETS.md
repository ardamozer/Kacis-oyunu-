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

---

## 4. Ateş — iki seçenek

### Önerilen: `Fire Animation - Pixel Art FX Sprites` ($1)

**https://brullov.itch.io/fire-animation**

- 5 ateş boyutu: 24x32, 20x24, 15x24, 10x24, 8x8
- Her biri **başlangıç + döngü + sönme** animasyonu, toplam 355 kare
- 5 renk varyasyonu
- 4.9/5 (53 oy)
- Ticari kullanım serbest, yeniden satmak yasak

**Neden $1'a değer:** `başlangıç → döngü → sönme` yapısı sizin yer ateşi mekaniğinizle
**birebir örtüşüyor**: ateş çıkar → bar dolar → ezilince söner. Kendiniz yapsanız saatler
sürer. Ateş bu oyunun her yerinde, buraya $1 harcamak mantıklı.

### Para harcamak istemezseniz (ücretsiz)

- **https://govfx.itch.io/fire-vfx-free-pack** — 3 ateş animasyonu
- **https://elska.itch.io/pixel-fire** — 4 kare, basit

---

## Eksikler (asset gerekmiyor)

| İhtiyaç | Çözüm |
|---|---|
| Su püskürtme | Unity **Particle System**, mavi kareler. Asset gereksiz. |
| UI (kalp, su barı) | Renkli dikdörtgen + Image fillAmount. Ya da Kenney "UI Pack - Pixel Adventure" (CC0) |
| Duman | Particle System, gri, yukarı hareketli, saydamlığı azalan |
| Ses efektleri | https://sfxr.me — 5 dakikada üretilir |
| Havlama sesi | https://freesound.org — **lisansı not alın** |

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
- Fire animations — brullov (https://brullov.itch.io/fire-animation)
- Sound effects — sfxr (https://sfxr.me)
```

Ücretsiz kullandığınız paketlerin çoğu kredi zorunlu tutmuyor ama **penzilla ticari projede
kredi istiyor** ve hepsine kredi vermek hem doğrusu hem de jam jürilerinin baktığı bir şey.
