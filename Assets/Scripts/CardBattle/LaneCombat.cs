using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 레인 전투 규칙. 턴을 종료할 때 CardManager가 호출한다.
    /// 전투 규칙을 바꾸고 싶으면 이 파일만 고치면 된다(키워드 이름은 CardKeywords.cs, 연출은 CombatAnimation.cs).
    ///
    /// 현재 규칙:
    ///  1) 필드는 레인(세로 줄) 단위로 싸운다. 내 레인 N은 상대 레인 N과만 싸운다.
    ///  2) 전열 유닛은 항상 공격한다. 후열 유닛은 Ranged 키워드가 있을 때만 공격한다.
    ///  3) 공격 대상: 상대 레인의 전열 유닛 → 없으면 후열 유닛 → 둘 다 없으면 상대 히어로.
    ///     (그래서 전열 유닛이 후열 유닛과 히어로를 지켜준다)
    ///  4) 모든 공격은 동시에 계산한 뒤 한꺼번에 적용한다. 서로 때리는 두 유닛은 같이 죽을 수 있다.
    ///  5) Wall 키워드 유닛은 전열에 있을 때 받는 피해가 CardKeywords.WallDamageReduction만큼 줄어든다.
    ///  6) 체력이 0 이하가 된 유닛은 필드에서 제거된다.
    ///
    /// 연출을 넣을 수 있도록 세 단계로 나눠져 있다:
    ///   Plan()        : 누가 누구를 얼마나 때리는지 계산만 한다 (아직 아무도 다치지 않음)
    ///   ApplyDamage() : 계산한 피해를 유닛에 적용한다 (죽은 유닛은 아직 필드에 남아있음)
    ///   RemoveDead()  : 죽은 유닛을 필드에서 치운다
    ///   Resolve()     : 위 세 단계를 한 번에 (연출 없이 바로 결과만 필요할 때)
    /// </summary>
    public static class LaneCombat
    {
        /// <summary>공격 한 번의 기록 (연출에서 "누가 → 누구에게 → 몇 피해"를 보여줄 때 사용).</summary>
        public class Hit
        {
            public int lane;           // 몇 번째 레인에서 일어난 공격인지
            public CardView attacker;  // 공격한 유닛
            public CardView target;    // 맞은 유닛 (null이면 히어로가 맞음)
            public Side targetSide;    // 맞은 쪽 편
            public int damage;         // 실제로 들어가는 피해 (방어 효과 반영 후)
        }

        /// <summary>전투 한 번의 결과 요약.</summary>
        public class Result
        {
            public int damageToPlayerHero; // 플레이어 히어로가 받은 피해
            public int damageToEnemyHero;  // 상대 히어로가 받은 피해
            public int unitsKilled;        // 이번 전투에서 죽은 유닛 수(양쪽 합)
            public List<Hit> hits = new List<Hit>();           // 모든 공격 기록 (레인 순서)
            public List<CardView> deadUnits = new List<CardView>(); // ApplyDamage 후 죽은 유닛들

            /// <summary>해당 편 히어로가 받은 피해.</summary>
            public int HeroDamageTo(Side side)
            {
                return side == Side.Player ? damageToPlayerHero : damageToEnemyHero;
            }
        }

        /// <summary>계산 → 피해 적용 → 죽은 유닛 제거를 한 번에 한다. (히어로 체력 반영은 CardManager가 한다)</summary>
        public static Result Resolve(FieldZone playerField, FieldZone enemyField)
        {
            var result = Plan(playerField, enemyField);
            ApplyDamage(result);
            result.unitsKilled = RemoveDead(playerField, enemyField);
            return result;
        }

        /// <summary>양쪽 필드의 모든 공격을 계산만 한다. 유닛 체력은 아직 바뀌지 않는다.</summary>
        public static Result Plan(FieldZone playerField, FieldZone enemyField)
        {
            var result = new Result();
            if (playerField == null || enemyField == null) return result;

            int lanes = System.Math.Min(playerField.LaneCount, enemyField.LaneCount);
            for (int lane = 0; lane < lanes; lane++)
            {
                CollectLaneAttacks(playerField, enemyField, lane, Side.Enemy, result);  // 내 유닛 → 상대
                CollectLaneAttacks(enemyField, playerField, lane, Side.Player, result); // 상대 유닛 → 나
            }
            return result;
        }

        /// <summary>Plan()에서 계산한 유닛 피해를 한꺼번에 적용하고, 죽은 유닛을 deadUnits에 모은다(아직 제거하지 않음).</summary>
        public static void ApplyDamage(Result result)
        {
            // 같은 유닛이 여러 번 맞았으면 피해를 합산해서 한 번에 적용한다(동시 피해)
            var total = new Dictionary<CardView, int>();
            foreach (var hit in result.hits)
            {
                if (hit.target == null) continue; // 히어로 피해는 CardManager가 처리
                int already;
                total.TryGetValue(hit.target, out already);
                total[hit.target] = already + hit.damage;
            }
            foreach (var pair in total)
            {
                pair.Key.ApplyDamage(pair.Value);
                if (pair.Key.IsDead) result.deadUnits.Add(pair.Key);
            }
        }

        /// <summary>양쪽 필드에서 체력이 0 이하인 유닛을 치우고, 치운 수를 돌려준다.</summary>
        public static int RemoveDead(FieldZone playerField, FieldZone enemyField)
        {
            int killed = 0;
            if (playerField != null) killed += RemoveDeadUnits(playerField);
            if (enemyField != null) killed += RemoveDeadUnits(enemyField);
            return killed;
        }

        /// <summary>
        /// attacker 필드의 한 레인이 defender 필드의 같은 레인을 공격하는 것을 계산해 result에 기록한다.
        /// 막는 유닛이 없으면 defenderSide 히어로 피해로 더한다.
        /// </summary>
        static void CollectLaneAttacks(FieldZone attacker, FieldZone defender, int lane, Side defenderSide, Result result)
        {
            // 전열 → 후열 순서로 공격할 수 있는 유닛을 확인한다
            foreach (bool attackerInFront in new[] { true, false })
            {
                var slot = attacker.SlotAt(attackerInFront, lane);
                var unit = slot != null ? slot.OccupantView : null;
                if (!CanAttack(unit, attackerInFront)) continue;

                bool targetInFront;
                var target = FindTarget(defender, lane, out targetInFront);
                int damage = unit.data.attack;
                if (target != null) damage = DamageAfterDefense(target, targetInFront, damage); // 방어 효과 적용

                result.hits.Add(new Hit { lane = lane, attacker = unit, target = target, targetSide = defenderSide, damage = damage });

                if (target == null) // 막는 유닛이 없으면 히어로가 맞는다
                {
                    if (defenderSide == Side.Player) result.damageToPlayerHero += damage;
                    else result.damageToEnemyHero += damage;
                }
            }
        }

        /// <summary>이 유닛이 공격할 수 있는지: 살아있고, 공격력이 있고, 전열이거나 원거리여야 한다.</summary>
        static bool CanAttack(CardView unit, bool inFront)
        {
            if (unit == null || unit.data == null || unit.IsDead) return false;
            if (unit.data.attack <= 0) return false;
            return inFront || unit.data.HasKeyword(CardKeywords.Ranged);
        }

        /// <summary>상대 레인에서 맞을 유닛을 찾는다(전열 우선). 없으면 null = 히어로가 맞는다.</summary>
        static CardView FindTarget(FieldZone defender, int lane, out bool inFront)
        {
            // 전열 확인
            var front = defender.SlotAt(true, lane);
            var frontUnit = front != null ? front.OccupantView : null;
            if (frontUnit != null && !frontUnit.IsDead) { inFront = true; return frontUnit; }

            // 전열이 비었으면 후열 확인
            var back = defender.SlotAt(false, lane);
            var backUnit = back != null ? back.OccupantView : null;
            inFront = false;
            if (backUnit != null && !backUnit.IsDead) return backUnit;
            return null;
        }

        /// <summary>맞는 쪽의 방어 효과(현재는 Wall)를 반영한 최종 피해. 0 아래로는 내려가지 않는다.</summary>
        static int DamageAfterDefense(CardView target, bool targetInFront, int damage)
        {
            if (targetInFront && target.data != null && target.data.HasKeyword(CardKeywords.Wall))
                damage -= CardKeywords.WallDamageReduction;
            return damage < 0 ? 0 : damage;
        }

        /// <summary>체력이 0 이하인 유닛을 필드에서 제거하고, 제거한 수를 돌려준다.</summary>
        static int RemoveDeadUnits(FieldZone field)
        {
            int killed = 0;
            foreach (bool front in new[] { true, false })
            {
                var row = front ? field.frontRow : field.backRow;
                if (row == null) continue;
                foreach (var slot in row)
                {
                    var unit = slot != null ? slot.OccupantView : null;
                    if (unit == null || !unit.IsDead) continue;
                    field.DestroyCardAt(slot);
                    killed++;
                }
            }
            return killed;
        }
    }
}
