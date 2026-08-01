using System.Collections.Generic;
using UnityEngine;

namespace TapArena.MemoryMatch
{
    /// <summary>
    /// A swappable pack of card faces (e.g. default set, seasonal cosmetic
    /// set). Must contain at least as many faces as the largest round's
    /// pair count — for MVP Round 1 that's 6.
    /// </summary>
    [CreateAssetMenu(fileName = "CardIconSet", menuName = "TapArena/MemoryMatch/Card Icon Set")]
    public class CardIconSetSO : ScriptableObject
    {
        public List<CardFace> faces = new List<CardFace>();
    }
}
