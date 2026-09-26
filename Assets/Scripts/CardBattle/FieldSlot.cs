using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 필드 위의 카드 한 자리(슬롯). occupant가 null이면 빈 슬롯이다.
    /// 드래그 앤 드롭(CardDragHandler)이 Physics2D로 이 컴포넌트가 붙은 콜라이더를 찾아
    /// "여기 놓을 수 있는지"를 판단하므로, 씬의 슬롯 오브젝트에는 반드시 Collider2D가 있어야 한다.
    ///
    /// (예전에는 FieldZone.cs 안에 같이 들어있었는데, Unity는 MonoBehaviour 클래스 이름과 파일 이름이
    ///  같아야 안정적으로 스크립트를 인식하므로 별도 파일로 분리했다.)
    /// </summary>
    public class FieldSlot : MonoBehaviour
    {
        public Transform occupant;

        public bool IsEmpty { get { return occupant == null; } }

        /// <summary>이 슬롯에 놓인 카드의 CardView. 비어있으면 null.</summary>
        public CardView OccupantView
        {
            get { return occupant != null ? occupant.GetComponent<CardView>() : null; }
        }
    }
}
