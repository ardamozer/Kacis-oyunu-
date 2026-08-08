using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncunun topladigi anahtarlar. Player objesine takilir.
///
/// Anahtarlar HARCANMAZ — kapiyi acinca cebinizde kalir. Harcamak
/// hicbir sey katmaz, sadece "yanlis kapiya harcadim" korkusu yaratir.
/// </summary>
public class KeyInventory : MonoBehaviour
{
    readonly HashSet<KeyColor> held = new HashSet<KeyColor>();

    /// <summary>UI'in okumasi icin.</summary>
    public bool Has(KeyColor k) => held.Contains(k);

    public int Count => held.Count;

    public void Add(KeyColor k)
    {
        if (held.Add(k)) Debug.Log($"[Anahtar] {k} anahtar alindi.");
    }
}
