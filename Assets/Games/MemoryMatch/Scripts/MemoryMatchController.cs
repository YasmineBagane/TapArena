using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TapArena.Core;

namespace TapArena.MemoryMatch
{
    [RequireComponent(typeof(UIDocument))]
    public class MemoryMatchController : MonoBehaviour, IMinigame
    {
        [Header("Data")]
        [SerializeField] private CardIconSetSO iconSet;
        [SerializeField] private RoundConfigTableSO roundTable;

        [Header("Tuning")]
        [Tooltip("How long a non-matching pair stays face-up before flipping back.")]
        [SerializeField] private float mismatchDelaySeconds = 0.8f;
        [Tooltip("Base score awarded per fully-cleared round, before the flip-efficiency bonus.")]
        [SerializeField] private int perRoundBaseScore = 100;
        [Tooltip("Max bonus points a round can add for flipping at (or near) the theoretical minimum.")]
        [SerializeField] private int maxEfficiencyBonus = 50;

        [Header("UXML element names")]
        [SerializeField] private string gridRootName = "grid-root";
        [SerializeField] private string timerLabelName = "timer-label";
        [SerializeField] private string roundLabelName = "round-label";

        [Header("Testing")]
        [Tooltip("Temporary: calls StartRun() on Play so this game is testable standalone before the hub exists. Turn off once MinigameRunController drives this.")]
        [SerializeField] private bool autoStartOnPlay = true;

        public event Action<RunResult> OnRunEnded;

        private enum RunState { Idle, Playing, Resolving, Ended }

        private UIDocument _document;
        private VisualElement _gridRoot;
        private Label _timerLabel;
        private Label _roundLabel;

        private readonly List<MemoryCardElement> _cards = new List<MemoryCardElement>();
        private readonly List<MemoryCardElement> _flippedThisTurn = new List<MemoryCardElement>();

        private RunState _state = RunState.Idle;
        private int _roundIndex;
        private int _pairsMatchedThisRound;
        private int _flipsThisRound;
        private int _totalScore;
        private float _timeRemaining;
        private float _runElapsed;
        private Coroutine _resolveRoutine;

        // TODO: replace with Core PB storage / Unity Cloud Save once the shared module lands (SRS §7.2, §10).
        private const string PbKey = "MemoryMatch_PB";

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _gridRoot = root.Q<VisualElement>(gridRootName);
            _timerLabel = root.Q<Label>(timerLabelName);
            _roundLabel = root.Q<Label>(roundLabelName);

            if (_gridRoot == null)
                Debug.LogError($"MemoryMatchController: could not find '{gridRootName}' in the UXML tree.");
        }

        private void Start()
        {
            if (autoStartOnPlay) StartRun();
        }

        public void StartRun()
        {
            if (iconSet == null || roundTable == null || roundTable.rounds.Count == 0)
            {
                Debug.LogError("MemoryMatchController: missing CardIconSet or RoundConfigTable.");
                return;
            }

            _roundIndex = 0;
            _totalScore = 0;
            _runElapsed = 0f;
            BuildRound(_roundIndex);
        }

        public void AbortRun()
        {
            _state = RunState.Idle;
            if (_resolveRoutine != null) StopCoroutine(_resolveRoutine);
            ClearGrid();
        }

        private void Update()
        {
            if (_state != RunState.Playing && _state != RunState.Resolving) return;

            _runElapsed += Time.deltaTime;
            _timeRemaining -= Time.deltaTime;

            if (_timerLabel != null)
                _timerLabel.text = Mathf.Max(0f, _timeRemaining).ToString("0.0");

            if (_timeRemaining <= 0f)
                EndRun();
        }

        // ---------- Round setup ----------

        private void BuildRound(int roundIndex)
        {
            ClearGrid();

            RoundConfig config = roundTable.rounds[roundIndex];
            int pairCount = config.PairCount;

            if (iconSet.faces.Count < pairCount)
            {
                Debug.LogError($"MemoryMatchController: icon set has {iconSet.faces.Count} faces but round {roundIndex} needs {pairCount}.");
                return;
            }

            List<int> pairIds = BuildShuffledPairIds(config.columns, config.rows);

            for (int row = 0; row < config.rows; row++)
            {
                var rowElement = new VisualElement();
                rowElement.AddToClassList("match-row");
                _gridRoot.Add(rowElement);

                for (int col = 0; col < config.columns; col++)
                {
                    int cardIndex = row * config.columns + col;
                    int pairId = pairIds[cardIndex];

                    var card = new MemoryCardElement();
                    card.Init(cardIndex, pairId, iconSet.faces[pairId]);
                    card.Tapped += OnCardTapped;

                    rowElement.Add(card);
                    _cards.Add(card);
                }
            }

            _pairsMatchedThisRound = 0;
            _flipsThisRound = 0;
            _flippedThisTurn.Clear();
            _timeRemaining = config.timeBudgetSeconds;
            _state = RunState.Playing;

            if (_roundLabel != null)
                _roundLabel.text = $"Round {roundIndex + 1}";
        }

        private List<int> BuildShuffledPairIds(int columns, int rows)
        {
            int pairCount = (columns * rows) / 2;
            var ids = new List<int>(columns * rows);
            for (int i = 0; i < pairCount; i++)
            {
                ids.Add(i);
                ids.Add(i);
            }

            // Fisher-Yates shuffle.
            for (int i = ids.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }

            return ids;
        }

        private void ClearGrid()
        {
            foreach (var card in _cards)
                card.Tapped -= OnCardTapped;

            _cards.Clear();
            _gridRoot?.Clear();
        }

        // ---------- Input / match logic ----------

        private void OnCardTapped(MemoryCardElement card)
        {
            // While a mismatch is resolving, a tap on a third card is simply
            // ignored (not queued) — the consistent choice called out as an
            // open edge case in GDD 5.6.
            if (_state != RunState.Playing) return;

            card.SetState(CardVisualState.FaceUp);
            _flippedThisTurn.Add(card);
            _flipsThisRound++;

            if (_flippedThisTurn.Count < 2) return;

            _state = RunState.Resolving;
            var a = _flippedThisTurn[0];
            var b = _flippedThisTurn[1];

            if (a.PairId == b.PairId)
            {
                a.SetState(CardVisualState.Matched);
                b.SetState(CardVisualState.Matched);
                _flippedThisTurn.Clear();
                _pairsMatchedThisRound++;
                _state = RunState.Playing;

                if (_pairsMatchedThisRound * 2 == _cards.Count)
                    OnRoundCleared();
            }
            else
            {
                _resolveRoutine = StartCoroutine(ResolveMismatch(a, b));
            }
        }

        private IEnumerator ResolveMismatch(MemoryCardElement a, MemoryCardElement b)
        {
            yield return new WaitForSeconds(mismatchDelaySeconds);

            // Timeout may have ended the round while we were waiting — don't
            // touch cards from a round transition that's already happened.
            if (_state == RunState.Ended)
            {
                _resolveRoutine = null;
                yield break;
            }

            a.SetState(CardVisualState.FaceDown);
            b.SetState(CardVisualState.FaceDown);
            _flippedThisTurn.Clear();
            _state = RunState.Playing;
            _resolveRoutine = null;
        }

        private void OnRoundCleared()
        {
            int theoreticalMin = _pairsMatchedThisRound * 2;
            float efficiency = Mathf.Clamp01(theoreticalMin / (float)Mathf.Max(_flipsThisRound, 1));
            int roundScore = perRoundBaseScore + Mathf.RoundToInt(efficiency * maxEfficiencyBonus);
            _totalScore += roundScore;

            int nextRound = _roundIndex + 1;
            if (nextRound < roundTable.rounds.Count)
            {
                _roundIndex = nextRound;
                BuildRound(_roundIndex);
            }
            else
            {
                EndRun();
            }
        }

        // ---------- Run end ----------

        private void EndRun()
        {
            _state = RunState.Ended;
            if (_resolveRoutine != null) StopCoroutine(_resolveRoutine);

            int previousBest = PlayerPrefs.GetInt(PbKey, 0);
            bool isPb = _totalScore > previousBest;
            if (isPb) PlayerPrefs.SetInt(PbKey, _totalScore);

            var result = new RunResult(_totalScore, _runElapsed, isPb);
            OnRunEnded?.Invoke(result);
        }
    }
}
