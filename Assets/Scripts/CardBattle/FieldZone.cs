using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 필드 위의 카드 한 자리(슬롯). occupant가 null이면 빈 슬롯이다.
    /// 드래그 앤 드롭(CardDragHandler)이 Physics2D로 이 컴포넌트가 붙은 콜라이더를 찾아
    /// "여기 놓을 수 있는지"를 판단하므로, 씬에서 슬롯 오브젝트에는 반드시 Collider2D가 있어야 한다.
    /// </summary>
    public class FieldSlot : MonoBehaviour
    {
        public Transform occupant;
        public bool IsEmpty { get { return occupant == null; } }
    }

    /// <summary>
    /// 한 플레이어(또는 상대)의 필드 전체. 전열(frontRow)/후열(backRow) 각 5칸으로 구성되며,
    /// 배열 순서와 좌우 대칭은 씬에서 미리 배치해둔 FieldSlot 오브젝트들의 배열 그대로를 따른다.
    /// </summary>
    public class FieldZone : MonoBehaviour
    {
        public FieldSlot[] frontRow;
        public FieldSlot[] backRow;

        /// <summary>전열/후열 중 지정한 줄에서 가장 먼저 비어있는 슬롯을 찾는다(현재는 자동 배치 로직에서 미사용, 향후 AI/자동 배치용으로 남겨둠). 없으면 null.</summary>
        public FieldSlot GetFirstEmpty(bool front)
        {
            var row = front ? frontRow : backRow;
            if (row == null) return null;
            foreach (var s in row) if (s != null && s.IsEmpty) return s;
            return null;
        }

        /// <summary>
        /// 카드를 이 필드존의 자식으로 재배치하고 지정한 슬롯의 점유자로 등록한다.
        /// 실제 이동은 CardSlotMover가 있으면 부드럽게 보간하고, 없으면 즉시 스냅한다.
        /// </summary>
        public void PlaceCard(Transform card, FieldSlot slot)
        {
            if (slot == null) return;
            slot.occupant = card;
            card.SetParent(transform, true);
            var mover = card.GetComponent<CardSlotMover>();
            if (mover != null) mover.MoveTo(slot.transform.position, slot.transform.rotation);
            else { card.position = slot.transform.position; card.rotation = slot.transform.rotation; }
        }
    }
}
