# ALEV — Örnek Kat Planı (Unity koordinatlarıyla)

> Bu bir **örnek**. Birebir uymak zorunda değilsiniz — ölçek ve yapı fikri versin.
> Kendi planınızı çizerken aşağıdaki **kurallara** uyun, gerisi serbest.

Oyuncu ~1.6 birim boyunda, zıplayarak ~3 birim yükseliyor, ~4 birim yatay atlıyor.
Bütün ölçüler buna göre.

---

## Genel ölçüler

| | Değer |
|---|---|
| Evin genişliği | **100 birim** (x: 0 → 100) |
| Alt kat zemini | y = **0** |
| Alt kat tavanı | y = **7** |
| Üst kat zemini | y = **9** |
| Üst kat tavanı | y = **15** |
| Kamera ortho size | 6 (bir ekran ≈ 21 × 12 birim) |

Yani ev yaklaşık **5 ekran genişliğinde.** 5 dakikalık bir oyun için doğru büyüklük.
Daha büyük yapmayın — dolduramazsınız.

---

## ALT KAT — arama yolu (soldan sağa)

| Oda | x aralığı | İçinde ne var |
|---|---|---|
| **Antre** | 0 – 14 | Oyuncu başlangıcı (x=4, y=1). Giriş kapısı x=1. |
| **Salon** | 14 – 34 | Akvaryum (OneShot su) x=24 · Kitaplık canavarı x=20, y=3 |
| **Mutfak** | 34 – 52 | **Musluk (Infinite)** x=42 · Dolap canavarı x=46, y=3 |
| **Koridor** | 52 – 62 | Dar geçit. 2 yer ateşi x=54 ve x=59 |
| **Banyo** | 62 – 78 | **Küvet (Infinite)** x=70 · Ayna canavarı x=66, y=3 |
| **Çocuk odası** | 78 – 100 | **KARABAŞ x=94, y=1** · Örten ateş x=94 · Yer ateşi x=84 |

### Alt kat platformları

Düz zemin sıkıcı olur, zıplama mekaniğini kullandırın:

| Nesne | Position | Scale |
|---|---|---|
| Zemin | (50, -0.5) | (100, 1) |
| Tavan | (50, 7.5) | (100, 1) |
| Salon sehpası | (28, 1.5) | (5, 0.5) |
| Mutfak tezgâhı | (38, 2, 0) | (6, 0.5) |
| Koridor devrik dolap | (56, 1.5) | (3, 0.5) |
| Banyo küvet kenarı | (73, 1.5) | (4, 0.5) |
| Çocuk odası ranza | (88, 2.5) | (5, 0.5) |

---

## TIRMANIŞ — çocuk odasından üst kata (x 90 – 100)

Karabaş kurtulduktan sonra buradan yukarı çıkılır.

| Platform | Position | Scale |
|---|---|---|
| 1 | (92, 3) | (3, 0.5) |
| 2 | (97, 5) | (3, 0.5) |
| 3 | (93, 7) | (3, 0.5) |
| Üst kat girişi | (96, 8.5) | (4, 0.5) |

⚠️ **Tavanda delik bırakın** — x 90–100 arasında tavan olmasın, yoksa geçemezler.

---

## ÜST KAT — kaçış yolu (sağdan sola)

| Bölge | x aralığı | İçinde ne var |
|---|---|---|
| **Tavan arası** | 100 – 24 | 3 yer ateşi (x=80, 58, 36) · 1 canavar x=68, y=11 · **Musluk x=50** |
| **İniş** | 24 – 14 | Platformlarla alt kata iner |
| **Çıkış holü** | 14 – 0 | **BOSS x=6** · Çıkış kapısı x=1 |

### Üst kat platformları

| Nesne | Position | Scale |
|---|---|---|
| Üst zemin | (60, 8.5) | (84, 1) |
| Üst tavan | (60, 15.5) | (84, 1) |
| İniş 1 | (22, 7) | (3, 0.5) |
| İniş 2 | (18, 4.5) | (3, 0.5) |
| İniş 3 | (21, 2) | (3, 0.5) |

⚠️ **Musluk (x=50) üst katta zorunlu.** Kaçış yolu susuz olursa oyuncu kilitlenir.

---

## Tetikleyiciler

| Ne | Nerede | Bileşen |
|---|---|---|
| **Karabaş'ın ölümü** | Üst kat, x=44, y=10 | `EndingTriggerZone` → `DogDeath` |
| **Boss sahnesi** | Alt kat, x=16, y=1 | `EndingTriggerZone` → `BossScene` |
| Giriş kapısını kapatan alev | Antre x=1 — Karabaş kurtulunca aktif olsun | (görsel + collider) |

Köpeğin ölümü **oyuncunun arkasında** olmalı — tetikleyici soldan sağa değil,
sağdan sola geçerken tetikleniyor, yani oyuncu ateşi geride bırakmış oluyor.

---

## Kamera sınırları

Seviye bitince `CameraRig → CameraFollow`:

- **Use Bounds** ✅
- Min Bounds: **(10, 2)**
- Max Bounds: **(90, 10)**

Böylece kamera evin dışını göstermez.

---

## UYULMASI GEREKEN KURALLAR

Planı değiştirirseniz bile bunlara uyun:

1. **Her odadan erişilebilir en az bir Infinite su kaynağı** (musluk/küvet).
   Akvaryum ve bidon tek kullanımlık, onlar sayılmaz. Bu kural olmadan oyuncu kilitlenir.
2. **Kaçış yolunda da su olacak.** En sık atlanan madde bu.
3. **Karabaş en dipte** — arama uzun sürsün, havlama sesi oyuncuyu çeksin.
4. **Giriş kapısı kaçışta kapalı.** "Geldiğin yol yandı" hissi bu oyunun kalbi.
5. **Kaçış yolu ayrı bir güzergâh olsun**, aynı yoldan geri dönmek sıkıcı.
6. **Boss çıkışta**, kapının hemen önünde. Kurtuluşa bir adım kala.
7. **Toplam ~100 birim.** Daha büyük yaparsanız dolduramaz, boş hissettirir.

---

## Kurma sırası (saat 14-18)

1. Zemin + tavan + oda duvarları (düz koyu dikdörtgenler yeter)
2. Platformlar — **baştan sona yürünebildiğini test edin**, henüz düşman koymayın
3. Su kaynakları — her odada erişilebilir mi, kontrol edin
4. Kamera sınırları
5. Kişi B'nin prefabları: canavarlar, yer ateşleri, Karabaş
6. Tetikleyici bölgeler
7. Asset'lerle görsel geçiş

**2. adımdan sonra bir kere baştan sona oynayın.** Düşmansız, sadece yürüyüp zıplayarak.
Geçilemeyen bir boşluk varsa şimdi bulun, düşmanlar eklendikten sonra değil.
