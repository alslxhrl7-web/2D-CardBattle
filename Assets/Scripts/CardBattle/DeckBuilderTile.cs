using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 덱 빌더 화면에서 로스터 카드 한 장을 나타내는 타일(같은 오브젝트의 CardView로 카드 모양을 보여준다).
    /// 클릭할 때마다 덱에 넣을 매수가 0 → 1 → ... → maxCount → 0 으로 돈다.
    /// maxCount는 패널이 열릴 때 DeckBuilderUI가 GameRules.MaxCopies(희귀도)로 매번 다시 맞춰준다.
    /// </summary>
    public class DeckBuilderTile : MonoBehaviour
    {
        public CardData card;        // 이 타일이 나타내는 카드
        public int count;            // 덱에 넣을 매수
        public int maxCount = 3;     // 최대 매수 (패널이 열릴 때 규칙대로 다시 설정됨)
        public DeckBuilderUI ui;     // 매수가 바뀌었다고 알릴 덱 빌더
        public TextMesh countBadge;  // 우상단 "xN" 표시

        /// <summary>타일을 클릭했을 때: 매수를 하나 올린다(최대치를 넘으면 0으로).</summary>
        void OnMouseUp()
        {
            if (ui == null) return;
            SetCount((count + 1) % (maxCount + 1));
            ui.OnTileCountChanged();
        }

        /// <summary>매수를 바꾸고(0~maxCount로 제한) 배지를 갱신한다.</summary>
        public void SetCount(int value)
        {
            count = Mathf.Clamp(value, 0, maxCount);
            UpdateBadge();
        }

        /// <summary>배지 글자를 현재 매수에 맞춘다. 0장이면 배지를 비운다.</summary>
        public void UpdateBadge()
        {
            UnityUtil.SetText(countBadge, count > 0 ? string.Format(GameTexts.TileCountBadge, count) : "");
        }
    }
}
