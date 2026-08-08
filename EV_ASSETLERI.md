# ALEV — Yandan Görünüm Ev İçi Asset ve Tasarım Rehberi

Bu rehber, **ALEV** oyununuzdaki yanan ev mekanlarını (Antre, Salon, Mutfak, Koridor, Çocuk Odası, Arka Koridor) inşa etmek için gereken yandan görünüm (2D Platformer side-view) ev içi asset seçeneklerini, Unity ayarlarını ve room-by-room mobilya eşleştirmelerini içerir.

---

## 🏠 1. Neden Yandan Görünüm (Platformer) Ev Asset'i Nadirdir?

Piyasadaki 2D ev içi pixel art paketlerinin **%90'ı üstten bakış (Top-Down / RPG)** perspektifindedir. 
Sizin oyununuz bir **2D Platformer** olduğu için mutlaka **yandan görünüm (Side-View / Frontal)** duvar, zemin, merdiven ve mobilyalara ihtiyacınız vardır.

`SahneRaporu.txt` incelendiğinde oyununuzdaki mobilyalar aynı zamanda **oynanış elemanıdır**:
- **Zıplama platformu** (Piyano, koltuk, masa, ranza, devrik dolap)
- **Canavar çıkış noktası** (Kitaplık, tablo, dolaplar)
- **Su dolum kaynağı** (Musluk, akvaryum, küvet, patlak boru)
- **Hikaye / Kurtarma anı** (Çocuk odasında devrilmiş mobilya arkasında sıkışan Karabaş)

---

## 🎨 2. En İyi Yandan Görünüm Ev Asset Seçenekleri

### ⭐ Seçenek A (En Çok Önerilen): `Platformer House Interior` (penzilla)
- **Link:** https://penzilla.itch.io/house-interior
- **İçerik:** 8x8 px temel tile, 16x16+ mobilyalar. **97 ev tile'ı + 44 mobilya** (kitaplık, koltuk, masa, sandık, ranza, dolaplar, tablolar).
- **Lisans:** Ücretsiz (Royalty-free), **Ticari/Jam kullanımında kredi isteniyor** (`penzilla`).
- **Uyum:** Oyununuzun kat planındaki tüm mobilya ve zemin ihtiyaçlarını birebir karşılar.

### ⭐ Seçenek B (En Hızlı & En Temiz): "Siluet Ev" + Minimal Mobilya (Limbo / INSIDE Tarzı)
- **Mantık:** Duvarlar ve zeminler için tileset **kullanmazsınız**. Unity `Square` sprite'ı ile düz koyu gri/siyah dikdörtgen zemin ve duvarlar çizersiniz.
- **Üstüne:** Sadece mobilya sprite'ları (kitaplık, piyano, masa, dolap) yerleştirilir.
- **Işıklandırma:** Ateşin turuncu Point Light ışığı ve partiküller sahneyi aydınlatır.
- **Avantajı:** 10 dakikada tüm kat planı biter, tile uydurma derdi sıfırdır, karakterler ve ateş koyu zemin üzerinde çok daha yüksek okunaklılığa sahip olur.

### 🌿 Seçenek C (CC0 Kamu Malı): `Kenney Pixel Platformer / Furniture`
- **Link:** https://kenney.nl/assets/pixel-platformer
- **İçerik:** Genel 2D platformer zeminleri, tahtalar, sandıklar ve temel platform tile'ları.
- **Lisans:** **CC0 (Kamu Malı)** — Hiçbir kredi zorunluluğu yok.

---

## 🖼️ 3. Odalara Göre Mobilya ve Asset Eşleştirme Tablosu

`SahneRaporu.txt`'deki koordinat ve obje yapısına tam uyumlu mobilya listesi:

| Oda | Boyut | Gerekli Mobilya & Obje Asset'leri | Oyundaki İşlevi / Rolü |
|---|---|---|---|
| **Antre** | x: 0..34, h: 11 | Ayakkabılık, Konsol, Kalorifer | Zıplama platformları + Kalorifer (Su dolum noktası) |
| **Salon** | x: 34..102, h: 20 | Piyano, Koltuk, Raflar/T1..T9, Akvaryum, Kitaplık, Tablo, Mavi Kilitli Kapı | Piyano & Koltuk (Zıplama) + Akvaryum (Anında %100 Su) + Kitaplık/Tablo (WallMonster doğma noktası) |
| **Mutfak** | x: 102..152, h: 11 | Tezgah, Mutfak Masası, Dolaplar, Musluk, Küvet, Yeşil Kilitli Kapı | Tezgah & Masa (Zıplama) + Musluk & Küvet (Sonsuz Su dolum) + Dolap (WallMonster) |
| **Koridor** | x: 152..188, h: 11 | Devrik Dolap, Raf, Tablolar, Alev Duvarı, Mor Kilitli Kapı | Devrik Dolap (Zıplama) + Tablolar (3x WallMonster) + Geri Dönüşü Kapatan Alev Duvarı |
| **Çocuk Odası** | x: 188..226, h: 15 | Ranza, Oyuncak Sandığı, Duvar Rafı, Devrilmiş Kiriş/Koltuk | Ranza (Yüksek zıplama) + **KARABAŞ KURTARMA NOKTASI** (Sıkışmış alevli mobilya) |
| **Arka Koridor** | x: 226..294, h: 11 | Ahşap Kasalar (Kasa 1-4), Ahşap Kirişler, Patlak Boru | Kasa & Kiriş (Zıplama) + Karabaş'ın Ölüm Sahnesi Trigger'ı |
| **Arka Kapı (Final)** | x: 294..336, h: 13 | Dev Kapı, Patlak Su Borusu, Yanan Kirişler | Dev Alev Boss Sahnesi + Patlak Boru (Son Su Doldurma) + Çıkış Kapısı |

---

## ⚙️ 4. Unity Import & Visual Consistency (Kritik Ayarlar)

Farklı paketlerden sprite toplarken görsel uyumu korumak için Inspector ayarları:

1. **Pixels Per Unit (PPU):** Hepsinde **`16`** yapın.
   - PPU 16 olduğunda: 32x32 karakter 2 birim boyunda, 16x16 tile 1 birim boyunda olur.
2. **Filter Mode:** **`Point (no filter)`** (Pixel art'ın bulanıklaşmasını engeller).
3. **Compression:** **`None`** (Piksellerin bozulmasını engeller).
4. **Sprite Sorting Layers Setup:**
   ```text
   - Background          (Order: -10) -> Evin koyu arka planı
   - HouseStructure      (Order: -5)  -> Duvarlar, tavanlar
   - Furniture_Back      (Order: -2)  -> Tablolar, duvara asılı raflar
   - Ground_Platforms    (Order: 0)   -> Üstüne basılan zeminler & mobilyalar
   - Furniture_Front     (Order: 2)   -> Ön plandaki sandık, sandalye ayakları
   - Interactive         (Order: 5)   -> Musluk, Akvaryum, Kilitli Kapılar
   - Characters          (Order: 10)  -> İtfaiyeci (Player), Karabaş, Düşmanlar
   - Fire_Effects        (Order: 15)  -> Alevler, Duman partikülleri, Su huzmesi
   - PostProcess_VFX     (Order: 20)  -> Turuncu Overlay + Vinyet Maskesi
   ```

---

## 🔥 5. 15 Dakikalık Stil Birleştirme Hilesi (Atmosphere Overlay)

Farklı sanatçıların çizimlerini tek bir bütün haline getirmek için:

1. Main Camera altına tam ekran bir **Canvas UI Image** veya **Sprite** yerleştirin.
2. Rengini **Sıcak Turuncu / Kehribar (Hex: `#FF7700` veya `#E65100`)** yapın.
3. Alpha (Saydamlık) değerini **`%15 - %20`** seviyesine getirin (Blend Mode: Multiply veya Normal).
4. Kenarlara `Vignette.png` (siyah karartma maskesi) ekleyin.
5. **Sonuç:** Oyundaki tüm mobilyalar, karakterler ve zeminler yanan evin sıcak akkor aydınlatması altında birleşir; çizim stili farkları gözden kaybolur.

---

## 📝 6. Hazır Itch.io Kredi Metni (Copy-Paste)

İndirdiğiniz ev asset'ini itch.io sayfanıza şu şekilde ekleyebilirsiniz:

```markdown
Credits & Assets:
- Firefighter character — laredgames (https://laredgames.itch.io/char-fireman)
- Dog sprites — LuizMelo, CC0 (https://luizmelo.itch.io/pet-dogs-pack)
- House interior tileset & furniture — penzilla (https://penzilla.itch.io/house-interior)
- Fire animations — piposchpatz, CC BY 4.0 (https://piposchpatz.itch.io/pixel-art-fires-fx-spritesheets)
- Sound effects — sfxr (https://sfxr.me) & Freesound.org
```
