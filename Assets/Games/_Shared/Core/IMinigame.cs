using System;

namespace TapArena.Core
{
    /// <summary>
    /// Result payload every minigame reports back to the shared Core
    /// (ScoreService / PB storage / End-of-Run overlay) when a run ends.
    /// Matches the RunResult contract referenced in the launch GDDs.
    /// </summary>
    [Serializable]
    public struct RunResult
    {
        public int Score;
        public float TimeSeconds;
        public bool IsPersonalBest;

        public RunResult(int score, float timeSeconds, bool isPersonalBest)
        {
            Score = score;
            TimeSeconds = timeSeconds;
            IsPersonalBest = isPersonalBest;
        }
    }

    /// <summary>
    /// Contract every launch/roadmap minigame implements so it plugs into the
    /// shared MinigameRunController without a bespoke integration per game.
    /// </summary>
    public interface IMinigame
    {
        /// <summary>Raised exactly once, when the run ends (success or fail).</summary>
        event Action<RunResult> OnRunEnded;

        /// <summary>Starts (or restarts) a fresh run from round/state zero.</summary>
        void StartRun();

        /// <summary>Hard-stops the current run without emitting OnRunEnded (e.g. hub navigation away).</summary>
        void AbortRun();
    }
}
