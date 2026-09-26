using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 한쪽 편의 필드 전체. 전열(frontRow)/후열(backRow)로 구성되며, 같은 인덱스끼리가 하나의 "레인(세로 줄)"이다.
    /// 예: frontRow[2]와 backRow[2]는 같은 레인이고, 상대 필드의 frontRow[2]/backRow[2]와 마주본다.
    /// 배열 순서는 씬에 배치된 슬롯의 왼쪽→오른쪽 순서를 따른다(에디터 메뉴 "씬 자동 구성"이 x좌표로 정렬해줌).
    /// </summary>
    public class FieldZone : MonoBehaviour
    {
        public FieldSlot[] frontRow;
        public FieldSlot[] backRow;

        /// <summary>레인 수 = 전열/후열 중 짧은 쪽의 칸 수.</summary>
        public int LaneCount
        {
            get
            {
                int f = frontRow != null ? frontRow.Length : 0;
                int b = backRow != null ? backRow.Length : 0;
                return Mathf.Min(f, b);
            }
        }

        /// <summary>지정한 줄(전열/후열)과 레인 번호의 슬롯. 범위를 벗어나면 null.</summary>
        public FieldSlot SlotAt(bool front, int lane)
        {
            var row = front ? frontRow : backRow;
            if (row == null || lane < 0 || lane >= row.Length) return null;
            return row[lane];
        }

        /// <summary>지정한 줄에서 가장 왼쪽의 빈 슬롯. 없으면 null.</summary>
        public FieldSlot GetFirstEmpty(bool front)
        {
            var row = front ? frontRow : backRow;
            if (row == null) return null;
            foreach (var s in row) if (s != null && s.IsEmpty) return s;
            return null;
        }

        /// <summary>
        /// 카드를 이 필드존의 자식으로 옮기고 지정한 슬롯의 점유자로 등록한다.
        /// CardSlotMover가 있으면 부드럽게 이동하고, 없으면 즉시 제자리로 옮긴다.
        /// </summary>
        public void PlaceCard(Transform card, FieldSlot slot)
        {
            if (slot == null || card == null) return;
            slot.occupant = card;
            card.SetParent(transform, true);
            var mover = card.GetComponent<CardSlotMover>();
            if (mover != null) mover.MoveTo(slot.transform.position, slot.transform.rotation);
            else { card.position = slot.transform.position; card.rotation = slot.transform.rotation; }
        }

        /// <summary>슬롯에 놓인 카드를 제거(파괴)하고 슬롯을 비운다. 전투에서 유닛이 죽었을 때 사용.</summary>
        public void DestroyCardAt(FieldSlot slot)
        {
            if (slot == null || slot.occupant == null) return;
            var go = slot.occupant.gameObject;
            slot.occupant = null;
            UnityUtil.DestroySafe(go);
        }
    }
}
