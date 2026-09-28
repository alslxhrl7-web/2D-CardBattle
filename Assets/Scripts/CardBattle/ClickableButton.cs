using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 화면 버튼의 공통 동작(마우스를 올리면/누르면 색이 바뀌고, 클릭하면 OnClick 호출).
    /// uGUI Canvas 없이 카드와 같은 방식(SpriteRenderer + BoxCollider2D + OnMouse* 메시지)으로 동작한다.
    /// 글씨가 배경보다 길면 배경과 클릭 범위를 글씨에 맞게 가로로 늘린다 (아래 LateUpdate).
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

        // ================= 배경 폭을 글씨에 맞추기 =================

        const float LabelPadding = 0.5f; // 글씨 양옆 여백을 합친 폭 (월드 단위). 배경 폭 = 글씨 폭 + 이 값

        TextMesh label;      // 버튼 글씨 (자식에서 찾는다)
        string fittedText;   // 마지막으로 폭을 맞춘 글씨. 글씨가 바뀌면 다시 맞춘다
        float designedWidth; // 씬에 놓인 원래 배경 폭. 이보다 좁게는 줄이지 않는다

        /// <summary>글씨가 바뀌었으면 배경 폭을 다시 맞춘다. (실행 중에 글씨가 바뀌는 버튼도 있어서 매 프레임 확인)</summary>
        protected virtual void LateUpdate()
        {
            if (label == null) label = GetComponentInChildren<TextMesh>();
            if (label == null || background == null || label.text == fittedText) return;
            if (FitBackgroundToLabel()) fittedText = label.text;
        }

        /// <summary>
        /// 배경을 "글씨 폭 + 여백"만큼 가로로 늘린다 (원래 폭보다 좁게는 안 함). 클릭 범위(BoxCollider2D)도 배경에 맞춘다.
        /// 글씨가 배경의 자식이면 배경을 늘릴 때 글씨도 같이 늘어나므로, 글씨는 그만큼 되돌려서 모양을 그대로 둔다.
        /// 글씨 크기를 아직 알 수 없으면 false (다음 프레임에 다시 시도).
        /// </summary>
        bool FitBackgroundToLabel()
        {
            var textRenderer = label.GetComponent<Renderer>();
            float textWidth = textRenderer != null ? textRenderer.bounds.size.x : 0f;
            float currentWidth = background.bounds.size.x;
            if (textWidth <= 0f || currentWidth <= 0f) return false;
            if (designedWidth <= 0f) designedWidth = currentWidth;

            float newWidth = Mathf.Max(designedWidth, textWidth + LabelPadding);
            float stretch = newWidth / currentWidth;
            if (Mathf.Abs(stretch - 1f) < 0.001f) return true; // 이미 맞음 (글씨가 원래 배경 안에 들어가는 버튼은 그대로)

            var bgScale = background.transform.localScale;
            background.transform.localScale = new Vector3(bgScale.x * stretch, bgScale.y, bgScale.z);
            if (label.transform.IsChildOf(background.transform))
            {
                var labelScale = label.transform.localScale;
                label.transform.localScale = new Vector3(labelScale.x / stretch, labelScale.y, labelScale.z);
            }

            var box = GetComponent<BoxCollider2D>();
            if (box != null) box.size = new Vector2(newWidth / transform.lossyScale.x, box.size.y);
            return true;
        }
    }
}
