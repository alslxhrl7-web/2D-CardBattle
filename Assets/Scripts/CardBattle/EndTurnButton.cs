using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 화면에 배치되는 "턴 종료" 버튼. 손패 카드를 원하는 만큼 낸 뒤 이 버튼을 눌러 턴을 넘긴다.
    /// 별도의 uGUI Canvas/EventSystem 없이, 카드와 똑같은 방식(SpriteRenderer + BoxCollider2D +
    /// OnMouse* 메시지)으로 클릭을 감지해서 CardManager.EndTurn()을 호출한다.
    /// 마우스가 올라오거나 누르는 동안 배경색을 바꿔 클릭 가능한 버튼임을 시각적으로 알려준다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class EndTurnButton : MonoBehaviour
    {
        public CardManager manager;
        public SpriteRenderer background;

        static readonly Color NormalColor = new Color(0.55f, 0.42f, 0.12f);
        static readonly Color HoverColor = new Color(0.75f, 0.58f, 0.20f);
        static readonly Color PressColor = new Color(0.35f, 0.26f, 0.07f);

        void OnMouseEnter()
        {
            if (background != null) background.color = HoverColor;
        }

        void OnMouseExit()
        {
            if (background != null) background.color = NormalColor;
        }

        void OnMouseDown()
        {
            if (background != null) background.color = PressColor;
        }

        void OnMouseUp()
        {
            if (background != null) background.color = HoverColor;
            if (manager != null) manager.EndTurn();
        }
    }
}
