using System.Collections;
using UnityEngine;

/// <summary>
/// FINAL — kopegin olumu + boss sahnesi + hayalet. PLAN.md'nin "kesinlikle
/// kesilmeyecek" dedigi tek blok, saat 19.5 hedefiyle oncelikli.
///
/// Iki giris noktasi var (sozlesme, Kisi A cagirir):
///   TriggerDogDeath()  — oyuncu arkasindan gelen buyuk alevle kopek olur.
///   TriggerBossScene() — cikis koridorunda dev alev (boss) ortaya cikar.
///
/// Boss dovusu YOK (PLAN.md "Boss = dovus DEGIL, sahne"): can hic azalmaz,
/// oyuncunun canı 1 kalbe dusunce sahne KENDILIGINDEN tetiklenir.
/// </summary>
public class EndingSequence : MonoBehaviour
{
    [Header("Oyuncu")]
    [Tooltip("Bos birakin — Tag='Player' ile kendi bulur.")]
    public Transform player;

    [Header("Kopegin olumu")]
    [Tooltip("Sahnedeki Karabas. Olum aninda gizlenir.")]
    public DogCompanion dog;
    [Tooltip("Buyuk alevin cikacagi nokta — kopegin arkasi, oyuncunun gormedigi yer.")]
    public Transform dogDeathPoint;
    [Tooltip("Yukselip sonecek buyuk alev gorseli. Sahnede devre disi baslamali.")]
    public GameObject bigFlameVisual;
    [Tooltip("Alev sonunce yerde kalan tasma. Sahnede devre disi baslamali.")]
    public GameObject collarPickup;
    public float silenceDuration = 3f;
    public float flameRiseDuration = 1f;
    public float flameFallDuration = 1f;

    [Header("Olum — dusen tahta (tercih edilen yontem)")]
    [Tooltip("Karabas'in uzerine dusecek yanan tahta.\n" +
             "Atanirsa buyuyen alev yerine BU kullanilir.\n" +
             "Auto Trigger'i kod zaten kapatiyor — erken dusmez.")]
    public FallingDebris deathDebris;
    [Tooltip("Tahta dustukten sonra kopegin gizlenmesine kadar gecen sure. " +
             "Tahtanin yere varma suresine gore ayarlayin.")]
    public float debrisImpactDelay = 0.55f;

    [Header("Olum — Karabas'in hamlesi")]
    [Tooltip("Karabas oyuncuyu kac birim yana iter. 0 = itme yok.\n" +
             "Anlami: kopek seni kurtariyor. Mekanigi: dusen tahtanin " +
             "altindan cikiyorsun, tahta sana hasar vermiyor.")]
    public float pushDistance = 2f;
    public float pushDuration = 0.35f;
    [Tooltip("Kopegin oyuncunun yerine atilma suresi. Itmeyle ayni anda calisir.\n" +
             "Itmeden KISA olsun — once kopek carpsin, sonra oyuncu savrulsun.")]
    public float dogLungeDuration = 0.25f;

    [Tooltip("Kopek carpmadan ONCE oyuncunun bu kadar yanina gelir.\n" +
             "0 yaparsaniz kopek dogrudan oyuncunun UZERINE lerp'lenir ve iki " +
             "sprite ust uste biner — 'itti' degil 'icine girdi' gorunur. " +
             "Playtest'te bildirilen sorun buydu.")]
    public float dogContactGap = 0.95f;

    [Tooltip("Yaklasma suresi. Kisa olmali: kopek kosarak geliyor, yuruyerek degil.")]
    public float dogApproachDuration = 0.28f;

    [Tooltip("Carpma aninda kopegin sicrama yuksekligi. 0 = kaymali (kotu).\n" +
             "Kavis, hareketi 'yurudu' degil 'atildi' yapan sey.")]
    public float lungeHopHeight = 0.5f;
    [Tooltip("Tahta carpinca kameranin sarsilmasi. 0 = kapali.")]
    public float impactShakeDuration = 0.4f;
    public float impactShakeMagnitude = 0.55f;

    [Header("Olum — sinematik okunabilirlik")]
    [Tooltip("Playtest: 'kopegin beni ittigi ve altinda kaldigi anlasilmiyor'.\n" +
             "Kapatirsaniz eski (hizli, duz) sahne oynar.")]
    public bool cinematicDeath = true;

    [Tooltip("Sinematik moddaki atilis suresi. Serilestirilmis dogLungeDuration " +
             "(0.25) cok kisa kaliyor — goz yakalamadan bitiyor.")]
    public float cinematicLungeDuration = 0.45f;

    [Tooltip("Agir cekim carpani. 1 = kapali. Atilistan carpmaya kadar surer, " +
             "sonra normale doner. Sessizlik NORMAL hizda yasanir.")]
    [Range(0.15f, 1f)] public float slowMotionScale = 0.35f;

    [Tooltip("Tahta dustukten sonra kopegin ALTINDA GORUNUR kaldigi sure " +
             "(gercek saniye). Kayip goruldukten sonra gizlenmeli.")]
    public float dogPinnedDuration = 0.8f;

    [Tooltip("Tahta uzerine dusunce kopegin dikey ezilme orani. Kanli degil, " +
             "sadece 'altinda kaldi' okunsun diye.")]
    [Range(0.2f, 1f)] public float dogSquashY = 0.45f;

    [Header("Olum — tehdidin gosterilmesi")]
    [Tooltip("Kiris, dusmeden hemen once oyuncunun TAM USTUNDE titreyip parlar.\n\n" +
             "⚠️ Bu bir UYARI DEGIL, bir ACIKLAMA. Kontrol coktan alinmis durumda " +
             "(CanControl = false), yani oyuncu kacamaz ve 'kacabilirdim' diyemez. " +
             "KAT_PLANI'nin yasakladigi sey oyuncu HALA OYNARKEN uyari vermek; " +
             "burada amac 'bu sana geliyordu' cumlesini kurmak, ki kopegin araya " +
             "girmesi anlam kazansin. Unlem/yer isareti KULLANMIYORUZ — onlar " +
             "oyunun 'kac' dili, burada kacis yok.")]
    public bool revealThreat = true;

    [Tooltip("Tehdidin gosterildigi sure (gercek saniye).")]
    public float threatRevealDuration = 0.5f;

    public Color threatGlowColor = new Color(1f, 0.75f, 0.35f);
    public float threatShakeMagnitude = 0.14f;

    [Tooltip("Kiris gorundukten sonra Karabas bir kez havlar — tek uyari, ve " +
             "zaten gec. Oyunun ondan duyulan son sesi bu.")]
    public bool barkBeforeLunge = true;
    [Range(0f, 1f)] public float deathBarkVolume = 1f;

    [Header("Replikler")]
    [Tooltip("Duygusal anlarda karakterlerin ustunde beliren yazilar.\n" +
             "Bir repligi BOSALTIRSANIZ o an sessiz gecer — sistemi tamamen " +
             "kapatmak icin hepsini bosaltin.")]
    public bool useSpeech = true;

    [Tooltip("Karabas atilirken — tek uyari, ve zaten gec.")]
    public string lineDogWarn = "DİKKAT ET!";

    [Tooltip("Carpmadan HEMEN sonra, oyuncunun ustunde. Ham tepki — daha " +
             "anlamlandiramadi, sadece sok. Kederi bundan sonra geliyor.")]
    public string linePlayerShock = "NAAPTIN SEN?!";

    [Tooltip("Sessizlikten sonra, oyuncunun ustunde. Tasma yerde kalirken.")]
    public string lineAfterDeath = "Beni kurtarmak için kendini feda ettin...";

    [Tooltip("Ev sarsildiktan sonra — oyuncu kacmasi gerektigini anlar.")]
    public string lineMustEscape = "Ev çöküyor... çıkmalıyım.";

    [Tooltip("Hayalet tam caresiz anda belirdiginde.")]
    public string lineGhostArrive = "Merak etme, yetiştim.";

    [Tooltip("Hayalet giderken — vedasi.")]
    public string lineGhostFarewell = "Cennette görüşürüz, dostum.";

    [Tooltip("Boss sonduktan sonra, oyuncunun ustunde.")]
    public string lineFinalThanks = "Yaptığın iyiliği asla unutmayacağım.";

    [Tooltip("Bir repligin ekranda kalma suresi (gercek saniye).")]
    public float lineDuration = 2.4f;

    [Header("Olumden sonra — ev sarsilir")]
    [Tooltip("Kopek oldukten sonra ev sarsilir: 'burada kalamazsin'. " +
             "0 = kapali.")]
    public float houseTremorDuration = 1.7f;
    public float houseTremorMagnitude = 0.5f;

    public Color dogLineColor = new Color(0.65f, 0.85f, 1f);
    public Color playerLineColor = Color.white;

    [Header("Sinema bantlari")]
    [Tooltip("Ara sahnelerde ust/alttan siyah paravanlar kapanir. Oyuncu " +
             "kontrolun kasten alindigini anlar — 'tuşlar tutmuyor' degil " +
             "'sahne oynuyor'. Olum ve hayalet sahnelerinde calisir.")]
    public bool useCinematicBars = true;

    [Tooltip("Bant yuksekligi, ekran yuksekliginin orani. 0.12 = %12 ust + %12 alt.")]
    [Range(0.05f, 0.25f)] public float barHeightFraction = 0.12f;

    [Tooltip("Bantlarin kapanma/acilma suresi (gercek saniye).")]
    public float barFadeDuration = 0.35f;

    [Header("Olum — ses")]
    [Tooltip("Tahta yere carpinca calar. Bos birakilabilir.")]
    public AudioClip crashClip;
    [Range(0f, 1f)] public float crashVolume = 1f;
    [Tooltip("Carpma sesi duyulduktan sonra sessizlige gecise kadar bekleme.\n" +
             "Cok kisa olursa carpma kesilir, cok uzun olursa sessizlik gec gelir.")]
    public float silenceStartDelay = 0.45f;
    [Tooltip("Karabas'in hayaleti gecerken calar. Bos birakilabilir.")]
    public AudioClip ghostClip;
    [Range(0f, 1f)] public float ghostVolume = 0.8f;

    [Header("Boss")]
    [Tooltip("BossCore tasiyan dev alev objesi. Sahnede devre disi baslamali.")]
    public GameObject bossObject;
    public Transform bossFirePoint;
    [Tooltip("WallMonster ile ayni FlameProjectile prefabi kullanilabilir.")]
    public GameObject flamePrefab;
    public float bossFireInterval = 2.5f;
    [Tooltip("Opsiyonel. Ghost sahnesi bitince gizlenir (= kapi acildi).")]
    public GameObject exitDoor;

    [Header("Hayalet")]
    [Tooltip("Kopek sprite'i mavi/beyaz tint + yari saydam. Sahnede devre disi baslamali.")]
    public GameObject ghostVisual;
    public Transform ghostStart;
    public Transform ghostEnd;
    public float ghostCrossDuration = 2.5f;

    [Tooltip("Hayalet yolun bu noktasinda durup konusur (0-1). 0.5 = ortada, " +
             "yani oyuncunun hizasinda.")]
    [Range(0.2f, 0.8f)] public float ghostPauseAt = 0.5f;

    [Tooltip("Durup konustugu sure (gercek saniye). Repliklerin ikisi de " +
             "bu surenin icine sigar.")]
    public float ghostPauseDuration = 3.4f;

    [Header("Hayalet — girisi")]
    [Tooltip("Hayalet gelmeden onceki MUTLAK SESSIZLIK (gercek saniye).\n" +
             "Oyuncu bu boslukta kaybettigini saniyor. Kisaltirsaniz havlama " +
             "'sahnenin devami' gibi duyulur, uzatirsaniz oyuncu oyunun " +
             "donduğunu saniyor. 1.2-1.8 arasi dogru bolge.")]
    public float despairSilenceDuration = 1.4f;

    [Tooltip("Havlamadan sonra hayalet gorunene kadar gecen sure. Once SES, " +
             "sonra goruntu — oyuncu kim geldigini gormeden anlasin.")]
    public float ghostBarkLead = 0.55f;

    [Tooltip("Hayaletin havlamasi. Bos birakilirsa Karabas'in havlamasi kullanilir.")]
    public AudioClip ghostBarkClip;
    [Range(0f, 1f)] public float ghostBarkVolume = 1f;
    public Color collarGlowColor = new Color(1f, 1f, 0.6f);
    [Tooltip("Boss'un sonme suresi. Aniden yok olmasi 'bug' gibi duruyor.")]
    public float bossFadeDuration = 1.2f;

    bool dogDeathTriggered;
    bool bossActive;
    bool bossSceneEnding;
    bool bossResolved;
    float bossFireTimer;

    /// <summary>Boss su an ayakta mi? (Cikis kapisi bunu okuyor.)</summary>
    public bool BossActive => bossActive;

    /// <summary>
    /// Boss sahnesi bitti mi (hayalet gecti, alev sondu)? `bossObject.activeSelf`
    /// bunu ayirt edemiyor: hem "henuz cagrilmadi" hem "coktan bitti" durumunda
    /// kapali. Cikis kapisinin ikisini karistirmamasi lazim.
    /// </summary>
    public bool BossResolved => bossResolved;

    // Boss sirasinda can okumasinin izi — "hayalet neden gelmedi" sorusu icin.
    int lastBossHealth = -1;
    bool bossHealthUnreadable;

    // Alevin sahnede ayarladiginiz gercek boyutu. Sabit Vector3.one KULLANMIYORUZ:
    // Inspector'da alevi buyutup kucultmekte ozgur olmalisiniz, kod onu ezmesin.
    Vector3 bigFlameBaseScale = Vector3.one;

    // Kopegin oldugu yer — tasma buraya birakilir.
    Vector3 dogLastPosition;

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        // Kopek sahneye `Finali Sahneye Kur`dan SONRA konduysa alan bos kalir.
        // Sahnede su an dolu, ama bos kalmasi sessizce olumu iptal ettigi
        // (ve bunu ancak oynarken fark ettigimiz) icin garantiye aliyoruz.
        if (dog == null)
        {
            dog = FindFirstObjectByType<DogCompanion>(FindObjectsInactive.Include);
            if (dog != null)
                Debug.LogWarning("[Final] `dog` alani bostu, sahnedeki Karabas otomatik baglandi.");
            else
                Debug.LogError("[Final] Sahnede DogCompanion YOK — olum sahnesi kopegi gizleyemez!");
        }

        if (bossObject != null) bossObject.SetActive(false);

        if (bigFlameVisual != null)
        {
            bigFlameBaseScale = bigFlameVisual.transform.localScale;
            bigFlameVisual.SetActive(false);
        }

        if (collarPickup != null) collarPickup.SetActive(false);
        if (ghostVisual != null) ghostVisual.SetActive(false);
    }

    void Update()
    {
        if (bossActive && !bossSceneEnding) BossUpdate();
    }

    // ============================================================
    //  Sozlesme — Kisi A cagirir
    // ============================================================

    /// <summary>Oyuncunun arkasindan cikan buyuk alevle Karabas olur.</summary>
    public void TriggerDogDeath()
    {
        if (dogDeathTriggered) return;
        dogDeathTriggered = true;
        StartCoroutine(DogDeathRoutine());
    }

    /// <summary>Cikis koridorunda dev alev (boss) ortaya cikar, ates etmeye baslar.</summary>
    public void TriggerBossScene()
    {
        if (bossActive) return;
        bossActive = true;
        bossFireTimer = bossFireInterval * 0.5f; // ilk atesten once kisa bir nefes payi
        if (bossObject != null) bossObject.SetActive(true);
    }

    // ============================================================
    //  Kopegin olumu
    // ============================================================

    IEnumerator DogDeathRoutine()
    {
        // Faz loglari: sahne "tetiklendi ama olmadi" diye bildirildiginde
        // hangi adimda koptugunu tek oyun turunda gosterirler.
        Debug.Log("[Final] Olum 1/5 — basladi, kontrol alindi.");

        SetPlayerControl(false);

        // ⚠️ SES BURADA KESILMIYOR — carpmadan SONRA kesiliyor.
        // PLAN.md "3 saniye tum sesler kesilir" diyor; o sessizlik carpmanin
        // ARDINDAN gelen bosluk. Bastan kisarsak tahtanin dusmesi sessiz olur
        // ve sahnenin en sert ani kaybolur. Gurultulu carpma -> ani mutlak
        // sessizlik, tersinden cok daha guclu.
        float previousVolume = AudioListener.volume;

        // ⚠️ SAHNENIN TAMAMI BU NOKTA UZERINE KURULU.
        // Tahta, oyuncunun TAM DURDUGU yere duser. Karabas oraya kosar,
        // oyuncu savrulur. Anlatilan cumle: "sana gelecek darbeyi o aldi."
        // Sabit bir olum noktasi kullanirsak bu cumle kurulmaz — oyuncu
        // sadece "kendiliginden kaydim, kopek uzakta oldu" gorur.
        Vector3 impactPoint = player != null ? player.position : transform.position;

        // Itme yonunu SIMDI hesapla: kopek birazdan hareket edecek, sonra
        // hesaplarsak yon yanlis cikar.
        float pushDir = 1f;
        if (dog != null && player != null)
            pushDir = player.position.x >= dog.transform.position.x ? 1f : -1f;

        // Bantlar ONCE kapansin: sahne "basliyor" sinyali agir cekimden once
        // gelmeli, yoksa oyuncu once yavasligi fark edip "takildi mi?" diyor.
        if (useCinematicBars) CinematicBars.Show(barHeightFraction, barFadeDuration);

        // AGIR CEKIM burada basliyor — kopek hareket etmeden hemen once.
        // Sahnenin tamamini yavaslatmiyoruz: sadece "itilis + tahtanin dusmesi"
        // penceresi. Sessizlik normal hizda yasanmali, yoksa 3 saniye 8 saniye
        // olur ve an dagilir. Kamera sabit (KAT_PLANI: zoom/kesme yok).
        if (cinematicDeath) StartSlowMotion();

        // 1) TEHDIT GORUNUR OLUR — kiris oyuncunun tam ustunde titrer ve akkor
        //    olur. Oyuncu "bu bana geliyor" der ama tusa basamaz; kacis yok,
        //    dolayisiyla suclanacak bir hata da yok.
        if (cinematicDeath && revealThreat && deathDebris != null)
            yield return RevealThreatRoutine(impactPoint);

        // 2) KARABAS HAVLAR — tek, keskin, ve zaten gec.
        //    Uyariyi CEVRE degil KOPEK veriyor: "senin fark etmedigini o fark etti."
        if (cinematicDeath && barkBeforeLunge) BarkOnce();

        // Havlamayla ayni anda: "DIKKAT ET!". Uyariyi cevre degil kopek veriyor.
        if (useSpeech && dog != null)
            SpeechBubble.Say(dog.transform, lineDogWarn, lineDuration, dogLineColor);

        if (dog != null)
        {
            dog.enabled = false;                 // takip mantigi hamleyle yarismasin
            PutDogInFrontOfPlayer();             // ust uste binerse "onunde" okunsun
        }

        // ---- A) YAKLASMA ----
        // Kopek once oyuncunun YANINA gelir, uzerine degil. Bu faz olmadan
        // kopek dogrudan oyuncunun durdugu noktaya lerp'leniyordu ve iki sprite
        // birkac kare ic ice giriyordu: "itti" degil "icine girdi" gorunuyordu.
        if (cinematicDeath && dog != null)
            yield return DogApproachRoutine(impactPoint, pushDir);

        // ---- B) CARPMA ----
        // Kopek sicrayarak oyuncunun yerine girer, oyuncu AYNI ANDA savrulur.
        // Ikisi es zamanli olmali ("once kopek yurudu, sonra oyuncu kaydi"
        // gorunmesin), ama kopek artik YANDAN basladigi icin oyuncu bosalttikca
        // yerine geciyor — hicbir karede ust uste binmiyorlar.
        Coroutine lunge = (dog != null) ? StartCoroutine(DogLungeRoutine(impactPoint)) : null;

        yield return PushPlayerAside(pushDir);
        if (lunge != null) yield return lunge;

        Debug.Log("[Final] Olum 2/5 — kopek oyuncunun yerine gecti, oyuncu savruldu.");

        if (deathDebris != null)
        {
            // Tahtayi carpma noktasinin USTUNE tasi — kopek artik orada.
            Vector3 d = deathDebris.transform.position;
            deathDebris.transform.position = new Vector3(impactPoint.x, d.y, d.z);

            // ⚠️ DropNow(false): UYARI YOK.
            // Oyun boyunca "unlem gorursen kac" ogretiliyor. Olum aninda unlem
            // cikarsa oyuncu "kacabilirdim, benim hatam" der; duygu suclulukdan
            // ofkeye doner ve final coker. Kacilamaz olmasi KASITLI.
            deathDebris.autoTrigger = false;   // oyuncu yaklasinca erken dusmesin
            deathDebris.DropNow(false);

            yield return new WaitForSeconds(debrisImpactDelay);

            if (crashClip != null)
                AudioSource.PlayClipAtPoint(crashClip, impactPoint, crashVolume);

            // Carpma anini bedene hissettir. FallingDebris kendi sarsintisini
            // zaten yapiyor; bu onun ustune binen, daha agir ve kisa bir vurus.
            if (CameraShake.Instance != null)
                CameraShake.Instance.Shake(impactShakeDuration, impactShakeMagnitude);

            // ⭐ KOPEK TAHTANIN ALTINDA — VE BU GORULMELI.
            //
            // Kopek zaten ~1 saniye ekranda kaliyordu, ama tahtayla ayni
            // duzlemde duruyordu: "altinda mi kaldi, yaninda mi duruyor" belli
            // olmuyordu. Iki dokunus netlestiriyor:
            //   1. Tahtanin sprite'i kopegin ONUNE aliniyor (sorting order)
            //   2. Kopek dikeyde eziliyor — kan yok, sadece okunabilirlik
            if (cinematicDeath && dog != null)
            {
                PinDogUnderDebris();

                // Ham tepki, carpmanin hemen ustune. Oyuncu daha ne oldugunu
                // anlamadi — once sok, keder sonra. Sirayi ters kurarsak
                // sahne "aciklama" gibi okunuyor, "yasanan sey" gibi degil.
                if (useSpeech && player != null)
                    SpeechBubble.Say(player, linePlayerShock, lineDuration, playerLineColor);

                yield return new WaitForSecondsRealtime(dogPinnedDuration);
            }

            // Agir cekim BITIYOR — sessizlik normal hizda yasansin.
            if (cinematicDeath) StopSlowMotion();

            // Carpma duyuldu — SIMDI sessizlik. Bu bosluk sahnenin kendisi.
            yield return new WaitForSeconds(silenceStartDelay);
        }
        else if (bigFlameVisual != null)
        {
            // Yedek yol: sahnede tahta atanmadiysa eski buyuyen alev oynar.
            // Boylece tahta baglanmadan da sahne test edilebilir.
            if (dogDeathPoint != null) bigFlameVisual.transform.position = dogDeathPoint.position;
            bigFlameVisual.SetActive(true);
            yield return ScaleRoutine(bigFlameVisual.transform, Vector3.zero, bigFlameBaseScale, flameRiseDuration);
        }

        // Iki daldan biri de calismamis olabilir (ne tahta ne alev bagli).
        // Zaman olcegi ASLA acik kalmamali — oyun kalici olarak yavaslar.
        if (cinematicDeath) StopSlowMotion();

        Debug.Log("[Final] Olum 3/5 — tahta dustu, carpma duyuldu.");

        // Carpma sesi duyuldu, simdi mutlak sessizlik (PLAN.md: 3 saniye).
        AudioListener.volume = 0f;

        // Kopek gorunmez olur — kanli/grafik degil, sessiz ve yumusak (PLAN.md).
        // Die() sprite'i aninda gizler + isDead bayragini set eder; boylece
        // coroutine yield'leri arasinda kopek tekrar gorulmuyor.
        if (dog != null)
        {
            dog.Die();
            dog.gameObject.SetActive(false);
            Debug.Log("[Final] Olum 4/5 — Karabas gizlendi.");
        }
        else
        {
            Debug.LogError("[Final] Olum 4/5 ATLANDI — `dog` bos, kopek ekranda kaldi!");
        }

        yield return new WaitForSeconds(silenceDuration);

        if (deathDebris == null && bigFlameVisual != null)
        {
            yield return ScaleRoutine(bigFlameVisual.transform, bigFlameBaseScale, Vector3.zero, flameFallDuration);
            bigFlameVisual.SetActive(false);

            // Sonraki acilista sifir boyutta kalmasin.
            bigFlameVisual.transform.localScale = bigFlameBaseScale;
        }

        // "Yerde sadece tasma kalir." — kopegin oldugu yerde, sabit bir
        // noktada degil. Tasma nerede kaldiysa oyuncu oraya bakar.
        if (collarPickup != null)
        {
            Vector3 collarPos = dogLastPosition;
            if (dog == null && dogDeathPoint != null) collarPos = dogDeathPoint.position;

            collarPickup.transform.position = collarPos;
            collarPickup.SetActive(true);
        }

        AudioListener.volume = previousVolume;

        // Oyuncunun ustunde: kaybin dile gelmesi. Ses geri geldikten SONRA —
        // sessizligin icine yazi koymak o bosluğu bozardi.
        if (useSpeech && player != null)
        {
            SpeechBubble.Say(player, lineAfterDeath, lineDuration, playerLineColor);
            yield return new WaitForSecondsRealtime(lineDuration * 0.6f);
        }

        // ---- EV SARSILIR: "burada kalamazsin" ----
        //
        // Kederi bir eyleme baglamak lazim, yoksa oyuncu tasmanin basinda
        // durup ne yapacagini bilemiyor. Ev sarsiliyor, tavandan toz iniyor:
        // soylenmeden anlasiliyor — kos.
        if (houseTremorDuration > 0f && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(houseTremorDuration, houseTremorMagnitude);

            if (useSpeech && player != null)
                SpeechBubble.Say(player, lineMustEscape, lineDuration, playerLineColor);

            yield return new WaitForSecondsRealtime(houseTremorDuration * 0.8f);
        }

        if (useCinematicBars) CinematicBars.Hide(barFadeDuration);
        SetPlayerControl(true);

        Debug.Log("[Final] Olum 5/5 — tasma birakildi, kontrol geri verildi.");
    }

    /// <summary>
    /// Oyuncuyu kopekten UZAGA, yana kaydirir.
    ///
    /// Olcek animasyonu degil duz pozisyon lerp'i — "itildim" hissi
    /// dogrudan konum degisiminden geliyor.
    /// </summary>
    /// <summary>
    /// Karabas oyuncunun durdugu yere atilir. Tahta oraya dusecek.
    ///
    /// DogCompanion'i kapatiyoruz cunku kendi takip mantigi ayni transform'u
    /// oynatiyor ve hamleyle yarisip kopegi titretiyor.
    /// </summary>
    /// <summary>
    /// A FAZI — kopek oyuncunun YANINA kosar, uzerine degil.
    /// `dogContactGap` kadar beride durur; temas ancak B fazinda olur.
    /// </summary>
    IEnumerator DogApproachRoutine(Vector3 impactPoint, float pushDir)
    {
        Transform t = dog.transform;
        Vector3 from = t.position;

        // Oyuncunun itilecegi yonun TERSINDE dur — yani kopek arkada kalsin,
        // itis yonu dogal gorunsun.
        Vector3 to = new Vector3(impactPoint.x - pushDir * dogContactGap, from.y, from.z);

        SpriteRenderer sr = dog.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = to.x < from.x;

        float e = 0f;
        while (e < dogApproachDuration)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / dogApproachDuration);
            k = 1f - (1f - k) * (1f - k);        // EaseOut — hizli cikis, yumusak varis
            t.position = Vector3.Lerp(from, to, k);
            yield return null;
        }

        t.position = to;
    }

    /// <summary>
    /// Kopegi oyuncunun ONUNE cizdirir. Iki sprite bir kare bile ust uste
    /// binerse "icine girdi" gibi okunuyordu; onde olmasi "carpti" yapiyor.
    /// </summary>
    void PutDogInFrontOfPlayer()
    {
        SpriteRenderer dogSprite = dog.GetComponentInChildren<SpriteRenderer>();
        if (dogSprite == null || player == null) return;

        SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();
        if (playerSprite == null) return;

        if (dogSprite.sortingLayerID == playerSprite.sortingLayerID)
            dogSprite.sortingOrder = playerSprite.sortingOrder + 1;
    }

    IEnumerator DogLungeRoutine(Vector3 impactPoint)
    {
        if (dog == null) yield break;

        Transform t = dog.transform;
        Vector3 from = t.position;
        // Sadece X: kopek zemininde kalsin, oyuncu havadaysa ucmasin.
        Vector3 to = new Vector3(impactPoint.x, from.y, from.z);

        SpriteRenderer sr = dog.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = to.x < from.x;

        // Sinematik modda daha uzun: serilestirilmis 0.25 sn goz yakalamadan
        // bitiyordu ve "kopek onume gecti" cumlesi hic kurulmuyordu.
        float duration = cinematicDeath ? cinematicLungeDuration : dogLungeDuration;

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / duration);
            k = k * k * (3f - 2f * k);          // SmoothStep — atilis hissi

            Vector3 p = Vector3.Lerp(from, to, k);

            // SICRAMA KAVISI. Duz kaymak "yurudu" gibi okunuyordu; kavis
            // hareketi "atildi" yapiyor ve kopegin oyuncunun uzerinden degil
            // USTUNDEN gelip onu ittigi anlasiliyor.
            if (cinematicDeath)
                p.y += Mathf.Sin(k * Mathf.PI) * lungeHopHeight;

            t.position = p;
            yield return null;
        }

        t.position = to;
        dogLastPosition = to;                   // tasma buraya dusecek
    }

    // ============================================================
    //  Sinematik yardimcilari
    // ============================================================

    float defaultFixedDelta = 0.02f;
    bool slowMotionActive;

    void StartSlowMotion()
    {
        if (slowMotionActive || slowMotionScale >= 1f) return;

        slowMotionActive = true;
        defaultFixedDelta = Time.fixedDeltaTime;
        Time.timeScale = slowMotionScale;

        // Fizik adimi da olceklenmeli, yoksa agir cekimde hareket kekeliyor.
        Time.fixedDeltaTime = defaultFixedDelta * slowMotionScale;
    }

    void StopSlowMotion()
    {
        if (!slowMotionActive) return;

        slowMotionActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
    }

    /// <summary>
    /// ⚠️ GUVENLIK AGI. Coroutine yarida kesilirse (sahne degisimi, obje
    /// kapanmasi, hata) zaman olcegi 0.35'te kalir ve oyunun TAMAMI kalici
    /// olarak agir cekime duser. Bu, sahneyi kirmaktan cok daha beter.
    /// </summary>
    void OnDisable()
    {
        StopSlowMotion();

        // Bantlar da acik kalmasin — ekranin ustu altı siyah kalmis bir oyun
        // "bozuldu" gibi gorunur.
        CinematicBars.HideImmediate();
    }

    /// <summary>
    /// Kiris, dusecegi noktanin — yani oyuncunun basinin — ustunde titreyip
    /// akkor hale gelir. Amac tehdidi ACIKLAMAK, uyarmak degil: kontrol zaten
    /// alinmis durumda. Unlem ve yer isareti bilerek kullanilmiyor, onlar
    /// oyunun "kac" dili ve burada kacis yok.
    /// </summary>
    IEnumerator RevealThreatRoutine(Vector3 impactPoint)
    {
        // Kirisi oyuncunun tam ustune al — dusecegi yer burasi.
        Vector3 d = deathDebris.transform.position;
        Vector3 basePos = new Vector3(impactPoint.x, d.y, d.z);
        deathDebris.transform.position = basePos;

        SpriteRenderer sr = deathDebris.GetComponentInChildren<SpriteRenderer>();
        Color baseColor = sr != null ? sr.color : Color.white;

        // Tavandan gelen boguk bir titresim — oyuncuyu yukari baktirir.
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(threatRevealDuration, threatShakeMagnitude);

        float e = 0f;
        while (e < threatRevealDuration)
        {
            // Agir cekim acik; gercek zaman kullaniyoruz ki bu beat timeScale
            // ne olursa olsun ayni uzunlukta kalsin.
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / threatRevealDuration);

            // Titreme sona dogru siddetlenir: "kopmak uzere".
            float amp = 0.05f + 0.12f * k;
            deathDebris.transform.position = basePos +
                new Vector3(Mathf.Sin(e * 55f) * amp, 0f, 0f);

            if (sr != null)
                sr.color = Color.Lerp(baseColor, threatGlowColor,
                                      Mathf.PingPong(e * 6f, 1f) * 0.85f);

            yield return null;
        }

        deathDebris.transform.position = basePos;
        if (sr != null) sr.color = baseColor;
    }

    /// <summary>
    /// Hayaletin havlamasi. Kopek o sirada SetActive(false) — yani kendi
    /// AudioSource'u calamaz. Bu yuzden PlayClipAtPoint kullaniyoruz;
    /// klip referansi kapali objede de gecerli kaliyor.
    /// </summary>
    void PlayGhostBark()
    {
        AudioClip clip = ghostBarkClip;
        if (clip == null && dog != null) clip = dog.happyBarkClip;
        if (clip == null) return;

        Vector3 at = player != null ? player.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, at, ghostBarkVolume);
    }

    /// <summary>Karabas'in son sesi: tek, keskin havlama.</summary>
    void BarkOnce()
    {
        if (dog == null || dog.audioSource == null || dog.happyBarkClip == null) return;

        // Normalden biraz pes: sevinc degil alarm.
        dog.audioSource.pitch = 0.85f;
        dog.audioSource.PlayOneShot(dog.happyBarkClip, deathBarkVolume);
    }

    /// <summary>
    /// Kopegi tahtanin ALTINDA gosterir: tahta sprite'i one alinir, kopek
    /// dikeyde ezilir. Kan yok — sadece mekanik okunabilirlik.
    /// </summary>
    void PinDogUnderDebris()
    {
        SpriteRenderer dogSprite = dog.GetComponentInChildren<SpriteRenderer>();
        if (dogSprite == null) return;

        if (deathDebris != null)
        {
            SpriteRenderer debrisSprite = deathDebris.GetComponentInChildren<SpriteRenderer>();
            if (debrisSprite != null && debrisSprite.sortingLayerID == dogSprite.sortingLayerID)
                debrisSprite.sortingOrder = dogSprite.sortingOrder + 1;
        }

        Vector3 s = dog.transform.localScale;
        dog.transform.localScale = new Vector3(s.x, s.y * dogSquashY, s.z);
    }

    IEnumerator PushPlayerAside(float dir)
    {
        if (player == null || pushDistance <= 0f || pushDuration <= 0f) yield break;

        float targetX = player.position.x + dir * pushDistance;

        // Kisi A'nin ForceMoveTo'su: SmoothStep'li ve sadece X'i oynatiyor,
        // dikey hizi fizige birakiyor. Kendi elle lerp'imiz oyuncu havadayken
        // yercekimini de iptal ediyordu — bu daha dogru.
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            yield return controller.ForceMoveTo(targetX, pushDuration);
            yield break;
        }

        // Yedek: PlayerController yoksa (test kuklasi) duz lerp.
        Vector3 from = player.position;
        float t = 0f;
        while (t < pushDuration)
        {
            t += Time.deltaTime;
            Vector3 p = player.position;
            p.x = Mathf.Lerp(from.x, targetX, t / pushDuration);
            player.position = p;
            yield return null;
        }
    }

    // ============================================================
    //  Boss sahnesi
    // ============================================================

    void BossUpdate()
    {
        bossFireTimer -= Time.deltaTime;
        if (bossFireTimer <= 0f)
        {
            ShootAtPlayer();
            bossFireTimer = bossFireInterval;
        }

        // Oyuncunun cani 1 kalbe dusunce hayalet sahnesi kendiliginden
        // tetiklenir (PLAN.md madde 4).
        //
        // Playtest'te can 1'e dustu ama hayalet gelmedi ve oyuncu oldu; sebep
        // log'dan cikmiyordu cunku bu blok sessizdi. Artik konusuyor.
        if (!TryGetPlayerHealth(out int current))
        {
            if (!bossHealthUnreadable)
            {
                bossHealthUnreadable = true;
                Debug.LogError("[Final] Boss: oyuncunun cani OKUNAMIYOR (player veya " +
                               "PlayerHealth bulunamadi) — hayalet sahnesi ASLA tetiklenemez.");
            }
            return;
        }

        if (current != lastBossHealth)
        {
            lastBossHealth = current;
            Debug.Log($"[Final] Boss: oyuncu cani {current} (hayalet esigi <= 1).");
        }

        if (current <= 1)
        {
            bossSceneEnding = true;
            Debug.Log("[Final] Hayalet 1/2 — esik asildi, sahne basliyor.");
            StartCoroutine(GhostRoutine());
        }
    }

    void ShootAtPlayer()
    {
        if (flamePrefab == null || bossFirePoint == null || player == null) return;

        GameObject flame = Instantiate(flamePrefab, bossFirePoint.position, Quaternion.identity);
        FlameProjectile projectile = flame.GetComponent<FlameProjectile>();
        if (projectile != null)
        {
            float dir = player.position.x >= bossFirePoint.position.x ? 1f : -1f;
            projectile.Launch(new Vector2(dir, 0f));
        }
    }

    IEnumerator GhostRoutine()
    {
        SetPlayerControl(false);

        if (useCinematicBars) CinematicBars.Show(barHeightFraction, barFadeDuration);

        // Boss artik direnmeyi biraksin: yenilenmesi dursun ve olcegi serbest
        // kalsin. Yoksa BossCore ile FadeOutBoss ayni localScale'i cekistirir.
        if (bossObject != null)
        {
            BossCore core = bossObject.GetComponentInChildren<BossCore>();
            if (core != null) core.Defeat();
        }

        // ---- "HER SEY BITTI" ANI ----
        //
        // Bantlar kapandi, oyuncunun cani 1, boss hala ayakta. Burada HICBIR
        // SEY olmuyor: ne muzik, ne hareket. Oyuncu kaybettigini saniyor.
        //
        // Bu bosluk kurtarilmanin bedeli. Hayalet dogrudan gelirse "sahne
        // devam etti" olur; once umut tamamen bitmeli ki gelisi bir sey ifade
        // etsin. Bosluk ne kadar rahatsiz edici olursa havlama o kadar guclu.
        float previousVolume = AudioListener.volume;
        AudioListener.volume = 0f;
        yield return new WaitForSecondsRealtime(despairSilenceDuration);

        // ---- HAVLAMA ----
        // Sessizligi yirtan tek ses. Oyuncu daha hicbir sey gormeden, sadece
        // duyarak kim geldigini anliyor — goruntuden once TANIMA geliyor.
        AudioListener.volume = previousVolume;
        PlayGhostBark();

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.45f, 0.35f);

        yield return new WaitForSecondsRealtime(ghostBarkLead);

        if (collarPickup != null) yield return CollarGlowRoutine();

        if (ghostVisual != null && ghostStart != null && ghostEnd != null)
        {
            ghostVisual.SetActive(true);
            ghostVisual.transform.position = ghostStart.position;

            if (ghostClip != null)
                AudioSource.PlayClipAtPoint(ghostClip, ghostStart.position, ghostVolume);

            SpriteRenderer ghostSprite = ghostVisual.GetComponentInChildren<SpriteRenderer>();
            float ghostAlpha = ghostSprite != null ? ghostSprite.color.a : 1f;

            // Hayalet artik gecip GITMIYOR: yolun ortasinda duruyor, konusuyor,
            // sonra veda edip gidiyor. Oyuncunun en caresiz aninda gelip tek
            // kelime etmeden kaybolmasi "olay" gibi okunuyordu; durup konusmasi
            // onu bir KARSILASMA yapiyor.
            bool spokeArrive = false;
            float t = 0f;
            while (t < ghostCrossDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / ghostCrossDuration);

                ghostVisual.transform.position =
                    Vector3.Lerp(ghostStart.position, ghostEnd.position, k);

                // Yolun ortasinda dur ve konus.
                if (!spokeArrive && k >= ghostPauseAt)
                {
                    spokeArrive = true;

                    if (useSpeech)
                    {
                        SpeechBubble.Say(ghostVisual.transform, lineGhostArrive,
                                         lineDuration, dogLineColor);
                        yield return new WaitForSecondsRealtime(ghostPauseDuration * 0.55f);

                        SpeechBubble.Say(ghostVisual.transform, lineGhostFarewell,
                                         lineDuration, dogLineColor);
                        yield return new WaitForSecondsRealtime(ghostPauseDuration * 0.45f);
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(ghostPauseDuration);
                    }
                }

                // Hayalet aniden belirip aniden kaybolmasin: iki ucta da
                // suzulerek girsin ciksin. Bir hayaletin "beliriyor" olmasi
                // gerekiyor, "acilip kapaniyor" degil.
                if (ghostSprite != null)
                {
                    float fade = Mathf.Min(Mathf.InverseLerp(0f, 0.25f, k),
                                           Mathf.InverseLerp(1f, 0.75f, k));
                    Color c = ghostSprite.color;
                    c.a = ghostAlpha * fade;
                    ghostSprite.color = c;
                }

                yield return null;
            }

            ghostVisual.SetActive(false);

            // Sonraki acilista gorunur olsun.
            if (ghostSprite != null)
            {
                Color c = ghostSprite.color;
                c.a = ghostAlpha;
                ghostSprite.color = c;
            }
        }

        // Boss sonuyor: yok olmadan once kucululup soluyor, aniden
        // kaybolmasi "bug" gibi duruyordu.
        if (bossObject != null) yield return FadeOutBoss();
        bossActive = false;

        if (exitDoor != null) exitDoor.SetActive(false); // kapi acilir

        // Boss sondu, cikis acildi — son soz oyuncunun.
        if (useSpeech && player != null)
        {
            SpeechBubble.Say(player, lineFinalThanks, lineDuration, playerLineColor);
            yield return new WaitForSecondsRealtime(lineDuration * 0.7f);
        }

        if (useCinematicBars) CinematicBars.Hide(barFadeDuration);
        SetPlayerControl(true);
        bossResolved = true;

        Debug.Log("[Final] Hayalet 2/2 — boss sondu, cikis acildi.");
    }

    /// <summary>
    /// Boss'un sonusu. Karabas'in hayaleti gecti, dev alev artik yenildi —
    /// bu anin bir agirligi olmali, obje aniden kaybolmamali.
    /// </summary>
    IEnumerator FadeOutBoss()
    {
        SpriteRenderer bossSprite = bossObject.GetComponentInChildren<SpriteRenderer>();
        Vector3 startScale = bossObject.transform.localScale;
        Color startColor = bossSprite != null ? bossSprite.color : Color.white;

        float t = 0f;
        while (t < bossFadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / bossFadeDuration);

            // Yatayda kapanip dikeyde cokuyor — sonen bir alev gibi.
            bossObject.transform.localScale = new Vector3(
                Mathf.Lerp(startScale.x, startScale.x * 0.15f, k),
                Mathf.Lerp(startScale.y, startScale.y * 0.05f, k),
                startScale.z);

            if (bossSprite != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, k);
                bossSprite.color = c;
            }

            yield return null;
        }

        bossObject.SetActive(false);

        // Tekrar test edilebilsin diye eski haline getir.
        bossObject.transform.localScale = startScale;
        if (bossSprite != null) bossSprite.color = startColor;
    }

    IEnumerator CollarGlowRoutine()
    {
        SpriteRenderer sprite = collarPickup.GetComponentInChildren<SpriteRenderer>();
        if (sprite == null) yield break;

        Color original = sprite.color;
        float t = 0f;
        float duration = 1.2f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.PingPong(t * 4f, 1f);
            sprite.color = Color.Lerp(original, collarGlowColor, k);
            yield return null;
        }

        sprite.color = original;
    }

    // ============================================================
    //  Yardimcilar
    // ============================================================

    // ScaleRoutine bu dosyanin sonunda — Kisi A yazdi (SmoothStep'li surum).

    void SetPlayerControl(bool canControl)
    {
        if (player == null) return;
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null) controller.CanControl = canControl;
    }

    /// <summary>
    /// PlayerHealth.CurrentHealth'i derleme bagimliligi kurmadan okur.
    ///
    /// NEDEN reflection? Sozlesmede "CurrentHealth { get; } // final sahnesi
    /// bunu okur" yaziyor — yani bu okuma tasarim geregi. Ama CurrentHealth bir
    /// DEGER DONDURUYOR; FlameProjectile.cs'teki SendMessage cozumu (cagri
    /// yapar, sonuc almaz) burada yetmiyor. GetComponent(string) + reflection,
    /// sinif yokken sessizce false doner, sinif gelince tek satir degismeden
    /// calisir. Once kendi test ikizimize (TestPlayerDummy) bakar.
    /// </summary>
    bool TryGetPlayerHealth(out int current)
    {
        current = 0;
        if (player == null) return false;

        TestPlayerDummy testDummy = player.GetComponent<TestPlayerDummy>();
        if (testDummy != null)
        {
            current = testDummy.CurrentHealth;
            return true;
        }

        Component healthComponent = player.GetComponent("PlayerHealth");
        if (healthComponent == null) return false;

        System.Reflection.PropertyInfo prop = healthComponent.GetType().GetProperty("CurrentHealth");
        if (prop == null) return false;

        current = (int)prop.GetValue(healthComponent);
        return true;
    }

    // ============================================================
    //  Yardimci
    //  NOT (Kisi A ekledi): Bu metod cagriliyordu ama yazilmamisti,
    //  proje derlenmiyordu ve ikimiz de Play'e basamiyorduk. Eksigi
    //  tamamladim. Farkli calismasini istersen serbestce degistir.
    // ============================================================

    /// <summary>
    /// Bir Transform'un olcegini verilen surede 'from'dan 'to'ya yumusak gecisle tasir.
    /// Alevin yukselmesi ve sonmesi icin kullaniliyor.
    /// </summary>
    IEnumerator ScaleRoutine(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null) yield break;

        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        target.localScale = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // SmoothStep: basta ve sonda yavas, ortada hizli. Lineerden cok daha iyi durur.
            t = t * t * (3f - 2f * t);
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.localScale = to;
    }
}
