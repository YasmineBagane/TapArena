using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TapArena.MemoryMatch
{
    public enum CardVisualState { FaceDown, FaceUp, Empty }

    /// <summary>
    /// A single tappable card. Only reacts to pointer-down while face-down —
    /// that one rule is what makes a rapid double-tap on the same card a
    /// natural no-op (GDD 5.6 edge case), no extra bookkeeping needed.
    ///
    /// `Empty` (a matched, cleared pair) hides the card via visibility
    /// rather than removing it from the layout tree, so its grid slot stays
    /// occupied and the board never reflows.
    /// </summary>
    [UxmlElement]
    public partial class MemoryCardElement : VisualElement
    {
        private const string UssCard = "match-card";
        private const string UssFaceDown = "match-card--face-down";
        private const string UssFaceUp = "match-card--face-up";

        // Neutral "hidden" color — doesn't need to be colorblind-distinct
        // since it conveys no information, unlike the face colors do.
        private static readonly StyleColor FaceDownColor = new StyleColor(new Color(0.55f, 0.55f, 0.62f));

        private readonly VisualElement _faceElement;
        private CardFace _face;

        public int CardIndex { get; private set; }
        public int PairId { get; private set; }
        public CardVisualState State { get; private set; } = CardVisualState.FaceDown;

        public event Action<MemoryCardElement> Tapped;

        public MemoryCardElement()
        {
            AddToClassList(UssCard);
            AddToClassList(UssFaceDown);

            _faceElement = new VisualElement { name = "card-face" };
            _faceElement.AddToClassList("match-card__face");
            Add(_faceElement);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        public void Init(int cardIndex, int pairId, CardFace face)
        {
            CardIndex = cardIndex;
            PairId = pairId;
            _face = face;

            SetState(CardVisualState.FaceDown);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (State != CardVisualState.FaceDown) return;
            Tapped?.Invoke(this);
        }

        public void SetState(CardVisualState newState)
        {
            State = newState;
            RemoveFromClassList(UssFaceDown);
            RemoveFromClassList(UssFaceUp);

            if (newState == CardVisualState.Empty)
            {
                // Hidden but still laid out — the grid slot is preserved,
                // the board doesn't reflow when a pair is cleared.
                style.visibility = Visibility.Hidden;
                pickingMode = PickingMode.Ignore;
                return;
            }

            style.visibility = Visibility.Visible;
            pickingMode = PickingMode.Position;

            if (newState == CardVisualState.FaceDown)
            {
                AddToClassList(UssFaceDown);
                _faceElement.style.backgroundImage = StyleKeyword.Null;
                _faceElement.style.backgroundColor = FaceDownColor;
            }
            else // FaceUp
            {
                AddToClassList(UssFaceUp);
                if (_face.icon != null)
                {
                    _faceElement.style.backgroundImage = new StyleBackground(_face.icon);
                }
                else
                {
                    _faceElement.style.backgroundImage = StyleKeyword.Null;
                    _faceElement.style.backgroundColor = new StyleColor(_face.placeholderColor);
                }
            }
        }
    }
}