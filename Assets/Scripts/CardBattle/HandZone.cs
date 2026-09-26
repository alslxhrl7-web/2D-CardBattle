using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 손패 한 벌(플레이어 또는 상대)의 카드를 부채꼴로 자동 배치한다.
    /// 이 오브젝트의 자식으로는 오직 "현재 손패에 있는 카드"만 있어야 한다는 것이 전제 조건이다
    /// (CardManager.ClearHand가 리셋 시 이 하이어라키를 통째로 비운다). 그래서 카드를 넣고 뺄 때는
    /// 반드시 AddCard/RemoveCard를 통해야 cards 리스트와 실제 자식 목록이 어긋나지 않는다.
    /// </summary>
    public class HandZone : MonoBehaviour
    {
        public List<Transform> cards = new List<Transform>(); // 손패에 있는 카드들 (왼쪽부터 순서대로)
        public float cardSpacing = 1.4f;   // 카드 사이 가로 간격(월드 유닛)
        public float fanAngleDeg = 6f;     // 중앙에서 바깥쪽으로 갈수록 카드가 기울어지는 각도
        public float arcHeight = 0.25f;    // 부채꼴 아치 곡률(바깥 카드일수록 아래로 내려감)

        /// <summary>카드를 손패에 추가하고 즉시 전체를 재배치한다.</summary>
        public void AddCard(Transform card)
        {
            cards.Add(card);
            card.SetParent(transform, true);
            Relayout();
        }

        /// <summary>카드를 손패에서 제거(필드에 낼 때 등)하고 남은 카드들을 다시 부채꼴로 재배치한다.</summary>
        public void RemoveCard(Transform card)
        {
            cards.Remove(card);
            card.SetParent(null, true); // 손패의 자식에서 뺀다(곧 필드의 자식이 됨)
            Relayout();
        }

        /// <summary>
        /// 현재 cards 순서대로 부채꼴 위치/회전/겹침 순서(sortingOrder)를 다시 계산한다.
        /// 드래그가 유효하지 않은 곳에 떨어졌을 때도 이 함수가 호출되어 카드를 원래 자리로 되돌린다.
        /// </summary>
        public void Relayout()
        {
            int n = cards.Count;
            for (int i = 0; i < n; i++)
            {
                float mid = (n - 1) / 2f;
                float offset = i - mid; // 중앙 기준 좌우 대칭 오프셋(음수=왼쪽, 양수=오른쪽)
                Vector3 pos = transform.position + new Vector3(offset * cardSpacing, -Mathf.Abs(offset) * arcHeight, -i * 0.01f);
                Quaternion rot = Quaternion.Euler(0, 0, -offset * fanAngleDeg);
                var mover = cards[i].GetComponent<CardSlotMover>();
                if (mover != null) mover.MoveTo(pos, rot);                   // 애니메이션 이동
                else { cards[i].position = pos; cards[i].rotation = rot; }  // 즉시 이동

                // 손패 안에서 카드가 겹칠 때 "인덱스가 클수록(오른쪽일수록) 위에 그려지도록"
                // 카드별 sortingOrder 버킷을 인덱스 기준으로 다시 매긴다.
                var view = cards[i].GetComponent<CardView>();
                if (view != null) view.SetLayerBase(i);
            }
        }
    }
}
