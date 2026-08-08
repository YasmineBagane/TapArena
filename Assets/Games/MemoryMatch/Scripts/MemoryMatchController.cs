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
        [Tooltip("All cards start face-up showing their true color for this long before flipping to gray.")]
        [SerializeField] private float introRevealSeconds = 5f;
        [Tooltip("How long a matched pair stays visible before being hidden.")]
        [SerializeField] private float matchDisplaySeconds = 0.4f;
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
        [SerializeField] private string endScreenName = "end-screen";
        [SerializeField] private string endTitleName = "end-title";
        [SerializeField] private string endScoreName = "end-score";
        [SerializeField] private string endTimeName = "end-time";
        [SerializeField] private string endPbName = "end-pb";
        [SerializeField] private string retryButtonName = "retry-button";

        [Header("Testing")]
        [Tooltip("Temporary: calls StartRun() on Play so this game is testable standalone before the hub exists. Turn off once MinigameRunController drives this.")]
        [SerializeField] private bool autoStartOnPlay = true;

        public event Action<RunResult> OnRunEnded;

        // Intro: all cards face-up, not tappable, "get ready" countdown showing.
        // Playing: cards face-down/gray, tappable.
        // Resolving: two cards face-up, waiting to confirm match/mismatch.
        // Ended: run over, end screen showing.
        private enum RunState { Idle, Intro, Playing, Resolving, Ended }

        private UIDocument _document;
        private VisualElement _gridRoot;
        private Label _timerLabel;
        private Label _roundLabel;
        private VisualElement _endScreen;
        private Label _endTitleLabel;
        private Label _endScoreLabel;
        private Label _endTimeLabel;
        private Label _endPbLabel;
        private Button _retryButton;

        private readonly List<MemoryCardElement> _cards = new List<MemoryCardElement>();
        private readonly List<MemoryCardElement> _flippedThisTurn = new List<MemoryCardElement>();

        private RunState _state = RunState.Idle;
        private int _roundIndex;
        private int _totalCardsThisRound;
        private int _pairsMatchedThisRound;
        private int _flipsThisRound;
        private int _totalScore;
        private float _timeRemaining;
        private float _introTimeRemaining;
        private float _runElapsed;
        private Coroutine _introRoutine;
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
            _endScreen = root.Q<VisualElement>(endScreenName);
            _endTitleLabel = root.Q<Label>(endTitleName);
            _endScoreLabel = root.Q<Label>(endScoreName);
            _endTimeLabel = root.Q<Label>(endTimeName);
            _endPbLabel = root.Q<Label>(endPbName);
            _retryButton = root.Q<Button>(retryButtonName);

            if (_gridRoot == null)
                Debug.LogError($"MemoryMatchController: could not find '{gridRootName}' in the UXML tree.");
            if (_endScreen == null)
                Debug.LogError($"MemoryMatchController: could not find '{endScreenName}' in the UXML tree.");

            if (_endScreen != null)
                _endScreen.style.display = DisplayStyle.None;

            if (_retryButton != null)
                _retryButton.clicked += StartRun;
        }

        private void OnDisable()
        {
            if (_retryButton != null)
                _retryButton.clicked -= StartRun;
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

            if (_endScreen != null)
                _endScreen.style.display = DisplayStyle.None;

            _roundIndex = 0;
            _totalScore = 0;
            _runElapsed = 0f;
            BuildRound(_roundIndex);
        }

        public void AbortRun()
        {
            _state = RunState.Idle;
            if (_introRoutine != null) StopCoroutine(_introRoutine);
            if (_resolveRoutine != null) StopCoroutine(_resolveRoutine);
            ClearGrid();
        }

        private void Update()
        {
            if (_state == RunState.Intro)
            {
                _introTimeRemaining -= Time.deltaTime;
                if (_timerLabel != null)
                    _timerLabel.text = $"Get ready: {Mathf.Max(0f, _introTimeRemaining):0.0}";
                return;
            }

            if (_state != RunState.Playing && _state != RunState.Resolving) return;

            _runElapsed += Time.deltaTime;
            _timeRemaining -= Time.deltaTime;

            if (_timerLabel != null)
                _timerLabel.text = Mathf.Max(0f, _timeRemaining).ToString("0.0");

            if (_timeRemaining <= 0f)
                EndRun(success: false);
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

            _totalCardsThisRound = config.columns * config.rows;
            _pairsMatchedThisRound = 0;
            _flipsThisRound = 0;
            _flippedThisTurn.Clear();
            _timeRemaining = config.timeBudgetSeconds;
            _introTimeRemaining = introRevealSeconds;

            if (_roundLabel != null)
                _roundLabel.text = "Memorize!";

            // Reveal-then-hide intro: show every face, then flip to gray.
            // The round timer doesn't tick during Intro, so the time budget
            // starts fresh once cards actually go gray.
            _state = RunState.Intro;
            foreach (var card in _cards)
                card.SetState(CardVisualState.FaceUp);

            _introRoutine = StartCoroutine(IntroReveal());
        }

        private IEnumerator IntroReveal()
        {
            while (_introTimeRemaining > 0f)
                yield return null;

            foreach (var card in _cards)
                card.SetState(CardVisualState.FaceDown);

            _state = RunState.Playing;
            if (_roundLabel != null)
                _roundLabel.text = $"Round {_roundIndex + 1}";
            _introRoutine = null;
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
            // Ignored during Intro (cards aren't face-down yet, so this
            // won't fire anyway) and while a mismatch/match is resolving —
            // a tap on a third card is simply ignored, not queued.
            if (_state != RunState.Playing) return;

            card.SetState(CardVisualState.FaceUp);
            _flippedThisTurn.Add(card);
            _flipsThisRound++;

            if (_flippedThisTurn.Count < 2) return;

            _state = RunState.Resolving;
            var a = _flippedThisTurn[0];
            var b = _flippedThisTurn[1];

            if (a.PairId == b.PairId)
                _resolveRoutine = StartCoroutine(ResolveMatch(a, b));
            else
                _resolveRoutine = StartCoroutine(ResolveMismatch(a, b));
        }

        private IEnumerator ResolveMatch(MemoryCardElement a, MemoryCardElement b)
        {
            yield return new WaitForSeconds(matchDisplaySeconds);

            if (_state == RunState.Ended)
            {
                _resolveRoutine = null;
                yield break;
            }

            // Empty hides the card but keeps its grid slot occupied — the
            // 4x3 layout never reflows.
            a.Tapped -= OnCardTapped;
            b.Tapped -= OnCardTapped;
            a.SetState(CardVisualState.Empty);
            b.SetState(CardVisualState.Empty);

            _flippedThisTurn.Clear();
            _pairsMatchedThisRound++;
            _state = RunState.Playing;
            _resolveRoutine = null;

            if (_pairsMatchedThisRound * 2 == _totalCardsThisRound)
                OnRoundCleared();
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
                EndRun(success: true);
            }
        }

        // ---------- Run end ----------

        private void EndRun(bool success)
        {
            _state = RunState.Ended;
            if (_introRoutine != null) StopCoroutine(_introRoutine);
            if (_resolveRoutine != null) StopCoroutine(_resolveRoutine);

            int previousBest = PlayerPrefs.GetInt(PbKey, 0);
            bool isPb = _totalScore > previousBest;
            if (isPb) PlayerPrefs.SetInt(PbKey, _totalScore);

            ShowEndScreen(success, isPb);

            var result = new RunResult(_totalScore, _runElapsed, isPb);
            OnRunEnded?.Invoke(result);
        }

        private void ShowEndScreen(bool success, bool isPb)
        {
            if (_endScreen == null) return;

            if (_endTitleLabel != null)
                _endTitleLabel.text = success ? "Cleared!" : "Time's Up!";

            if (_endScoreLabel != null)
                _endScoreLabel.text = $"Score: {_totalScore}";

            if (_endTimeLabel != null)
                _endTimeLabel.text = $"Time: {_runElapsed:0.0}s";

            if (_endPbLabel != null)
                _endPbLabel.style.display = isPb ? DisplayStyle.Flex : DisplayStyle.None;

            _endScreen.style.display = DisplayStyle.Flex;
        }
    }
}