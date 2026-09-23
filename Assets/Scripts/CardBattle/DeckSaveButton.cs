using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 덱 빌더 패널 안의 "저장" 버튼. EndTurnButton과 동일한 방식으로 클릭을 감지해서
    /// DeckBuilderUI.SaveDeck()을 호출한다. 최소 매수 미달 시에는 SaveDeck() 내부에서 조용히
    /// 아무 일도 하지 않으므로(패널의 상태 문구가 이미 경고를 보여주고 있음), 이 버튼 자체는
    /// 항상 눌러볼 수 있게 둔다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DeckSaveButton : MonoBehaviour
    {
        public DeckBuilderUI builder;
        public SpriteRenderer background;

        static readonly Color NormalColor = new Color(0.14f, 0.42f, 0.20f);
        static readonly Color HoverColor = new Color(0.20f, 0.58f, 0.28f);
        static readonly Color PressColor = new Color(0.09f, 0.28f, 0.13f);

        void OnMouseEnter() { if (background != null) background.color = HoverColor; }
        void OnMouseExit()  { if (background != null) background.color = NormalColor; }
        void OnMouseDown()  { if (background != null) background.color = PressColor; }
        void OnMouseUp()
        {
            if (background != null) background.color = HoverColor;
            if (builder != null) builder.SaveDeck();
        }
    }
}
