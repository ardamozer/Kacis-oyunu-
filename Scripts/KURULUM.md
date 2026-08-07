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

Tags kismina da ekleyin: `Player`, `Enemy`, `WaterSource`, `Cat`

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

## Sirada ne var

Bu calisinca haber verin — su sistemi (`WaterTank`, `WaterGun`, `WaterSource`) hazir.
Plana gore saat 5-10 arasi is o.
