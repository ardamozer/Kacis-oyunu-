using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// KOPEK ANIMASYONLARINI BAGLA — menuden: ALEV → Kopek Animasyonlarini Bagla
///
/// Dilimlenmis sayfalardaki 54 kareyi Karabas.prefab'daki DogAnimator'a
/// dagitir. Elle surukleyerek yapmak 20 dakika surer ve tek bir karenin
/// sirasi kaysa animasyon takirdar.
///
/// ⚠️ KARELERIN SIRALAMASI SAYISAL OLMALI.
/// Alt-asset adlari "Saint-Bernard-Idle_0" ... "_9". Alfabetik siralamada
/// _10, _2'den ONCE gelir; 10 kareli Idle ve stretching bundan etkilenir
/// ve kopek tokezleyerek yurur. Sondaki sayiyi ayristirip ona gore
/// siraliyoruz.
///
/// Onkosul: ALEV → Kopek Sayfalarini Dilimle calistirilmis olmali.
/// </summary>
public static class AlevDogAnimatorBinder
{
    const string Folder     = "Assets/Art/Imported/Dog";
    const string PrefabPath = "Assets/Prefabs/Dog/Karabas.prefab";

    [MenuItem("ALEV/Kopek Animasyonlarini Bagla", false, 4)]
    public static void Bind()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            EditorUtility.DisplayDialog("ALEV", $"{PrefabPath} bulunamadi.", "Tamam");
            return;
        }

        var anim = root.GetComponent<DogAnimator>();
        if (anim == null) anim = root.AddComponent<DogAnimator>();

        var log = new System.Text.StringBuilder();

        // fps degerleri: yurume oyuncunun adimina yakin, kosu belirgin
        // daha hizli, yatma/nefes yavas.
        anim.idle      = Make("Saint-Bernard-Idle",       8f,  true,  log);
        anim.walk      = Make("Saint-Bernard-walk",      10f,  true,  log);
        anim.run       = Make("Saint-Bernard-run",       14f,  true,  log);
        anim.bark      = Make("Saint-Bernard-bark",       8f,  true,  log);
        anim.lyingDown = Make("Saint-Bernard-lying-down", 6f,  true,  log);
        anim.sitting   = Make("Saint-Bernard-sitting",    0f,  false, log);
        anim.licking   = Make("Saint-Bernard-licking1",  10f,  true,  log);
        anim.dead      = Make("Saint-Bernard-sleeping",   0f,  false, log);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();

        Debug.Log($"<b>[ALEV Kopek Animasyonu]</b> Karabas.prefab guncellendi.{log}");
    }

    static DogAnimator.Clip Make(string sheet, float fps, bool loop,
                                 System.Text.StringBuilder log)
    {
        string path = $"{Folder}/{sheet}.png";
        Sprite[] frames = LoadFrames(path);

        if (frames.Length == 0)
            log.Append($"\n  ✖ {sheet} — kare bulunamadi. Once 'Kopek Sayfalarini Dilimle'.");
        else
            log.Append($"\n  ✔ {sheet} — {frames.Length} kare, {fps} fps" + (loop ? "" : ", donugusuz"));

        return new DogAnimator.Clip { frames = frames, fps = fps, loop = loop };
    }

    static Sprite[] LoadFrames(string pngPath)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>().ToList();
        if (sprites.Count == 0) return new Sprite[0];

        // "..._12" → 12. Sayi yoksa sona atilir.
        return sprites
            .OrderBy(s =>
            {
                int i = s.name.LastIndexOf('_');
                if (i >= 0 && int.TryParse(s.name.Substring(i + 1), out int n)) return n;
                return int.MaxValue;
            })
            .ToArray();
    }
}
