using UnityEngine;
using UnityEngine.InputSystem;

namespace CardBattle
{
    /// <summary>
    /// 손패의 카드 하나를 마우스로 집어서(OnMouseDown) 드래그하고(OnMouseDrag) 놓으면(OnMouseUp)
    /// 그 아래 있는 FieldSlot에 배치를 시도하는 컴포넌트. 카드 프리팹 루트에 붙는다.
    /// Unity의 OnMouse* 메시지를 받으려면 이 오브젝트에 Collider2D가 있어야 한다(프리팹에 BoxCollider2D 추가됨).
    /// 이 프로젝트는 Active Input Handling이 새 Input System 전용으로 설정돼 있어, 레거시 Input 클래스 대신
    /// UnityEngine.InputSystem의 Mouse.current로 마우스 좌표를 읽는다.
    /// </summary>
    [RequireComponent(typeof(CardView))]
    public class CardDragHandler : MonoBehaviour
    {
        CardView view;
        CardManager manager;
        HandZone myHand;
        Vector3 dragOffset;   // 클릭한 지점과 카드 중심의 차이(드래그 중 카드가 마우스에 "붙잡힌 듯" 자연스럽게 움직이게 함)
        bool dragging;

        const int DragSortingBase = 999; // 드래그 중인 카드는 다른 모든 카드보다 항상 위에 그려지도록 하는 sortingOrder 버킷

        void Awake()
        {
            view = GetComponent<CardView>();
        }

        /// <summary>카드가 스폰될 때 CardManager가 호출: 어느 매니저/손패 소속 카드인지 기억해둔다.</summary>
        public void Init(CardManager mgr, HandZone hand)
        {
            manager = mgr;
            myHand = hand;
        }

        /// <summary>
        /// 이 카드가 필드에 성공적으로 배치된 뒤 호출됨: 더 이상 손패 소속이 아니므로,
        /// 이후 실수로 들어오는 클릭 이벤트가 손패에서 이 카드를 빼려고 시도하지 않도록 참조를 끊는다.
        /// </summary>
        public void ClearHand()
        {
            myHand = null;
            dragging = false;
        }

        void OnMouseDown()
        {
            if (manager == null || myHand == null) return;
            if (!manager.IsPlayerHand(myHand)) return; // 상대(청) 손패는 아직 드래그 불가 — 플레이어 손패만 조작 가능

            dragging = true;
            Vector3 mouseWorld = GetMouseWorld();
            dragOffset = transform.position - mouseWorld;
            view.SetLayerBase(DragSortingBase);
        }

        void OnMouseDrag()
        {
            if (!dragging) return;
            Vector3 mouseWorld = GetMouseWorld();
            transform.position = mouseWorld + dragOffset;
        }

        void OnMouseUp()
        {
            if (!dragging) return;
            dragging = false;
            TryDrop();
        }

        /// <summary>화면 마우스 좌표를 카드와 같은 z 평면 위의 월드 좌표로 변환한다.</summary>
        Vector3 GetMouseWorld()
        {
            Camera cam = Camera.main;
            Vector2 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Vector3 screen = new Vector3(mouseScreen.x, mouseScreen.y, 0f);
            // 오소그래픽 카메라에서 카드가 있는 z 평면까지의 거리.
            screen.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(screen);
            world.z = transform.position.z;
            return world;
        }

        void TryDrop()
        {
            FieldSlot slot = FindSlotUnderCard();

            if (slot != null && slot.IsEmpty && manager.TryPlaceCard(view, myHand, slot))
                return; // 필드에 정상적으로 낸 경우

            // 유효하지 않은 드롭(슬롯 없음 / 이미 참 / 군력 부족): 손패 부채꼴 자리로 되돌아간다.
            // 카드가 여전히 hand.cards에 남아있으므로 Relayout()이 애니메이션과 함께 원위치로 복귀시키고
            // sortingOrder 버킷도 되돌려준다.
            if (myHand != null) myHand.Relayout();
        }

        /// <summary>
        /// 카드 자신의 위치 아래에 있는 FieldSlot을 찾는다. OverlapPointAll(단일 히트가 아니라 전체)을 쓰는 이유:
        /// 드래그 중인 카드 자신의 콜라이더도 같은 지점에 겹쳐 있어서, OverlapPoint 하나만 쓰면 자기 자신이
        /// 걸릴 수 있다. 히트 목록 중 FieldSlot 컴포넌트가 있는 것만 채택한다.
        /// </summary>
        FieldSlot FindSlotUnderCard()
        {
            var hits = Physics2D.OverlapPointAll(transform.position);
            foreach (var h in hits)
            {
                var slot = h.GetComponent<FieldSlot>();
                if (slot != null) return slot;
            }
            return null;
        }
    }
}
