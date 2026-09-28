using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 장수(인조·홍타이지) 판: 초상화 + 이름 + 영웅 능력 글자. 누르면 onClick을 부른다.
    /// 배틀 화면 왼쪽에서는 영웅 능력 버튼으로, 진영 고르기 화면에서는 고르는 버튼으로 쓴다. CardManager가 실행 중에 만든다.
    /// </summary>
    public class HeroBadge : ClickableButton
    {
        static readonly Vector2 Size = new Vector2(2.2f, 3.0f); // 판 크기 (월드 유닛)
        const float PortraitHeight = 1.8f;                       // 초상화 높이
        static readonly Color PowerReady = new Color(1f, 0.85f, 0.4f);  // 영웅 능력을 쓸 수 있을 때 글자색
        static readonly Color PowerUsed = new Color(0.55f, 0.55f, 0.55f); // 못 쓸 때

        public System.Action onClick;   // 눌렀을 때 할 일
        public System.Func<bool> ready; // 지금 영웅 능력을 쓸 수 있는지 (없으면 항상 밝게)
        SpriteRenderer portrait;
        TextMesh nameText, powerText;

        /// <summary>판을 만든다. style = 글꼴을 복제할 글자, order = 그리기 순서(판 → 그림 → 글자 순으로 +1씩).</summary>
        public static HeroBadge Create(string name, Vector3 position, Transform parent, TextMesh style, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().size = Size;
            var badge = go.AddComponent<HeroBadge>();
            badge.background = HudFactory.EnsureBackdrop(go.transform, "Backdrop", Size, GamePalette.NeutralButton.normal, order);

            var pic = new GameObject("Portrait");
            pic.transform.SetParent(go.transform, false);
            pic.transform.localPosition = new Vector3(0f, 0.5f, -0.05f);
            badge.portrait = pic.AddComponent<SpriteRenderer>();
            badge.portrait.sortingOrder = order + 1;

            badge.nameText = badge.Label(style, "Name", -0.62f, 0.26f, true, order + 2);
            badge.powerText = badge.Label(style, "Power", -1.1f, 0.17f, false, order + 2);
            return badge;
        }

        /// <summary>장수 카드(그림·이름)와 영웅 능력 설명을 넣는다.</summary>
        public void Show(CardData hero, string title, string power)
        {
            nameText.text = title;
            powerText.text = power;
            portrait.sprite = hero != null ? hero.portrait : null;
            if (portrait.sprite != null) portrait.transform.localScale = Vector3.one * (PortraitHeight / portrait.sprite.bounds.size.y);
        }

        /// <summary>매 프레임: 영웅 능력을 쓸 수 있으면 글자를 밝게, 아니면 어둡게.</summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (powerText != null) powerText.color = ready == null || ready() ? PowerReady : PowerUsed;
        }

        protected override void OnClick()
        {
            if (onClick != null) onClick();
        }

        /// <summary>판 안에 가운데 정렬 글자를 하나 만든다.</summary>
        TextMesh Label(TextMesh style, string name, float y, float height, bool bold, int order)
        {
            var text = HudFactory.CreateLabel(style, name, transform.position + new Vector3(0f, y, -0.1f));
            text.transform.SetParent(transform, true);
            UnityUtil.SetTextHeight(text, height, bold);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = GamePalette.InfoText;
            text.GetComponent<MeshRenderer>().sortingOrder = order;
            return text;
        }
    }
}
