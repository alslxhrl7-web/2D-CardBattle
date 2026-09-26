using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 화면 버튼의 공통 동작(마우스를 올리면/누르면 색 바뀜 + 클릭 시 OnClick 호출).
    /// uGUI Canvas 없이 카드와 같은 방식(SpriteRenderer + BoxCollider2D + OnMouse* 메시지)으로 동작한다.
    ///
    /// 새 버튼을 만들 때는 이 클래스를 상속해서 OnClick()만 쓰면 된다:
    ///
    ///     public class MyButton : ClickableButton
    ///     {
    ///         public CardManager manager;
    ///         protected override ButtonColors Colors { get { return GamePalette.GreenButton; } }  // 색(선택)
    ///         protected override void OnClick() { manager.EndTurn(); }
    ///     }
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class ClickableButton : MonoBehaviour
    {
        [Tooltip("상태에 따라 색이 바뀔 배경 스프라이트")]
        public SpriteRenderer background;

        /// <summary>이 버튼의 색 조합. 하위 클래스에서 GamePalette의 스타일 중 하나를 고른다.</summary>
        protected virtual ButtonColors Colors { get { return GamePalette.NeutralButton; } }

        /// <summary>버튼을 눌렀다 뗐을 때 실행할 동작.</summary>
        protected abstract void OnClick();

        protected virtual void OnMouseEnter() { SetColor(Colors.hover); }
        protected virtual void OnMouseExit() { SetColor(Colors.normal); }
        protected virtual void OnMouseDown() { SetColor(Colors.press); }

        protected virtual void OnMouseUp()
        {
            SetColor(Colors.hover);
            OnClick();
        }

        void SetColor(Color color)
        {
            if (background != null) background.color = color;
        }
    }
}
