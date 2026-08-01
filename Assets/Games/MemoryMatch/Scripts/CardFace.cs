using System;
using UnityEngine;

namespace TapArena.MemoryMatch
{
    /// <summary>
    /// One unique card face. Leave `icon` unassigned to fall back to
    /// `placeholderColor` — lets us ship the board today and drop in real
    /// art later with zero code changes (GDD 5.6 tech note: data-driven faces).
    /// </summary>
    [Serializable]
    public class CardFace
    {
        public string faceId;
        public Sprite icon;
        public Color placeholderColor = Color.white;
    }
}
