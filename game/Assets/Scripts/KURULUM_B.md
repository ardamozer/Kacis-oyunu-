# Kisi B — Kurulum Rehberi

> Bu dosya **Kisi B**'nin isleri icin. Kisi A `KURULUM.md`'yi kullanir.

---

# BOLUM 0 — Baslarken

> ✅ **Bolum 0.1-0.5 ARTIK GEREKMIYOR.** Kisi A Unity projesini olusturup
> push'ladi, script'ler `game/Assets/Scripts/` altina tasindi, tag ve
> layer'lar repoya islendi. Asagisi sadece dogrulama icin.

## 0.1 Once `git pull` yap

⚠️ **Unity KAPALIYKEN pull yap.** Unity acikken proje dosyalari degisirse
editor kafayi yer.

Proje konumu artik: `Kacis-oyunu-\game\` (kucuk harf `game`, kokte
`Scripts/` klasoru KALMADI — hepsi `game/Assets/Scripts/` altinda).

## 0.2 Dogrulama (Unity'yi acinca, 1 dakika)

| Kontrol | Yeri | Olmasi gereken |
|---|---|---|
| Unity surumu | Unity Hub, proje satiri | **6000.5.7f1** ✅ repoda kilitli |
| Input Handling | `Project Settings → Player → Other Settings` | **Both** ✅ repoda ayarli |
| Tag'ler | `Project Settings → Tags and Layers → Tags` | `Enemy`, `WaterSource`, `Dog` ✅ repoda islendi |
| Layer'lar | ayni ekran, Layers | 6=`Ground`, 7=`Player`, 8=`Enemy` ✅ repoda islendi |

> `Player` tag'i listede GORUNMEZ — Unity'nin yerlesik tag'i, zaten var.
> Inspector'daki Tag menusunde secebilirsiniz. Bu normal.

### Layer slotlari hakkinda — 6/7/8 DOGRU, dokunmayin

Bu rehberin onceki surumunde "Unity 0-7 arasini kilitler, layer'lari 8/9/10'a
tasiyin" yaziyordu. **Bu yanlisti, duzeltildi.**

Unity'de kilitli olan slotlar sadece **0 (Default), 1 (TransparentFX),
2 (Ignore Raycast), 4 (Water), 5 (UI)**. Buna karsilik **3, 6 ve 7 duzenlenebilir
"User Layer" slotlaridir** — Tags & Layers ekraninda basliklari da oyle yazar.

Yani `6=Ground`, `7=Player`, `8=Enemy` gecerli bir kurulum. Kisi A test etti:
`LayerMask.NameToLayer("Ground")` dogru degeri donuyor, oyuncu zemine basiyor,
su sadece `Enemy` layer'ina isabet ediyor.

⚠️ **Bunlari 8/9/10'a tasimaya CALISMAYIN.** Sahnedeki butun layer atamalari
ve `LayerMask` alanlari elden gecmek zorunda kalir — kazanci sifir, riski yuksek.

## 0.3 ⚠️ .meta DOSYALARI — EN KRITIK ADIM

Unity bir script'i ilk gorunce ona rastgele bir **GUID** uretip `.meta`
dosyasina yazar. Prefab'lar ve sahneler script'lere **isimle degil, bu GUID
ile** baglanir.

**Tehlike:** Ayni `.cs` dosyasi icin sen ve Kisi A ayri ayri Unity acarsaniz,
Unity ikinize **FARKLI GUID** uretir. Sonra senin prefab'in onun makinesinde
"Missing (Mono Script)" olur. Jam'de takim durduran klasik hata budur.

**Durum:**
- `Enemies/` → ✅ meta'lar Kisi A tarafindan uretilip push'landi, sorun yok.
- `Dog/` ve `Ending/` → ❌ meta'lari **henuz yok**, Unity'yi acinca sende
  olusacak.

**Kural — istisnasiz:**

1. Unity'yi ac, Console'un derlemeyi bitirmesini bekle.
2. **HEMEN** olusan `.meta` dosyalarini commit'le ve push'la:

```bash
git add game/Assets/Scripts
git commit -m "Dog ve Ending meta dosyalarini uret"
git push
```

3. Kisi A'ya "meta'lari push'ladim, pull yap" de. **O pull yapmadan
   `DogCompanion` / `EndingSequence`'i hicbir objeye baglamasin.**

## 0.4 Kendi test sahneni olustur

1. `Assets/Scenes/` klasorune sag tik → **Create → Scene**
2. Adi: **`TestScene_B`** ⚠️ bu isim tam boyle olmali
   (tam yol: `game/Assets/Scenes/TestScene_B.unity`)
3. Cift tiklayip ac.

> 🚫 **`MainScene.unity` dosyasini ASLA acma.** Kisi A'nin. Acmak bile
> `.unity` dosyasinda degisiklik birakabilir.
>
> `SampleScene.unity` Unity'nin varsayilan sahnesi — Kisi A onu silecek veya
> `MainScene`'e cevirecek, sen dokunma.

---

# BOLUM 1 — Test ortami

> ✅ **ELLE KURMANA GEREK YOK.** Kisi A bir Editor script'i yazdi.
> `TestScene_B` acikken ust menuden **`ALEV → Test Sahnesini Kur`**.
> Onay kutusuna **Kur** de. Zemin, platformlar, oyuncu, kamera, HUD,
> musluk ve akvaryum tek seferde olusur.

## Bu komut neyi siler, neyi silmez

Sadece su isimli objeleri silip yeniden yaratir:
`Ground` · `Player` · `Musluk` · `Akvaryum` · `TestTarget_Su` ·
`TestTarget_Ezme` · `HUD` · `Platform`

**Senin objelerine DOKUNMAZ:** `WallMonster`, `FloorFire`, `Karabas`,
`TrappedFire`, `Boss`, `EndingSequence`. Yani dusmanlarini kurduktan
sonra bile guvenle tekrar calistirabilirsin.

## ⚠️ Test yardimcilarimi ARTIK EKLEME

Bu komutun urettigi `Player`, Kisi A'nin **gercek** kodunu tasiyor:
`PlayerController` + `WaterGun` + `WaterTank` + `PlayerHealth`.

`TestPlayerDummy` ve `TestWaterSprayer` script'lerimi bu objeye
**ekleme.** Ikisi de artik gereksiz — dahasi `TestPlayerDummy` eklersen
`EndingSequence` boss sahnesinde gercek `PlayerHealth` yerine onu okur
ve final yanlis canla tetiklenir.

(O script'ler repoda kalsin, zarari yok; sadece sahnede kullanma.)

## Test et

Play → `A`/`D` yuru, `Space` zipla, sol fare ile su sik.
Ekranin solunda mavi su bari ve kalpler gorunmeli.

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
| Console'da "Tag: Dog is not defined" | `Dog` tag'i eksik — Bolum 0.2 dogrulama tablosuna bak |
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

# BOLUM 6 — Karabas (DogCompanion)

`FireMonster` PLAN.md revizesiyle kesildi (bkz. Bolum 5 notu). Sirada Karabas var.

## 6.1 Kopek objesi

1. `Hierarchy → sag tik → 2D Object → Sprites → Square` · adi **`Karabas`**
2. **Tag → Dog** ⚠️ repoda tanimli, menude gorunmeli
3. **Scale**: X = `0.9`, Y = `0.7`
4. **Position**: baslangicta sikisacagi yere koyun, orn. X = `4`, Y = `-3.3`
5. Sprite Renderer → **Color** → kahverengi/siyah (Karabas)
6. **Add Component → Audio Source**
   - **Play On Awake** ❌ (kod kendi PlayOneShot ile calistiriyor)
7. **Add Component → Dog Companion**
8. **Add Component → Test Dog Trigger** (benim test script'im)

> Fiziksel carpisma/Rigidbody GEREKMEZ — kopek sadece X ekseninde MoveTowards
> ile hareket ediyor, zeminin Y'sinde sabit kaliyor. Coklu kat varsa bu
> basitlestirme yeterli olmayabilir, o zaman Kisi A ile konusun.

## 6.2 Inspector alanlarini bagla

`Karabas` objesini sec, **Dog Companion** bileseninde:

| Alan | Deger |
|---|---|
| **Follow Distance** | `1.5` |
| **Follow Speed** | `4` |
| **Player** | bos birak (kod Tag ile bulur) |
| **Fire Detect Range** | `1.0` |
| **Audio Source** | bos birak (kod kendi bulur) |
| **Whine Clip** | ses dosyaniz varsa surukleyin, yoksa bos birakin (sessiz calisir) |
| **Happy Bark Clip** | ayni sekilde opsiyonel |
| **Heart Particle Prefab** | opsiyonel — yoksa sadece ses/log calisir, hata vermez |

⚠️ Ses klipleri (`whineClip`, `happyBarkClip`) henuz asset olarak yoksa **bos
birakin** — kod `null` kontrolu yapiyor, hata firlatmaz, sessiz calisir.
ASSETS.md'de ses kaynagi netlesince doldurun.

`Test Dog Trigger` bileseninde:

| Alan | Deger |
|---|---|
| **Interact Range** | `3` |

## 6.3 Test listesi

**Play**'e bas ve sunlari sirayla dogrula:

| # | Test | Beklenen |
|---|---|---|
| 1 | Oyuncuyu kopege yaklastir, uzaklastir | (Whine Clip bagliysa) yakinken daha yuksek sesle inleme, uzakta hic yok |
| 2 | Yakinken `E` | Console: `[TEST] OnPetted() cagrildi.` — kopek hareket etmiyor, sadece ses/kalp |
| 3 | Yakinken `R` | Console: `[TEST] OnRescued() cagrildi.` — mutlu havlama sesi (varsa) |
| 4 | Yakinken `T` | Console: `[TEST] StartFollowing() cagrildi.` — kopek artik oyuncuyu takip ediyor |
| 5 | Takip sirasinda uzaklas | Kopek `Follow Distance` mesafesine kadar arkandan geliyor, sonra duruyor |
| 6 | Takip sirasinda buyumus (kizarmis) bir `FloorFire`'in ustunden gec | Kopek kisa bir ziplama yapip atesi sonduruyor |
| 7 | `E`, `R`, `T` — uzaktayken (Interact Range disi) | Hicbir seyi tetiklemiyor |

## 6.4 Calismiyorsa

| Belirti | Sebep |
|---|---|
| Inleme hic duyulmuyor | **Whine Clip** bos, veya **Audio Source** eksik |
| `E`/`R`/`T` hicbir sey yapmiyor | Oyuncu **Interact Range** disinda, veya Player'da **Tag = Player** degil |
| Kopek takip etmiyor | Once `T` ile **StartFollowing** cagirmadiniz (Stuck/Freed halinde yurumuyor, bu KASITLI) |
| Kopek atesi sondurmuyor | Ates **buyumus (IsGrown)** olmali — kucukken kopek dokunmaz, kucuk ateste zaten zarar da yok |
| Kopek zeminin altina/ustune batiyor | Baslangic Y konumu zeminle hizali degil, Position Y'yi elle ayarlayin |

---

# BOLUM 7 — Sikisma + kurtarma ani (TrappedFireCover)

PLAN.md "Karabas'la bag kurma" madde 3: kurtarma ani PASIF olmamali, oyuncu
suyu sikip gercekten sondurmeli. Bu bolum `Karabas` prefabinin CHILD'i olarak
kurulur — Kisi A'ya bagli degildir, TestScene_B'de bastan sona test edilir.

## 7.1 Kurulum

1. `Karabas` objesine sag tik → **2D Object → Sprites → Square** · adi
   **`TrappedFire`**
2. **Layer → Enemy** ⚠️ **EN KRITIK ADIM, ATLAMAYIN.**
   Kisi A'nin `WaterGun`'i sadece `hitLayers` maskesindeki objeleri tarar ve
   o maske **`Enemy`**'ye ayarli. `TrappedFire` `Default` layer'inda kalirsa
   **su ona hic degmez, Karabas ASLA kurtarilamaz** ve oyun oradan devam
   etmez. (Tag `Dog` olan Karabas'in KENDISI degil, sadece bu ates cocugu
   `Enemy` layer'inda olacak.)
3. **Position**: Karabas'in TAM UZERINDE, orn. X = `0`, Y = `0.3`
   (Karabas'i orten bir ates gibi gorunmeli)
4. **Scale**: X = `0.7`, Y = `0.7`
5. Sprite Renderer → **Color** → turuncu/kirmizi (WallMonster'daki gibi)
6. **Add Component → Box Collider 2D** — **Is Trigger** ✅ gerekmez, ama
   WaterGun/TestWaterSprayer `OverlapBox` ile IWaterDamageable arayacagi
   icin trigger olmasi sorun yaratmaz, isaretleyebilirsiniz.
7. **Add Component → Trapped Fire Cover**

## 7.2 Inspector alanlarini bagla

`TrappedFire` objesini sec, **Trapped Fire Cover** bileseninde:

| Alan | Deger |
|---|---|
| **Dog** | bos birak (kod ebeveynde `DogCompanion`'i kendi bulur) |
| **Max Health** | `18` |
| **Delay Before Follow** | `1.2` |

## 7.3 Karabas'i baslangicta gizle (opsiyonel ama onerilir)

Sikismisken kopek govdesi tam gorunmesin diye `Karabas`'in Sprite Renderer'ini
`TrappedFire`'in ARKASINA alin: `Karabas` → **Order in Layer** `-1`,
`TrappedFire` → **Order in Layer** `0`.

## 7.4 Test listesi

**Play**'e bas, oyuncuyu `TrappedFire`'e yaklastir, su sik (`F`):

| # | Test | Beklenen |
|---|---|---|
| 1 | Sikmaya basla | Ates beyaza calip flash yapiyor |
| 2 | ~0.5 sn sikmeye devam et (18 can / ~40 su-sn) | Ates kuculerek kayboluyor |
| 3 | Ates kaybolur kaybolmaz | Karabas artik gorunur (Order in Layer dogruysa) |
| 4 | ~1.2 sn bekle | Kopek kendiliginden takibe basliyor (StartFollowing otomatik cagrildi) |
| 5 | Ates sonmeden birak, tekrar sik | Can kaldigi yerden devam ediyor, sifirlanmiyor |

`TestDogTrigger`'daki `R`/`T` tuslari hala calisir — atesi kurmadan once
kopegi izole test etmek icin kullanislidir, biri digerini bozmaz
(`DogCompanion.OnRescued` zaten Stuck disindaysa hicbir sey yapmiyor).

## 7.5 Calismiyorsa

| Belirti | Sebep |
|---|---|
| Su siksam da ates hic azalmiyor | `TrappedFire`'in **Layer**'i `Enemy` degil (en sik sebep), veya **Box Collider 2D** yok |
| Ates sonuyor ama kopek takip etmiyor | **Dog** alani yanlis objeyi gosteriyor, veya `Karabas` ile `TrappedFire` ayni ebeveyn altinda degil |
| Kopek hemen atesle beraber yuruyor | `Delay Before Follow` cok kucuk — kurtarma anina nefes payi birakin |

---

# BOLUM 8 — Final (EndingSequence)

⚠️ **PLAN.md kirmizi cizgisi:** final sahnesi (kopegin olumu + boss) saat
19.5'ta kaba haliyle bitmis olmali. Asagidaki kurulum minimum — hicbir yeni
asset gerektirmiyor (kutu/daire sprite'lar + renk).

## 8.1 Yonetici obje

1. `Hierarchy → sag tik → Create Empty` · adi **`EndingSequence`**
2. **Add Component → Ending Sequence**
3. **Add Component → Test Ending Trigger** (benim test script'im)

## 8.2 Kopegin olumu icin gorseller

1. `Hierarchy → sag tik → 2D Object → Sprites → Circle` · adi **`BigDeathFlame`**
   - **Scale**: X = `2.5`, Y = `2.5` · Renk → koyu turuncu/kirmizi
   - Baslangicta **devre disi birakin** (Inspector'da checkbox'i kapatin)
2. `Hierarchy → sag tik → 2D Object → Sprites → Square` · adi **`Collar`**
   - **Scale**: X = `0.3`, Y = `0.2` · Renk → kahverengi/bej (tasma gibi)
   - Baslangicta **devre disi birakin**
3. `Hierarchy → sag tik → Create Empty` · adi **`DogDeathPoint`**
   - Karabas'in olecegi yere koyun (oyuncunun GORMEDIGI, arkasindaki nokta)

## 8.3 Boss gorseli

1. `Assets/Prefabs/Enemies/WallMonster` prefabini sahneye surukleyin, adi
   **`Boss`** yapin (kopyasi, orijinal prefabi bozmaz)
2. `Boss` uzerindeki **Wall Monster** bilesenini SILIN (boss'un kendi ates
   mantigi `EndingSequence` tarafindan yonetiliyor, WallMonster'a gerek yok)
3. **Scale**: mevcut degerin 4 kati (orn. WallMonster X=1,Y=1.4 ise → X=4,Y=5.6)
4. Sprite Renderer → **Color** → koyu kirmizi
5. **Layer → Enemy** olduğunu dogrulayin (WallMonster'dan devralmis olmali).
   Degilse `WaterGun` bossa su sikildigini fark etmez, oyuncu "olduremiyorum"
   yerine "oyun bozuk" hisseder.
6. **Add Component → Boss Core**
7. Baslangicta **devre disi birakin**
8. `Boss`'a sag tik → **Create Empty** · adi **`BossFirePoint`** — atesin
   cikacagi nokta

## 8.4 Hayalet gorseli

1. `Hierarchy → sag tik → 2D Object → Sprites → Square` · adi **`DogGhost`**
   - Karabas ile ayni Scale
   - Renk → acik mavi/beyaz, **Alpha ~%50**
   - Baslangicta **devre disi birakin**
2. `Hierarchy → sag tik → Create Empty` · adi **`GhostStart`** (ekranin bir
   kenari) ve **`GhostEnd`** (diger kenari) — hayalet bu ikisi arasinda gecer

## 8.5 Inspector alanlarini bagla

`EndingSequence` objesini sec, **Ending Sequence** bileseninde:

| Alan | Deger |
|---|---|
| **Player** | bos birak |
| **Dog** | Hierarchy'den `Karabas`'i surukle |
| **Dog Death Point** | `DogDeathPoint` |
| **Big Flame Visual** | `BigDeathFlame` |
| **Collar Pickup** | `Collar` |
| **Silence Duration** | `3` |
| **Boss Object** | `Boss` |
| **Boss Fire Point** | `Boss/BossFirePoint` |
| **Flame Prefab** | `Assets/Prefabs/Enemies/Flame` (WallMonster ile ayni) |
| **Bass Fire Interval** | `2.5` |
| **Exit Door** | bos birakilabilir (Kisi A entegre ederken baglar) |
| **Ghost Visual** | `DogGhost` |
| **Ghost Start** | `GhostStart` |
| **Ghost End** | `GhostEnd` |
| **Ghost Cross Duration** | `2.5` |

## 8.6 Test listesi

**Play**'e bas:

| # | Test | Beklenen |
|---|---|---|
| 1 | `1` tusuna bas | Kontrol kilitleniyor, ses kesiliyor, `BigDeathFlame` buyuyor, Karabas kayboluyor |
| 2 | 3 sn bekle | Alev kuculup kayboluyor, `Collar` yerde beliriyor, ses/kontrol geri geliyor |
| 3 | `2` tusuna bas | `Boss` beliriyor, ~1.25 sn sonra alev atmaya basliyor (her 2.5 sn'de bir) |
| 4 | Boss'a su sik | Beyaza calip flash yapiyor, AMA yok olmuyor / kuculmuyor |
| 5 | Bossun alevinden hasar al (`TestPlayerDummy` cani 1'e dusene kadar) | Kontrol kilitleniyor, `Collar` parliyor, `DogGhost` ekrani geciyor, `Boss` kayboluyor |
| 6 | Sahne bitince | Kontrol geri geliyor, `Exit Door` bagliysa gizleniyor |

## 8.7 Calismiyorsa

| Belirti | Sebep |
|---|---|
| `1`/`2` hicbir sey yapmiyor | `Test Ending Trigger`'da **Ending** alani bos, veya ayni objede **Ending Sequence** yok |
| Boss can 1'de otomatik tetiklenmiyor | Player'da **Test Player Dummy** yok, veya cani hic 1'e inmedi (Console'dan `[TEST] Hasar aldim` loglarini takip edin) |
| Boss'a su sikince yok oluyor | `Boss` uzerinde hala eski **Wall Monster** bileseni duruyor, silinmemis |
| Alev/hayalet gorseli hic gorunmuyor | Ilgili obje Inspector'da referans alanina surukLENMEMIS |

---

# Commit

```bash
git pull
git add .
git commit -m "final sahnesi: EndingSequence, BossCore"
git push
```
