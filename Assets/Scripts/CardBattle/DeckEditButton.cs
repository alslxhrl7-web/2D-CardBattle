using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// "덱 편집" 버튼. EndTurnButton과 동일한 방식(SpriteRenderer + BoxCollider2D + OnMouse*)으로
    /// 클릭을 감지해서 DeckBuilderUI.TogglePanel()을 호출해 덱 빌더 패널을 열고 닫는다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DeckEditButton : MonoBehaviour
    {
        public DeckBuilderUI builder;
        public SpriteRenderer background;

        static readonly Color NormalColor = new Color(0.30f, 0.32f, 0.40f);
        static readonly Color HoverColor = new Color(0.42f, 0.45f, 0.56f);
        static readonly Color PressColor = new Color(0.20f, 0.21f, 0.27f);

        void OnMouseEnter() { if (background != null) background.color = HoverColor; }
        void OnMouseExit()  { if (background != null) background.color = NormalColor; }
        void OnMouseDown()  { if (background != null) background.color = PressColor; }
        void OnMouseUp()
        {
            if (background != null) background.color = HoverColor;
            if (builder != null) builder.TogglePanel();
        }
    }
}
