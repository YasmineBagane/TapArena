using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapArena.MemoryMatch
{
    [Serializable]
    public class RoundConfig
    {
        [Min(1)] public int columns = 4;
        [Min(1)] public int rows = 3;

        [Tooltip("Generous for round 1 per GDD 5.6; tighten for later rounds.")]
        public float timeBudgetSeconds = 45f;

        public int PairCount => (columns * rows) / 2;
    }

    /// <summary>
    /// Data-driven round table (GDD 5.6 tech note). MVP ships one entry —
    /// the 4x3 / 6-pair board — so adding round 2+ later is just adding
    /// rows to this list, no code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "MemoryMatchRoundTable", menuName = "TapArena/MemoryMatch/Round Config Table")]
    public class RoundConfigTableSO : ScriptableObject
    {
        public List<RoundConfig> rounds = new List<RoundConfig>
        {
            new RoundConfig { columns = 4, rows = 3, timeBudgetSeconds = 45f }
        };
    }
}
