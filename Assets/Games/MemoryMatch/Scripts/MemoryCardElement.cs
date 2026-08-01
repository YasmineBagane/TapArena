using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TapArena.MemoryMatch
{
    public enum CardVisualState { FaceDown, FaceUp, Matched }

    /// <summary>
    /// A single tappable card. Only reacts to pointer-down while face-down —
    /// that one rule is what makes a rapid double-tap on the same card a
    /// natural no-op (GDD 5.6 edge case), no extra bookkeeping needed.
    /// </summary>
    public class MemoryCardElement : VisualElement
    {
        public new class UxmlFactory : UxmlElementAttribute { }

        private const string UssCard = "match-card";
        private const string UssFaceDown = "match-card--face-down";
        private const string UssFaceUp = "match-card--face-up";
        private const string UssMatched = "match-card--matched";

        private readonly VisualElement _faceElement;

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

            if (face.icon != null)
                _faceElement.style.backgroundImage = new StyleBackground(face.icon);
            else
                _faceElement.style.backgroundColor = new StyleColor(face.placeholderColor);

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
            RemoveFromClassList(UssMatched);

            switch (newState)
            {
                case CardVisualState.FaceDown:
                    AddToClassList(UssFaceDown);
                    break;
                case CardVisualState.FaceUp:
                    AddToClassList(UssFaceUp);
                    break;
                case CardVisualState.Matched:
                    AddToClassList(UssMatched);
                    break;
            }
        }
    }
}
