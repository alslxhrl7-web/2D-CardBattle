using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 전투 연출(액션). 게임 규칙에는 영향을 주지 않고 "보여주기"만 한다.
    ///   1) 레인마다 공격하는 유닛이 상대 쪽으로 돌진했다가 돌아온다
    ///   2) 맞은 카드는 빨갛게 번쩍이고, 머리 위에 "-피해" 숫자가 떠올랐다 사라진다 (히어로가 맞으면 체력 표시 쪽에)
    ///   3) 죽은 카드는 빙글 돌면서 작아져 사라진다
    ///   소리: 돌진할 때 유닛별 공격음, 부딪힐 때 타격음(Hit_1·Hit_2 번갈아), 죽을 때 사망음 (GameAudio)
    /// 속도나 거리를 바꾸고 싶으면 아래 상수만 고치면 된다. CardManager가 턴 종료 때 코루틴으로 실행한다.
    /// </summary>
    public static class CombatAnimation
    {
        // ---- 연출 수치 (초 / 월드 유닛) ----
        const float LungeTime = 0.18f;        // 돌진하는 데 걸리는 시간
        const float ReturnTime = 0.18f;       // 제자리로 돌아오는 데 걸리는 시간
        const float LungeDistance = 0.55f;    // 목표까지 거리 중 얼마나 파고드는지 (0~1)
        const float LaneGap = 0.08f;          // 레인과 레인 사이 쉬는 시간
        const float FlashTime = 0.15f;        // 맞은 카드가 빨갛게 번쩍이는 시간
        const float PopupTime = 0.7f;         // 피해 숫자가 떠 있는 시간
        const float PopupRise = 0.6f;         // 피해 숫자가 위로 떠오르는 높이
        const float PopupCharSize = 0.16f;    // 피해 숫자 글자 크기
        const float DeathTime = 0.35f;        // 죽은 카드가 사라지는 데 걸리는 시간
        const float DeathSpin = 180f;         // 사라지면서 도는 각도
        const int FrontLayerBase = 1200;      // 연출 중인 카드를 다른 카드보다 위에 그리기 위한 정렬 구간
        const int PopupSortingOrder = 32500;  // 피해 숫자는 모든 것보다 위에

        static readonly Color HitFlashColor = new Color(1f, 0.25f, 0.2f); // 맞았을 때 번쩍이는 색
        static readonly Color PopupColor = new Color(1f, 0.3f, 0.25f);    // 피해 숫자 색

        static readonly List<GameObject> popups = new List<GameObject>(); // 떠 있는 피해 숫자들 (중간에 정리할 때 사용)

        /// <summary>
        /// 모든 공격을 레인 순서대로 보여준다. 같은 레인의 공격은 동시에 일어난다.
        /// runner: 코루틴을 돌릴 오브젝트(CardManager), textStyle: 피해 숫자에 쓸 글꼴 원본(없으면 숫자 생략).
        /// </summary>
        public static IEnumerator PlayAttacks(MonoBehaviour runner, LaneCombat.Result plan, Vector3 playerHeroPos, Vector3 enemyHeroPos, TextMesh textStyle)
        {
            // 레인별로 묶기 (Chain Cavalry의 추가 공격은 같은 레인의 첫 공격이 끝난 다음 따로 보여준다)
            var byLane = new SortedDictionary<int, List<LaneCombat.Hit>>();
            foreach (var hit in plan.hits)
            {
                if (hit.attacker == null) continue;
                int key = hit.lane * 2 + (hit.chained ? 1 : 0);
                List<LaneCombat.Hit> list;
                if (!byLane.TryGetValue(key, out list)) { list = new List<LaneCombat.Hit>(); byLane[key] = list; }
                list.Add(hit);
            }

            foreach (var lane in byLane.Values)
            {
                // 각 공격자의 출발점과 목표점
                var homes = new List<Vector3>();
                var aims = new List<Vector3>();
                foreach (var hit in lane)
                {
                    Vector3 home = hit.attacker.transform.position;
                    Vector3 heroPos = hit.targetSide == Side.Player ? playerHeroPos : enemyHeroPos;
                    Vector3 targetPos = hit.target != null ? hit.target.transform.position
                                      : new Vector3(home.x, heroPos.y, home.z); // 히어로를 칠 때는 곧장 앞으로 돌진
                    homes.Add(home);
                    aims.Add(Vector3.Lerp(home, targetPos, LungeDistance));
                    hit.attacker.SetLayerBase(FrontLayerBase); // 돌진하는 카드는 위에 그린다
                }

                // 돌진 (유닛마다 자기 공격음)
                foreach (var hit in lane) GameAudio.PlayAttack(hit.attacker.data);
                yield return runner.StartCoroutine(MoveAll(lane, homes, aims, LungeTime));

                // 부딪힌 순간: 번쩍임 + 피해 숫자 + 타격음 (방어로 피해가 0이면 막는 소리)
                for (int i = 0; i < lane.Count; i++)
                {
                    var hit = lane[i];
                    if (hit.damage > 0) GameAudio.PlayHit();
                    else GameAudio.Play(GameAudio.WallBlock);
                    if (hit.target != null)
                    {
                        runner.StartCoroutine(Flash(hit.target));
                        ShowPopup(runner, textStyle, hit.target.transform.position, hit.damage);
                    }
                    else
                    {
                        ShowPopup(runner, textStyle, hit.targetSide == Side.Player ? playerHeroPos : enemyHeroPos, hit.damage);
                    }
                }

                // 복귀
                yield return runner.StartCoroutine(MoveAll(lane, aims, homes, ReturnTime));
                yield return new WaitForSeconds(LaneGap);
            }
        }

        /// <summary>죽은 카드들이 동시에 빙글 돌며 작아져 사라진다. (실제 제거는 끝난 뒤 CardManager가 한다)</summary>
        public static IEnumerator PlayDeaths(List<CardView> dead)
        {
            if (dead == null || dead.Count == 0) yield break;
            GameAudio.Play(GameAudio.UnitDeath);

            var startScale = new List<Vector3>();
            foreach (var view in dead) startScale.Add(view != null ? view.transform.localScale : Vector3.zero);

            float t = 0f;
            while (t < DeathTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / DeathTime); // 진행률 0~1
                for (int i = 0; i < dead.Count; i++)
                {
                    if (dead[i] == null) continue;
                    dead[i].transform.localScale = startScale[i] * (1f - k);                 // 점점 작게
                    dead[i].transform.rotation = Quaternion.Euler(0f, 0f, DeathSpin * k);    // 빙글
                }
                yield return null;
            }
        }

        /// <summary>남아있는 피해 숫자를 모두 지운다(새 판 시작 등 연출이 중간에 끊겼을 때).</summary>
        public static void ClearPopups()
        {
            foreach (var go in popups) UnityUtil.DestroySafe(go);
            popups.Clear();
        }

        // ================= 내부 동작 =================

        /// <summary>한 레인의 공격자들을 from → to로 동시에 옮긴다(부드럽게 가속/감속).</summary>
        static IEnumerator MoveAll(List<LaneCombat.Hit> lane, List<Vector3> from, List<Vector3> to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                k = k * k * (3f - 2f * k); // 부드러운 곡선(시작/끝이 천천히)
                for (int i = 0; i < lane.Count; i++)
                    if (lane[i].attacker != null) lane[i].attacker.transform.position = Vector3.Lerp(from[i], to[i], k);
                yield return null;
            }
            for (int i = 0; i < lane.Count; i++)
                if (lane[i].attacker != null) lane[i].attacker.transform.position = to[i]; // 정확히 도착
        }

        /// <summary>카드의 모든 그림을 잠깐 빨갛게 했다가 원래 색으로 되돌린다.</summary>
        static IEnumerator Flash(CardView view)
        {
            if (view == null) yield break;
            var renderers = view.GetComponentsInChildren<SpriteRenderer>(true);
            var original = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                original[i] = renderers[i].color;
                var c = HitFlashColor;
                c.a = original[i].a; // 투명도는 유지
                renderers[i].color = c;
            }
            yield return new WaitForSeconds(FlashTime);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = original[i]; // 원래 색 복구
        }

        /// <summary>"-피해" 숫자를 만들어 위로 떠오르며 사라지게 한다.</summary>
        static void ShowPopup(MonoBehaviour runner, TextMesh textStyle, Vector3 position, int damage)
        {
            if (textStyle == null || damage <= 0) return;
            var go = Object.Instantiate(textStyle.gameObject); // 기존 글자와 같은 글꼴로 복제
            go.name = "DamagePopup";
            go.transform.SetParent(null, false);
            go.transform.position = position;
            go.SetActive(true);
            var text = go.GetComponent<TextMesh>();
            text.text = "-" + damage;
            text.color = PopupColor;
            text.characterSize = PopupCharSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = PopupSortingOrder;
            popups.Add(go);
            runner.StartCoroutine(PopupRoutine(go, text, position));
        }

        /// <summary>피해 숫자가 위로 떠오르면서 점점 투명해지다가 없어진다.</summary>
        static IEnumerator PopupRoutine(GameObject go, TextMesh text, Vector3 start)
        {
            float t = 0f;
            while (t < PopupTime && go != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / PopupTime);
                go.transform.position = start + new Vector3(0f, PopupRise * k, 0f); // 위로
                var c = text.color;
                c.a = 1f - k * k;                                                   // 뒤로 갈수록 빨리 투명해짐
                text.color = c;
                yield return null;
            }
            popups.Remove(go);
            UnityUtil.DestroySafe(go);
        }
    }
}
