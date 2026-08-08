# ALEV — Kat Planı (Unity koordinatlarıyla)

> **Örnek plan.** Birebir uymak zorunda değilsiniz, ama sondaki
> **"Uyulması gereken kurallar"** bölümüne uyun — orası kilitlenmeyi önlüyor.

Oyuncu ~1.6 birim boyunda, zıplayarak ~3 birim yükseliyor, ~4 birim yatay atlıyor.
Bütün ölçüler buna göre.

---

## Genel ölçüler

| | Değer |
|---|---|
| Evin genişliği | **100 birim** (x: 0 → 100) |
| Alt kat zemini / tavanı | y = **0** / y = **7** |
| Üst kat zemini / tavanı | y = **9** / y = **15** |
| Kamera ortho size | 6 (bir ekran ≈ 21 × 12 birim) |

Ev yaklaşık **5 ekran genişliğinde.** 5 dakikalık oyun için doğru büyüklük.

---

# TASARIM OMURGASI

Bu planın sıkıcı olmamasını sağlayan 7 hamle. Yerleştirme detayları aşağıdaki
tablolara işlendi — buradaki bölüm **niyeti** anlatıyor, ona göre kurun.

### 1. Karabaş seviyeyi değiştirir ⭐ en önemlisi

Kurtarma bir hikâye anı değil, bir **güç değişimi** olmalı.

| Bölüm | Oyuncunun hissi |
|---|---|
| Arama (alt kat, yalnız) | Zorlanıyorum |
| Kaçış (üst kat, Karabaş'la) | Daha çok tehlike var ama **güçlüyüm** |
| Ölümünden sonra (son ~25 birim) | Yine yalnızım — ve artık farkındayım |

Bunun için: **üst kata, tek başına baş edilemeyecek kadar yer ateşi koyun.**
Karabaş onları söndürüyor. Oyuncu köpeği mekanik olarak seviyor, ölünce
gerçekten kaybediyor. Duyguyu diyalog değil seviye taşıyor.

### 2. Karabaş en baştan görünsün

Antre'den bakınca, uzakta, yanan kapı aralığından köpeğin **silueti** görünür.
Ulaşılamaz. Havlama sesi oradan gelir. Oyuncu ilk 10 saniyede nereye gideceğini bilir.

### 3. Her odaya tek bir fikir

Odalar birbirine benzemesin. Her birinin **bir** ayırt edici özelliği var
(tablodaki "Fikri" sütunu).

### 4. Nefes odası

Çocuk odasından hemen önce **tamamen boş** bir koridor. Düşman yok, ateş uzakta,
ses azalır. Kontrast olmadan yoğunluk hissedilmez — ve bu sessizlik kurtarma
sahnesini büyütür.

### 5. Su gerçek bir karar olsun

Koridor'da (x 52–62) **3 duvar canavarı var, suyunuz 2'sine yeter.** Birini
öldürüp birinden kaçacaksınız. Tek bir yerde bunu kurun, oyuncu suya bir daha
aynı gözle bakmaz.

### 6. Yukarı nişan almanın sebebi olsun

`WaterGun`'da yukarı nişan var ama seviyede gerektiren yer yoksa boşa gider.
**Bazı canavarlar y=5'te** (tavana yakın) — oyuncu yukarı bakıp sıkmak zorunda.

### 7. Geri dönüşü olmayan an görünür olsun

Karabaş kurtulduğu an: kamera sarsılır, çökme sesi gelir, girişteki alev duvarı
**oyuncunun göreceği şekilde** yükselir. "Artık geri dönemezsin" söylenmez, gösterilir.

---

# ALT KAT — arama yolu (soldan sağa)

| Oda | x | Fikri | İçinde ne var |
|---|---|---|---|
| **Antre** | 0–14 | Sakin, öğretici | Oyuncu (4, 1) · 1 zayıf canavar (10, 1) · **Karabaş siluetini gösteren delik (13, 3)** |
| **Salon** | 14–34 | **Dikey** | Mobilyalara tırmanış · **Yüksek canavar (20, 5)** ← yukarı nişan · Akvaryum (24, 1) |
| **Mutfak** | 34–52 | **Bolluk** | **Musluk (42, 1)** · Dolap canavarı (46, 3) · Tehlike az, nefes alınır |
| **Koridor** | 52–62 | **Kıtlık** | Su yok · **3 canavar: (54,1), (57,5), (60,1)** · Dar geçit |
| **Banyo** | 62–78 | **Yoğunluk** | **Küvet (70, 1)** · Canavarlar (65,4) ve (74,1) · 2 yer ateşi (67,0) (76,0) |
| **Sessiz koridor** | 78–86 | **Nefes** | **Hiçbir şey yok.** Düşman yok, ateş yok. Sadece yürüyüş. |
| **Çocuk odası** | 86–100 | **Kurtarma** | **KARABAŞ (96, 1)** · Örten ateş (96, 1) · Düşman yok |

### Alt kat platformları

| Nesne | Position | Scale |
|---|---|---|
| Zemin | (50, -0.5) | (100, 1) |
| Tavan (x 0–88 arası) | (44, 7.5) | (88, 1) |
| Salon: sehpa | (18, 1.5) | (4, 0.5) |
| Salon: kitaplık üstü | (23, 3.5) | (4, 0.5) |
| Salon: raf | (28, 5.5) | (4, 0.5) |
| Mutfak: tezgâh | (38, 2) | (6, 0.5) |
| Koridor: devrik dolap | (56, 1.5) | (3, 0.5) |
| Banyo: küvet kenarı | (73, 1.5) | (4, 0.5) |
| Çocuk odası: ranza | (90, 2.5) | (5, 0.5) |

Salon'daki üç platform üst üste — **tek dikey oda burası.** Diğer odaları düz tutun,
her yeri dikey yaparsanız çeşitlilik kaybolur.

⚠️ **Tavan x 88–100 arasında OLMASIN** — tırmanış oradan geçiyor.

---

# TIRMANIŞ — çocuk odasından üst kata (x 88–100)

Karabaş kurtulduktan sonra buradan çıkılır.

| Platform | Position | Scale |
|---|---|---|
| 1 | (92, 3) | (3, 0.5) |
| 2 | (97, 5) | (3, 0.5) |
| 3 | (93, 7) | (3, 0.5) |
| Üst kat girişi | (96, 8.5) | (4, 0.5) |

---

# ÜST KAT — kaçış yolu (sağdan sola)

Burada **Karabaş yanınızda.** Yer ateşlerini o söndürüyor, bu yüzden tek başınıza
baş edemeyeceğiniz kadar çok var. Kasıtlı.

| Bölge | x | İçinde ne var |
|---|---|---|
| **Karabaş'lı bölüm** | 100 → 44 | **6 yer ateşi:** (88,9) (80,9) (72,9) (64,9) (56,9) (48,9) · Canavar (68, 12) · **Musluk (50, 9)** |
| **ÖLÜM ANI** | 44 | `EndingTriggerZone → DogDeath` |
| **Yalnız bölüm** | 44 → 24 | **4 yer ateşi sık aralıklı:** (40,9) (36,9) (32,9) (28,9) · Canavar (34, 12) |
| **İniş** | 24 → 14 | Platformlarla alt kata |
| **Çıkış holü** | 14 → 0 | **BOSS (6, 1)** · Çıkış kapısı (1, 1) |

**"Yalnız bölüm" özellikle sık.** Karabaş'lı bölümde ateşler 8 birim aralıklı ve
köpek hallediyor; burada 4 birim aralıklı ve tek başınızasınız. Aynı tehlike,
yarı mesafe, sıfır yardım. Kaybın büyüklüğü buradan hissediliyor.

### Üst kat platformları

| Nesne | Position | Scale |
|---|---|---|
| Üst zemin | (60, 8.5) | (84, 1) |
| Üst tavan | (60, 15.5) | (84, 1) |
| İniş 1 | (22, 7) | (3, 0.5) |
| İniş 2 | (18, 4.5) | (3, 0.5) |
| İniş 3 | (21, 2) | (3, 0.5) |

⚠️ **Musluk (50, 9) zorunlu.** Kaçış yolu susuz olursa oyuncu kilitlenir.

---

# Tetikleyiciler

| Ne | Nerede | Bileşen / Not |
|---|---|---|
| Karabaş'ın ölümü | Üst kat **(44, 10)** | `EndingTriggerZone → DogDeath` |
| Boss sahnesi | Alt kat **(16, 1)** | `EndingTriggerZone → BossScene` |
| **Giriş alev duvarı** | Antre **(1, 1)** | Karabaş kurtulunca aktif olur · kamera sarsıntısı + çökme sesi |
| Karabaş silueti | Antre **(13, 3)** | Sadece görsel. Kurtarılınca gizlenir. |

Köpeğin ölümü **oyuncunun arkasında** olmalı — sağdan sola geçerken tetikleniyor,
yani ateş geride kalıyor, oyuncu görmüyor.

---

# Kamera sınırları

Seviye bitince `CameraRig → CameraFollow`:

- **Use Bounds** ✅ · Min **(10, 2)** · Max **(90, 10)**

---

# UYULMASI GEREKEN KURALLAR

Planı değiştirseniz bile bunlara uyun:

1. **Her bölgeden erişilebilir en az bir Infinite su kaynağı.** Akvaryum ve bidon
   tek kullanımlık, sayılmaz.
   *Tek istisna: Koridor (52–62). Kasıtlı susuz — ama canavarlar kaçarak geçilebiliyor,
   ve hemen ardında küvet var. Kilitlenme yok.*
2. **Kaçış yolunda da su olacak.** En sık atlanan madde bu.
3. **Yer ateşleri her zaman zıplayarak öldürülebilir** — su bitse bile ilerlenir.
4. **Karabaş en dipte**, ama **en baştan görünsün.**
5. **Giriş kapısı kaçışta kapalı**, ve kapanışı oyuncu görsün.
6. **Kaçış ayrı bir güzergâh** — aynı yoldan geri dönmek sıkıcı.
7. **Toplam ~100 birim.** Büyütürseniz dolduramazsınız, boş hissettirir.
8. **Sadece bir oda dikey olsun** (Salon). Her yer dikey olursa çeşitlilik biter.

---

# Kurma sırası (saat 14–18)

1. Zemin, tavan, oda duvarları — düz koyu dikdörtgenler yeter
2. Platformlar → **baştan sona yürünebildiğini test edin**, düşman koymadan
3. Su kaynakları → her bölgeden erişilebilir mi kontrol edin
4. Kamera sınırları
5. Kişi B'nin prefabları: canavarlar, yer ateşleri, Karabaş
6. Tetikleyici bölgeler + alev duvarı
7. Asset'lerle görsel geçiş

**2. adımdan sonra bir kere baştan sona oynayın.** Düşmansız, sadece yürüyüp
zıplayarak. Geçilemeyen boşluk varsa şimdi bulun — 30 düşman yerleştirdikten
sonra keşfetmek çok daha pahalı.
