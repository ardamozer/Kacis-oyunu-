# Kisi B — Kurulum Rehberi

> Bu dosya **Kisi B**'nin isleri icin. Kisi A `KURULUM.md`'yi kullanir.

---

# BOLUM 0 — Unity projesi ve test sahnesi (saat 0-1)

## 0.1 Unity surumunu Kisi A ile netlestir ⚠️

Bu makinede kurulu olan: **6000.5.7f1**
PLAN.md'de yazan: **6000.0.x LTS**

**Kisi A'ya sor: sen hangi surumu kurdun?** Ikinizin BIREBIR ayni surumde
olmasi sart. Farkli surumler `.unity` ve `.prefab` dosyalarini otomatik
yukseltir, git'te cozulmez cakismalar cikarir.

Ikiniz de 6000.5.7f1 kullanacaksaniz sorun yok, kod calisir. Sadece PLAN.md
satir 106'daki bosluga gercek surumu yazin.

## 0.2 Proje yoksa olustur

Repoda henuz Unity projesi yok — sadece `Scripts/` klasoru var.

**Kisi A projeyi olusturup push'ladiysa:** `git pull` yap, bu adimi atla.

**Sen olusturacaksan** (once A ile konus, ikiniz ayri ayri olusturursaniz
cakisirsiniz):

1. Unity Hub → **New project**
2. Template: **2D (Built-In Render Pipeline)** — ⚠️ URP DEGIL
3. Project name: `Game`
4. Location: `C:\Users\Cyborg\Desktop\Kacis-oyunu-`
   (yani proje `...\Kacis-oyunu-\Game\` icinde olusacak)
5. **Create project**

## 0.3 Proje ayarlari (bir kere)

`Edit → Project Settings → Editor`:
- **Asset Serialization → Mode → Force Text**
- **Version Control → Mode → Visible Meta Files**

`Edit → Project Settings → Player → Other Settings → Active Input Handling`:
- **Both** sec (kod eski `Input.GetAxisRaw` kullaniyor). Unity yeniden
  baslatmak isteyecek, kabul et.

## 0.4 Script'leri projeye tasi

Windows Gezgini'nde:

```
Kacis-oyunu-\Scripts\   →   Kacis-oyunu-\Game\Assets\Scripts\
```

Klasorun tamamini surukle. Unity acikken tasirsan kendi alginlar ve derler.
Console'da hata olmamali. (`.md` dosyalari Unity'yi rahatsiz etmez.)

## 0.5 Tag ve Layer olustur

`Edit → Project Settings → Tags and Layers`

**Tags** (+ ile ekle) — dordu de lazim:
`Player` · `Enemy` · `WaterSource` · `Dog`

⚠️ **`Dog` tag'ini atlamayin.** `FlameProjectile` kodu `CompareTag("Dog")`
cagiriyor; tag tanimli degilse Unity hata firlatir.
(Not: `Scripts/KURULUM.md` satir 40'ta hala eski `Cat` yaziyor — Kisi A'ya
duzeltmesini soyle, hayvan artik kopek.)

**Layers** (bos satirlara yaz):
`Ground` · `Player` · `Enemy`

## 0.6 Kendi test sahneni olustur

1. `Assets/Scenes/` klasorune sag tik → **Create → Scene**
2. Adi: **`TestScene_B`** ⚠️ bu isim tam boyle olmali
3. Cift tiklayip ac.

> 🚫 **`MainScene.unity` dosyasini ASLA acma.** Kisi A'nin. Acmak bile
> `.unity` dosyasinda degisiklik birakabilir.

---

# BOLUM 1 — Test ortami (zemin + gecici oyuncu)

## 1.1 Zemin

1. `Hierarchy → sag tik → 2D Object → Sprites → Square` · adi **`Ground`**
2. Inspector ust kisim → **Layer → Ground**
3. **Transform → Scale**: X = `30`, Y = `1`
4. **Transform → Position**: X = `0`, Y = `-4`
5. **Add Component → Box Collider 2D**

## 1.2 Duvar (canavarin icinden cikacagi sey)

1. `Hierarchy → sag tik → 2D Object → Sprites → Square` · adi **`Wall`**
2. **Layer → Ground**
3. **Scale**: X = `1`, Y = `6`
4. **Position**: X = `8`, Y = `-0.5`
5. **Add Component → Box Collider 2D**
6. Sprite Renderer → **Color** → koyu gri yap (duvar gibi gorunsun)

## 1.3 Gecici oyuncu

1. `Hierarchy → sag tik → 2D Object → Sprites → Square` · adi **`Player`**
2. **Layer → Player**, **Tag → Player** ⚠️ ikisi de sart
3. **Scale**: X = `0.8`, Y = `1.6`
4. **Position**: X = `0`, Y = `0`
5. **Add Component → Rigidbody 2D**
   - **Gravity Scale**: `4`
   - **Constraints → Freeze Rotation Z** ✅
   - **Collision Detection**: `Continuous`
6. **Add Component → Box Collider 2D**
7. **Add Component → Player Controller** (Kisi A'nin script'i)
8. **Add Component → Test Player Dummy** (benim test script'im)
9. **Add Component → Test Water Sprayer** (benim test script'im)

### Zemin kontrol noktasi

10. `Player`'a sag tik → **Create Empty** · adi **`GroundCheck`**
11. **Position**: X = `0`, Y = `-0.52`, Z = `0`
12. `Player`'i sec → **Player Controller** bileseninde:
    - **Ground Check** → Hierarchy'den `GroundCheck`'i surukle birak
    - **Ground Layer** → acilir menuden **sadece `Ground`**'u isaretle
      (⚠️ bos birakma, "Everything" secme)

**Test et:** Play → `A`/`D` yuru, `Space` zipla. Calismiyorsa
`KURULUM.md` satir 96'daki tabloya bak.

---

# BOLUM 2 — Alev mermisi prefabi (once bunu yap)

Canavar alevi doguracagi icin once alev hazir olmali.

1. `Hierarchy → sag tik → 2D Object → Sprites → Circle` · adi **`Flame`**
2. **Scale**: X = `0.4`, Y = `0.4`
3. Sprite Renderer → **Color** → turuncu (`FF8800` civari)
4. **Add Component → Circle Collider 2D**
   - **Is Trigger** ✅ ⚠️ **bunu isaretlemezsen alev oyuncuyu iter, hasar vermez**
5. **Add Component → Rigidbody 2D**
   - **Body Type → Kinematic** ⚠️ **bu olmadan alev duvara carptigini fark etmez**
     *(Unity'de iki collider'dan en az biri Rigidbody2D tasimazsa trigger olayi
     tetiklenmez. Zemin/duvar statik oldugu icin Rigidbody'yi aleve koyuyoruz.)*
6. **Add Component → Flame Projectile**
   - **Blocking Layers** → acilir menuden **`Ground`**'u isaretle

### Prefab yap

7. `Assets/` altinda `Prefabs` klasoru yoksa olustur, icine `Enemies` klasoru ac
8. Hierarchy'deki `Flame` objesini `Assets/Prefabs/Enemies/` klasorune **surukle birak**
   → mavi kutu ikonu olur, prefab oldu
9. Hierarchy'deki `Flame`'i **sil** (Delete). Artik prefabtan doguyor.

---

# BOLUM 3 — Duvar canavari

1. `Hierarchy → sag tik → 2D Object → Sprites → Square` · adi **`WallMonster`**
2. **Tag → Enemy**, **Layer → Enemy**
3. **Scale**: X = `1`, Y = `1.4`
4. **Position**: X = `8`, Y = `-1` ⚠️ **duvarin TAM ICINDE olsun** — bu onun
   gizli konumu. Kod cikis konumunu buradan hesaplar.
5. Sprite Renderer → **Color** → kirmizi/turuncu
6. Sprite Renderer → **Order in Layer** → `-1`
   *(duvarin ARKASINDA dursun, gizliyken gorunmesin. Wall'un Order in Layer'i
   `0` kalsin.)*
7. **Add Component → Box Collider 2D**
   - **Is Trigger** ✅ *(oyuncu icinden gecebilsin — PLAN.md: "duvar canavarlari
     oldurulmek zorunda degil, kacarak gecilebilir")*
8. **Add Component → Rigidbody 2D**
   - **Body Type → Kinematic**
9. **Add Component → Wall Monster**

## Ates noktasi

10. `WallMonster`'a sag tik → **Create Empty** · adi **`FirePoint`**
11. **Position**: X = `-0.8`, Y = `0.2`, Z = `0`
    *(canavar sola bakiyor, alev govdenin solundan ciksin — govdenin icinden
    cikarsa kendi collider'ina takilabilir)*

## Inspector alanlarini bagla

`WallMonster` objesini sec, **Wall Monster** bileseninde:

| Alan | Deger |
|---|---|
| **Detect Range** | `7` |
| **Player** | bos birak (kod Tag ile bulur) |
| **Facing** | `-1` (sola bakiyor — sag duvardaki canavar) |
| **Emerge Distance** | `0.9` |
| **Emerge Speed** | `5` |
| **Flame Prefab** | `Assets/Prefabs/Enemies/Flame` prefabini surukle birak |
| **Fire Point** | Hierarchy'den `FirePoint` objesini surukle birak |
| **Fire Interval** | `2` |
| **Max Health** | `40` |

⚠️ **En sik hata:** Flame Prefab'a Hierarchy'deki objeyi degil, `Assets/`
klasorundeki **prefabi** surukle.

## Prefab yap

12. `WallMonster` objesini `Assets/Prefabs/Enemies/` klasorune surukle birak.

> Bundan sonra degisiklikleri **prefabin uzerinde** yap (cift tikla → Prefab
> Mode). Sahnedeki kopyada yaparsan Kisi A'ya giden prefaba islemez.

---

# BOLUM 4 — Test listesi

**Play**'e bas ve sunlari sirayla dogrula:

| # | Test | Beklenen |
|---|---|---|
| 1 | Uzakta dur | Canavar duvarin icinde, gorunmuyor |
| 2 | Yaklas (7 birim) | Duvardan kayarak cikiyor |
| 3 | Bekle | ~0.7 sn sonra alev atiyor, sonra her 2 sn'de bir |
| 4 | Alev sana carpsin | Console: `[TEST] Hasar aldim: 1. Kalan can: 2` |
| 5 | Ust uste carpmayi dene | `[TEST] Dokunulmazlik acik` — tek alevden 2 hasar almamali |
| 6 | Alev duvara/zemine carpsin | Yok oluyor, sonsuza kadar gitmiyor |
| 7 | Uzaklas | Canavar duvara geri giriyor, ates kesiliyor |
| 8 | Yaklas + `F` (veya sol tik) basili tut | Canavar beyaza calip ~2 sn'de kuculerek soluyor |
| 9 | Canavarin icinden kos | Icinden geciyorsun, itmiyor |

## Calismiyorsa

| Belirti | Sebep |
|---|---|
| Canavar hic cikmiyor | Player objesinde **Tag = Player** degil |
| Cikiyor ama ates etmiyor | **Flame Prefab** veya **Fire Point** bos |
| Alev cikiyor ama hasar yok | Flame'de **Is Trigger** isaretli degil |
| Alev duvarin icinden geciyor | Flame'de **Rigidbody 2D / Kinematic** yok, ya da **Blocking Layers** bos |
| Alev dogar dogmaz yok oluyor | FirePoint canavarin collider'inin icinde — X'i `-1.0` yap |
| `F` basinca hicbir sey olmuyor | Canavarda **Box Collider 2D** yok |
| Console'da "Tag: Dog is not defined" | Bolum 0.5'te `Dog` tag'ini eklememissin |
| Canavar duvarin onunde gorunuyor | Sprite Renderer → **Order in Layer** `-1` degil |

## His ayari (5 dakika, deger)

Play modunda Inspector'dan oyna (⚠️ Play'den cikinca sifirlanir, begendigin
degeri buraya not et):

| Alan | Aralik | Etkisi |
|---|---|---|
| `Emerge Speed` | 4-10 | Buyuk = daha ani, korkutucu cikis |
| `Fire Interval` | 1.2-3 | Kucuk = daha baskici |
| `Flame → Speed` | 4-9 | Kucuk = dodge'lanabilir, buyuk = kacinilmaz |
| `Detect Range` | 5-9 | Ne kadar erken uyanir |

**Hedef:** oyuncu kosarak gecebilmeli ama "ucuz atlattim" hissetmeli.

---

# BOLUM 5 — Yer atesi (FloorFire)

## 5.1 Ates govdesi

1. `Hierarchy → sag tik → 2D Object → Sprites → Square` · adi **`FloorFire`**
2. **Tag → Enemy**, **Layer → Enemy**
3. **Scale**: X = `0.9`, Y = `0.5` *(basik olsun, oyuncu ustunden ziplayabilsin)*
4. **Position**: X = `3`, Y = `-3.25` *(zeminin tam ustune otursun)*
5. Sprite Renderer → **Color** → sari-turuncu (`FFC04D` civari)
6. **Add Component → Box Collider 2D**
   - ⚠️ **Is Trigger ISARETLEME.** Ezme (stomp) gercek carpisma gerektirir;
     trigger yaparsan oyuncu icinden gecer ve hicbir zaman ezemez.
7. **Add Component → Floor Fire**

> Rigidbody2D **gerekmez** — ates hic hareket etmiyor ve oyuncunun
> Rigidbody'si carpismayi zaten tetikliyor.

## 5.2 Dolan bar

Bar soldan saga dolmali. Sprite'i dogrudan olceklersen ORTADAN buyur ve
kotu gorunur. Cozum: bos bir "capa" objesi kullan.

1. `FloorFire`'a sag tik → **Create Empty** · adi **`FillBar`**
2. `FillBar` → **Position**: X = `-0.5`, Y = `0.75`, Z = `0`
   *(atesin sol ust kosesi. Y'yi Scene'de gozunle ayarla, atesin hemen ustunde dursun.)*
3. `FillBar`'a sag tik → **2D Object → Sprites → Square** · adi **`BarFill`**
4. `BarFill` → **Position**: X = `0.5`, Y = `0`, Z = `0` ⚠️ **bu ofset sart** —
   capayi olceklerken barin sol kenari sabit kalmasini saglayan sey bu
5. `BarFill` → **Scale**: X = `1`, Y = `0.15`
6. `BarFill` → Sprite Renderer → **Color** → beyaz, **Order in Layer** → `1`
7. `BarFill`'in **Collider'i olmasin** (Square sprite zaten collider'siz gelir,
   yanlislikla eklediysen sil — yoksa oyuncu barin ustunde durur)

## 5.3 Inspector alanlari

`FloorFire`'i sec, **Floor Fire** bileseninde:

| Alan | Deger |
|---|---|
| **Fill Time** | `4` |
| **Fill Bar** | Hierarchy'den **`FillBar`** objesini surukle (⚠️ `BarFill` degil, **`FillBar`**) |
| **Grown Scale** | `1.7` |
| **Contact Damage** | `1` |
| **Damage Interval** | `1` |
| **Max Health** | `25` |

## 5.4 Prefab yap

`FloorFire` objesini `Assets/Prefabs/Enemies/` klasorune surukle birak.

## 5.5 Test listesi

| # | Test | Beklenen |
|---|---|---|
| 1 | Play'e bas, bekle | Bar soldan saga doluyor |
| 2 | Bar dolmadan ustune zipla | Ates soner + oyuncu yukari seker |
| 3 | Yeniden Play, 4 sn bekle | Ates buyur, kizarir, bar kaybolur |
| 4 | Buyumus atese YANDAN degen | Console: `[TEST] Hasar aldim` — saniyede 1 kez |
| 5 | Buyumus atesin USTUNE zipla | ⚠️ **Yine sonmeli.** Sonmuyorsa oyun kilitlenir |
| 6 | `F` ile su sik | ~1.2 saniyede soner |
| 7 | Sonen ates | Kuculerek kayboluyor, yerinde bir sey kalmiyor |

⚠️ **5 numara en kritik test.** PLAN.md kilitlenme kurali 3: su bitse bile
oyuncu ilerleyebilmeli. Buyumus ates ezilemiyorsa oyun oynanamaz hale gelir.

## 5.6 Calismiyorsa

| Belirti | Sebep |
|---|---|
| Ustune ziplayinca sonmuyor | Box Collider 2D'de **Is Trigger** isaretli |
| Bar gorunmuyor | **Fill Bar** alani bos, ya da `BarFill`'in Order in Layer'i `1` degil |
| Bar ortadan buyuyor | `BarFill`'in **Position X**'i `0.5` degil |
| Barin ustunde duruyorum | `BarFill`'e yanlislikla collider eklenmis, sil |
| Buyumus ates hasar vermiyor | Player objesinde **Tag = Player** degil |
| Surekli hasar aliyorum | **Damage Interval** `0` kalmis, `1` yap |

---

# Commit

```bash
git pull
git add .
git commit -m "duvar canavari, alev mermisi ve yer atesi"
git push
```
