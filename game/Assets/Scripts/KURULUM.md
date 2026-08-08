# Script Kurulum Rehberi — Kisi A

## 0. Once: script'leri Unity projesine tasiyin

Unity projesini olusturduktan sonra (`D:\Kacis oyunu\Game`), bu `Scripts` klasorunu
proje icine tasiyin:

```
D:\Kacis oyunu\Scripts\  →  D:\Kacis oyunu\Game\Assets\Scripts\
```

Unity acikken tasirsaniz otomatik alginlar ve derler.

---

## 1. Input ayari (ilk is — atlarsaniz hicbir sey calismaz)

Unity 6'da yeni Input System varsayilan olabilir. O zaman `Input.GetAxisRaw` hata verir.

`Edit → Project Settings → Player → Other Settings → Active Input Handling`
→ **"Input Manager (Old)"** veya **"Both"** secin.

Unity yeniden baslatmak isteyecek, kabul edin. (Yeni Input System jam icin gereksiz karmasik.)

> **Not:** Kod `rb.linearVelocity` kullaniyor — bu Unity 6 yazimi. Eger farkli bir
> surumdeyseniz ve "linearVelocity bulunamadi" hatasi alirsaniz, hepsini
> `rb.velocity` ile degistirin. Ctrl+H ile tek seferde olur.

---

## 2. Layer olusturun

`Edit → Project Settings → Tags and Layers → Layers`

Bos satirlara ekleyin:
- `Ground`
- `Player`
- `Enemy`

Tags kismina ekleyin: `Enemy`, `WaterSource`, `Dog`

⚠️ **`Player` tag'ini EKLEMEYIN** — Unity'de zaten yerlesik. Eklerseniz menude
cift gorunur. Ustteki "Tags" listesi bos gorunse bile Player mevcuttur.

---

## 3. Zemin yapin

1. `Hierarchy → sag tik → 2D Object → Sprites → Square`, adini **Ground** yapin
2. Inspector'da **Layer** acilir menusunden **Ground** secin
3. `Transform → Scale`: X = **30**, Y = **1**
4. `Transform → Position`: Y = **-4**
5. **Add Component → Box Collider 2D**

---

## 4. Oyuncuyu yapin

1. `Hierarchy → sag tik → 2D Object → Sprites → Square`, adini **Player** yapin
2. Inspector'da **Layer** → **Player**, **Tag** → **Player**
3. `Transform → Scale`: X = **0.8**, Y = **1.6** (insan orantisi)
4. **Add Component → Rigidbody 2D**
   - **Gravity Scale: 4** (varsayilan 1 cok yavas hissettirir)
   - **Constraints → Freeze Rotation Z** ✅ (kod da yapiyor ama garanti olsun)
   - **Collision Detection: Continuous** (hizli duserken zeminden gecmeyi onler)
5. **Add Component → Box Collider 2D**
6. **Add Component → Player Controller** (yazdigimiz script)

### Zemin kontrol noktasi

7. Player'a **sag tik → Create Empty**, adini **GroundCheck** yapin
8. `Transform → Position`: X = **0**, Y = **-0.52**, Z = **0**
   *(Player'in Y scale'i 1.6 oldugu icin ayaklar -0.5 civarinda. Yesil daire
   ayaklarin tam altina degecek sekilde olmali — Scene penceresinde gorursunuz.)*

### PlayerController alanlarini baglayin

Player'i secin, Inspector'da:

| Alan | Deger |
|---|---|
| **Ground Check** | Hierarchy'den **GroundCheck** objesini surukleyip birakin |
| **Ground Layer** | Acilir menuden **Ground**'u secin (sadece Ground isaretli olsun) |

⚠️ **En sik yapilan hata:** Ground Layer'i bos birakmak veya "Everything" secmek.
Bos birakirsaniz hic ziplayamazsiniz, "Everything" secerseniz havada ziplayabilirsiniz.

---

## 5. Test edin

**Play**'e basin:

- `A` / `D` veya ok tuslari → yurumeli
- `Space` → ziplamali
- Kenardan dusunce kisa bir an hala ziplayabilmeli (coyote time)
- Zipla tusunu erken birakinca daha alcak ziplamali (degisken yukseklik)

### Calismiyorsa

| Belirti | Sebep |
|---|---|
| Hicbir sey olmuyor, konsolda InputSystem hatasi | Adim 1'i atlamissiniz |
| Yuruyor ama ziplamiyor | **Ground Layer** bos veya **GroundCheck** baglanmamis |
| Havada da ziplayabiliyor | Ground Layer "Everything" secili |
| Karakter devriliyor | Rigidbody2D → Freeze Rotation Z isaretli degil |
| Zeminden gecip dusuyor | Collision Detection **Continuous** olmali |
| Cok agir/yavaz hissettiriyor | Rigidbody2D → **Gravity Scale**'i artirin (4-6 deneyin) |

---

## 6. His ayari — burada 15 dakika harcayin, deger

Kontrol hissi oyununuzun ilk 5 saniyesinde birakacagi izlenimdir. Play modundayken
Inspector'dan degerleri degistirebilirsiniz (⚠️ **Play'den cikinca sifirlanir**,
begendiginiz degeri bir yere not edin).

Denemeye deger araliklar:

| Alan | Aralik | Etkisi |
|---|---|---|
| `moveSpeed` | 6-9 | Genel tempo |
| `jumpForce` | 13-18 | Zipla yuksekligi |
| `Gravity Scale` (Rigidbody2D) | 3-6 | Agirlik hissi |
| `fallMultiplier` | 2-4 | Dusme keskinligi |
| `acceleration` | 50-90 | Kontrolun keskinligi |

Hedef: itfaiyeci **agir ama cevik** hissettirsin. Buz gibi kaymasin, tas gibi de olmasin.

---

---

## 7. Su sistemi (plana gore saat 5-10 isi)

Once hareket calissin, sonra buraya gecin.

### 7a. Namlu noktasi

1. **Player**'a sag tik → **Create Empty**, adini **Nozzle** yapin
2. `Transform → Position`: X = **0.5**, Y = **0.2**
   *(Karakterin onunde, bel hizasinda. Sprite'a gore ayarlayin.)*

### 7b. Component'leri ekleyin

**Player**'i secin → **Add Component** ile ikisini de ekleyin:

**Water Tank**
| Alan | Deger |
|---|---|
| Max Water | 100 |
| Start Water | 100 |
| Low Warning Percent | 0.25 |

**Water Gun**
| Alan | Deger |
|---|---|
| Water Per Second | 15 |
| Damage Per Second | 20 |
| Range | 3.5 |
| Width | 1.2 |
| **Nozzle** | Hierarchy'den **Nozzle** objesini surukleyin |
| **Hit Layers** | Acilir menuden **Enemy**'i secin |
| Spray Effect / Spray Sound | Simdilik bos birakin |

⚠️ **Hit Layers'i bos birakirsaniz su hicbir seye isabet etmez.** En sik hata bu.

### 7c. Test icin sahte bir dusman

Kisi B'nin dusmanlari gelmeden test etmek icin:

1. `2D Object → Sprites → Square`, adi **TestEnemy**, **Layer → Enemy**
2. **Add Component → Box Collider 2D**
3. Yeni bir script: `Assets/Scripts/Player/TestDummy.cs`

```csharp
using UnityEngine;

// Sadece test icin. Kisi B'nin gercek dusmanlari gelince silin.
public class TestDummy : MonoBehaviour, IWaterDamageable
{
    public float health = 60f;

    public void TakeWater(float amount)
    {
        health -= amount;
        Debug.Log($"Isabet! Kalan can: {health:F0}");
        if (health <= 0f) Destroy(gameObject);
    }
}
```

4. Bu script'i **TestEnemy**'ye ekleyin

### 7d. Su kaynagi (musluk)

1. `2D Object → Sprites → Square`, adi **Musluk**, mavi renk verin
2. **Add Component → Box Collider 2D** → **Is Trigger** ✅ isaretleyin
3. **Add Component → Water Source**
   - Type: **Infinite**
   - Refill Per Second: **30**

### 7e. Test

**Play** → sol fare tusu (veya `Ctrl`) basili tutun:

- Onunuze mavi bir kutu ciziliyor mu? (Player secili iken Scene penceresinde gorunur)
- TestEnemy'ye dogru sikinca konsolda "Isabet!" yaziyor mu? 3 saniyede olmeli.
- Yukari ok basili tutup sikinca huzme yukari donuyor mu?
- ~7 saniye sonra su bitip sikma duruyor mu?
- Musluga girip beklerseniz doluyor mu?

| Belirti | Sebep |
|---|---|
| Su sikmiyor | `Fire1` tusu: sol fare veya `Ctrl`. Player'da WaterGun var mi? |
| Sikiyor ama hasar yok | **Hit Layers** bos, ya da TestEnemy'nin Layer'i **Enemy** degil |
| Su hic bitmiyor | Water Tank component'i eklenmemis |
| Musluk doldurmuyor | Collider'da **Is Trigger** isaretsiz |

### 7f. Denge ayari

Su sinirli olmasi **gerilim** yaratmali, **sinir bozmamali**. Hedef his:
"dikkatli kullanmam lazim", asla "susuz kaldim, ne yapacagim?".

- Cok sik susuz kaliyorsaniz → `waterPerSecond`'i dusurun (15 → 10)
- Hic bitmiyorsa → yukseltin (15 → 20)
- Dusmanlar cok gec oluyorsa → `damagePerSecond`'i yukseltin

⚠️ Seviye tasarlarken: **her odada erisilebilir en az bir Infinite kaynak.**
Yoksa oyuncu kilitlenir.

---

## Sirada ne var

Su sistemi calisinca haber verin — `PlayerHealth` (3 kalp, hasar, ekran sarsintisi,
ekran kenari alev efekti) hazirlanacak. Plana gore saat 10-12 isi o.
