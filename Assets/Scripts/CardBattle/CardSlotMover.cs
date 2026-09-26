using UnityEngine;
using System.Collections;

namespace CardBattle
{
    /// <summary>
    /// 카드가 목표 위치/회전으로 부드럽게 이동하도록 도와주는 보조 컴포넌트(CardView 프리팹에 붙어있음).
    /// 손패 재배치(HandZone.Relayout)와 필드 배치(FieldZone.PlaceCard) 양쪽에서 공용으로 쓰인다.
    /// 이 컴포넌트가 없는 카드는 호출하는 쪽에서 즉시 이동으로 대신 처리한다.
    /// </summary>
    public class CardSlotMover : MonoBehaviour
    {
        public float duration = 0.25f; // 이동에 걸리는 시간(초)
        Coroutine active;              // 지금 진행 중인 이동 (없으면 null)

        /// <summary>목표 위치/회전으로 이동을 시작한다. 이동 중에 다시 호출되면 기존 이동을 취소하고 새 목표로 바꾼다.</summary>
        public void MoveTo(Vector3 pos, Quaternion rot)
        {
            // 비활성 오브젝트에서는 코루틴을 시작할 수 없으므로 즉시 이동 처리.
            if (!gameObject.activeInHierarchy) { transform.position = pos; transform.rotation = rot; return; }
            if (active != null) StopCoroutine(active);
            active = StartCoroutine(MoveRoutine(pos, rot));
        }

        /// <summary>매 프레임 조금씩 목표 쪽으로 옮기는 코루틴.</summary>
        IEnumerator MoveRoutine(Vector3 targetPos, Quaternion targetRot)
        {
            Vector3 startPos = transform.position;      // 출발 위치
            Quaternion startRot = transform.rotation;   // 출발 회전
            float t = 0f;                               // 지난 시간
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration); // 진행률 0~1
                transform.position = Vector3.Lerp(startPos, targetPos, k);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
                yield return null; // 다음 프레임까지 대기
            }
            // 소수점 오차로 정확히 목표에 도달하지 못할 수 있으므로 마지막에 딱 맞춰 마무리.
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
    }
}
