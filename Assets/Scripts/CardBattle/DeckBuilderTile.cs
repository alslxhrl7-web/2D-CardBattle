using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 덱 빌더 화면에서 로스터의 카드 한 장을 나타내는 타일.
    /// CardView(같은 오브젝트에 붙어있음)로 카드 모습을 그대로 보여주고, 클릭할 때마다
    /// "이 카드를 덱에 몇 장 넣을지"(count)를 0부터 maxCount까지 순환시키며,
    /// 우상단 배지(countBadge)에 "xN" 형태로 표시한다.
    /// 이 타일은 CardManager.SpawnCardToHand를 거치지 않고 DeckBuilderUI가 직접 인스턴스화하므로
    /// CardDragHandler.Init이 호출되지 않아(manager==null) 드래그 동작은 자동으로 비활성 상태가 된다.
    /// </summary>
    public class DeckBuilderTile : MonoBehaviour
    {
        public CardData card;
        public int count;
        public int maxCount = 3;   // 희귀도별 최대 매수(DeckBuilderUI가 타일 생성 시 결정해서 넣어줌)
        public DeckBuilderUI ui;
        public TextMesh countBadge;

        void OnMouseUp()
        {
            if (ui == null) return;
            count = (count + 1) % (maxCount + 1); // 0 → 1 → ... → maxCount → 0으로 순환
            UpdateBadge();
            ui.OnTileCountChanged();
        }

        /// <summary>배지 텍스트를 현재 count에 맞게 갱신한다. count가 0이면 배지를 비워 안 보이게 한다.</summary>
        public void UpdateBadge()
        {
            if (countBadge != null)
                countBadge.text = count > 0 ? ("x" + count) : "";
        }
    }
}
