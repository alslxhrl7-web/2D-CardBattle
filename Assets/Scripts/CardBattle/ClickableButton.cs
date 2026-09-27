using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 화면 버튼의 공통 동작(마우스를 올리면/누르면 색이 바뀌고, 클릭하면 OnClick 호출).
    /// uGUI Canvas 없이 카드와 같은 방식(SpriteRenderer + BoxCollider2D + OnMouse* 메시지)으로 동작한다.
    ///
    /// 새 버튼을 만들 때는 이 클래스를 상속해서 OnClick()만 쓰면 된다. 예시:
    ///
    ///     public class MyButton : ClickableButton
    ///     {
    ///         public CardManager manager;                                                         // 인스펙터에서 연결
    ///         protected override ButtonColors Colors { get { return GamePalette.GreenButton; } }  // 색 (생략 가능)
    ///         protected override void OnClick() { manager.EndTurn(); }                            // 눌렀을 때 할 일
    ///     }
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class ClickableButton : MonoBehaviour
    {
        [Tooltip("상태에 따라 색이 바뀔 배경 스프라이트")]
        public SpriteRenderer background; // 버튼 배경 (색이 바뀌는 대상)

        /// <summary>이 버튼의 색 조합. 하위 클래스에서 GamePalette의 스타일 중 하나를 고른다(기본: 회청색).</summary>
        protected virtual ButtonColors Colors { get { return GamePalette.NeutralButton; } }

        /// <summary>버튼을 눌렀다 뗐을 때 실행할 동작. 하위 클래스가 반드시 작성해야 한다.</summary>
        protected abstract void OnClick();

        /// <summary>마우스가 버튼 위로 올라왔을 때.</summary>
        protected virtual void OnMouseEnter() { SetColor(Colors.hover); }
        /// <summary>마우스가 버튼 밖으로 나갔을 때.</summary>
        protected virtual void OnMouseExit() { SetColor(Colors.normal); }
        /// <summary>버튼을 누르기 시작했을 때.</summary>
        protected virtual void OnMouseDown() { SetColor(Colors.press); }

        /// <summary>버튼에서 손을 뗐을 때 = 클릭 완료.</summary>
        protected virtual void OnMouseUp()
        {
            SetColor(Colors.hover);
            GameAudio.Play(GameAudio.Click);
            OnClick();
        }

        /// <summary>배경 색을 바꾼다(배경이 연결 안 돼 있으면 무시).</summary>
        void SetColor(Color color)
        {
            if (background != null) background.color = color;
        }
    }
}
