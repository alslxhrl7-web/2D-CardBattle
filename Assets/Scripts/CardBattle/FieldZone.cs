using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 한쪽 편의 필드 전체. 전열(frontRow)/후열(backRow)로 구성되며, 같은 번호끼리가 하나의 "레인(세로 줄)"이다.
    /// 전열 = 유닛·진을 놓고 싸우는 칸, 후열 = 장비를 놓는 칸(같은 레인 전열 카드를 강화).
    /// 예: frontRow[2]와 backRow[2]는 같은 레인이고, 상대 필드의 frontRow[2]/backRow[2]와 마주본다.
    /// 배열 순서는 씬에 배치된 슬롯의 왼쪽→오른쪽 순서를 따른다(메뉴 "씬 자동 구성"이 x좌표로 정렬해줌).
    /// </summary>
    public class FieldZone : MonoBehaviour
    {
        public FieldSlot[] frontRow; // 전열 슬롯들 (상대와 가까운 줄)
        public FieldSlot[] backRow;  // 후열 슬롯들 (전열 뒤쪽 줄)

        // 후열(장비) 카드는 작게, 앞 카드에서 조금 떨어뜨려 놓는다. 전열·후열 칸 사이(1.3)가 카드 높이(2.05)보다
        // 좁아서 원래 크기로 놓으면 앞 카드에 1/3쯤 가려지기 때문. (0.7배 + 0.45 뒤로 → 앞 카드와 거의 안 겹침)
        const float BackRowCardScale = 0.7f; // 후열 카드 크기 배율
        const float BackRowShift = 0.45f;    // 후열 카드를 앞 카드 반대쪽으로 밀어내는 거리 (월드 유닛)

        /// <summary>레인 수 = 전열/후열 중 짧은 쪽의 칸 수.</summary>
        public int LaneCount
        {
            get
            {
                int f = frontRow != null ? frontRow.Length : 0; // 전열 칸 수
                int b = backRow != null ? backRow.Length : 0;   // 후열 칸 수
                return Mathf.Min(f, b);
            }
        }

        /// <summary>지정한 줄(front=true면 전열)과 레인 번호의 슬롯. 범위를 벗어나면 null.</summary>
        public FieldSlot SlotAt(bool front, int lane)
        {
            var row = front ? frontRow : backRow;
            if (row == null || lane < 0 || lane >= row.Length) return null;
            return row[lane];
        }

        /// <summary>이 슬롯이 몇 번째 레인인지. 이 필드의 슬롯이 아니면 -1. front에는 전열이면 true.</summary>
        public int LaneOf(FieldSlot slot, out bool front)
        {
            front = false;
            if (slot == null) return -1;
            if (frontRow != null) for (int i = 0; i < frontRow.Length; i++) if (frontRow[i] == slot) { front = true; return i; }
            if (backRow != null) for (int i = 0; i < backRow.Length; i++) if (backRow[i] == slot) return i;
            return -1;
        }

        /// <summary>이 슬롯이 이 필드의 전열 칸인지.</summary>
        public bool IsFrontSlot(FieldSlot slot)
        {
            bool front;
            return LaneOf(slot, out front) >= 0 && front;
        }

        /// <summary>이 슬롯이 이 필드의 후열 칸(장비 칸)인지.</summary>
        public bool IsBackSlot(FieldSlot slot)
        {
            bool front;
            return LaneOf(slot, out front) >= 0 && !front;
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
        /// 카드를 이 필드의 자식으로 옮기고 지정한 슬롯의 주인으로 등록한다.
        /// CardSlotMover가 있으면 부드럽게 이동하고, 없으면 즉시 제자리로 옮긴다. 후열 카드는 작게 줄여서 놓는다.
        /// </summary>
        public void PlaceCard(Transform card, FieldSlot slot)
        {
            if (slot == null || card == null) return;
            slot.occupant = card;
            card.SetParent(transform, true); // 월드 위치를 유지한 채 필드의 자식으로

            bool front;
            int lane = LaneOf(slot, out front);
            card.localScale = Vector3.one * (front ? 1f : BackRowCardScale);
            Vector3 pos = front ? slot.transform.position : BackRowCardPosition(slot, lane);

            var mover = card.GetComponent<CardSlotMover>();
            if (mover != null) mover.MoveTo(pos, slot.transform.rotation); // 애니메이션 이동
            else { card.position = pos; card.rotation = slot.transform.rotation; } // 즉시 이동
        }

        /// <summary>후열 카드가 놓일 위치: 칸 위치에서 같은 레인 전열 반대쪽으로 BackRowShift만큼.</summary>
        Vector3 BackRowCardPosition(FieldSlot slot, int lane)
        {
            var front = SlotAt(true, lane);
            if (front == null) return slot.transform.position;
            float away = Mathf.Sign(slot.transform.position.y - front.transform.position.y); // 내 필드면 아래(-), 상대 필드면 위(+)
            return slot.transform.position + new Vector3(0f, away * BackRowShift, 0f);
        }

        /// <summary>슬롯에 놓인 카드를 파괴하고 슬롯을 비운다. 전투에서 유닛이 죽었을 때 사용.</summary>
        public void DestroyCardAt(FieldSlot slot)
        {
            if (slot == null || slot.occupant == null) return;
            var go = slot.occupant.gameObject;
            slot.occupant = null;     // 먼저 슬롯을 비우고
            UnityUtil.DestroySafe(go); // 카드 오브젝트를 파괴
        }
    }
}
