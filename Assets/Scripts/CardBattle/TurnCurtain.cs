using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 2인 대전에서 차례가 바뀔 때 화면 전체를 덮는 가림막. "청 차례입니다" 문구가 뜨고,
    /// 다음 사람이 화면을 클릭하면 걷히면서 그 사람의 손패가 앞면으로 보인다.
    /// CardManager가 실행 중에 만든다(씬에 놓을 필요 없음).
    /// </summary>
    public class TurnCurtain : ClickableButton
    {
        static readonly Vector2 Size = new Vector2(40f, 25f); // 화면(약 23×13)보다 넉넉하게
        const float TextHeight = 0.55f;  // 문구 글자 높이
        const int SortingOrder = 32600;  // 카드·피해 숫자보다 위, 승패 배너(32700)보다 아래
        const float Depth = -5f;         // 카메라 쪽으로 당겨서 클릭을 카드보다 먼저 받는다

        public CardManager manager; // 가림막이 걷혔다고 알려줄 곳
        TextMesh label;             // "청 차례입니다 …"

        /// <summary>지금 화면을 덮고 있는지.</summary>
        public bool IsShown { get { return gameObject.activeSelf; } }

        /// <summary>거의 불투명한 남색.</summary>
        protected override ButtonColors Colors { get { return GamePalette.CurtainButton; } }

        /// <summary>가림막을 만든다(처음엔 숨겨 둠). textStyle = 글꼴을 복제할 글자(군력 표시).</summary>
        public static TurnCurtain Create(CardManager manager, TextMesh textStyle, Transform parent)
        {
            var go = new GameObject("TurnCurtain");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 0f, Depth);
            go.AddComponent<BoxCollider2D>().size = Size;

            var curtain = go.AddComponent<TurnCurtain>();
            curtain.manager = manager;
            curtain.background = HudFactory.EnsureBackdrop(go.transform, "Backdrop", Size, GamePalette.CurtainButton.normal, SortingOrder);

            curtain.label = HudFactory.CreateLabel(textStyle, "Label", go.transform.position);
            curtain.label.transform.SetParent(go.transform, true);
            UnityUtil.SetTextHeight(curtain.label, TextHeight, true);
            curtain.label.anchor = TextAnchor.MiddleCenter;
            curtain.label.alignment = TextAlignment.Center;
            curtain.label.color = GamePalette.InfoText;
            curtain.label.GetComponent<MeshRenderer>().sortingOrder = SortingOrder + 1;

            go.SetActive(false);
            return curtain;
        }

        /// <summary>문구를 바꿔서 화면을 덮는다.</summary>
        public void Show(string text)
        {
            label.text = text;
            background.color = Colors.normal;
            gameObject.SetActive(true);
        }

        /// <summary>가림막을 걷는다.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>클릭 = 다음 사람이 준비됨 → 걷고 그 사람의 손패를 보여준다.</summary>
        protected override void OnClick()
        {
            Hide();
            manager.OnCurtainClosed();
        }
    }
}
