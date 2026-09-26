using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 레인 전투 규칙. 턴을 종료할 때 CardManager가 Resolve()를 한 번 호출한다.
    /// 전투 규칙을 바꾸고 싶으면 이 파일만 고치면 된다(키워드 이름은 CardKeywords.cs).
    ///
    /// 현재 규칙:
    ///  1) 필드는 레인(세로 줄) 단위로 싸운다. 내 레인 N은 상대 레인 N과만 싸운다.
    ///  2) 전열 유닛은 항상 공격한다. 후열 유닛은 Ranged 키워드가 있을 때만 공격한다.
    ///  3) 공격 대상: 상대 레인의 전열 유닛 → 없으면 후열 유닛 → 둘 다 없으면 상대 히어로.
    ///     (그래서 전열 유닛이 후열 유닛과 히어로를 지켜준다)
    ///  4) 모든 공격은 동시에 계산한 뒤 한꺼번에 적용한다. 서로 때리는 두 유닛은 같이 죽을 수 있다.
    ///  5) Wall 키워드 유닛은 전열에 있을 때 받는 피해가 CardKeywords.WallDamageReduction만큼 줄어든다.
    ///  6) 체력이 0 이하가 된 유닛은 필드에서 제거된다.
    /// </summary>
    public static class LaneCombat
    {
        /// <summary>전투 한 번의 결과 요약.</summary>
        public class Result
        {
            public int damageToPlayerHero;
            public int damageToEnemyHero;
            public int unitsKilled;

            public int HeroDamageTo(Side side)
            {
                return side == Side.Player ? damageToPlayerHero : damageToEnemyHero;
            }
        }

        public static Result Resolve(FieldZone playerField, FieldZone enemyField)
        {
            var result = new Result();
            if (playerField == null || enemyField == null) return result;

            var pendingDamage = new Dictionary<CardView, int>();
            int lanes = System.Math.Min(playerField.LaneCount, enemyField.LaneCount);

            for (int lane = 0; lane < lanes; lane++)
            {
                result.damageToEnemyHero += CollectLaneAttacks(playerField, enemyField, lane, pendingDamage);
                result.damageToPlayerHero += CollectLaneAttacks(enemyField, playerField, lane, pendingDamage);
            }

            foreach (var pair in pendingDamage)
                pair.Key.ApplyDamage(pair.Value);

            result.unitsKilled = RemoveDeadUnits(playerField) + RemoveDeadUnits(enemyField);
            return result;
        }

        /// <summary>
        /// attacker 필드의 한 레인이 defender 필드의 같은 레인을 공격한다.
        /// 유닛에게 가는 피해는 pendingDamage에 모아두고, 히어로에게 가는 피해는 반환값으로 돌려준다.
        /// </summary>
        static int CollectLaneAttacks(FieldZone attacker, FieldZone defender, int lane, Dictionary<CardView, int> pendingDamage)
        {
            int heroDamage = 0;

            foreach (bool attackerInFront in new[] { true, false })
            {
                var slot = attacker.SlotAt(attackerInFront, lane);
                var unit = slot != null ? slot.OccupantView : null;
                if (!CanAttack(unit, attackerInFront)) continue;

                bool targetInFront;
                var target = FindTarget(defender, lane, out targetInFront);
                int damage = unit.data.attack;

                if (target == null)
                {
                    heroDamage += damage;
                }
                else
                {
                    damage = DamageAfterDefense(target, targetInFront, damage);
                    int already;
                    pendingDamage.TryGetValue(target, out already);
                    pendingDamage[target] = already + damage;
                }
            }
            return heroDamage;
        }

        static bool CanAttack(CardView unit, bool inFront)
        {
            if (unit == null || unit.data == null || unit.IsDead) return false;
            if (unit.data.attack <= 0) return false;
            return inFront || unit.data.HasKeyword(CardKeywords.Ranged);
        }

        /// <summary>상대 레인에서 맞을 유닛을 찾는다(전열 우선). 없으면 null = 히어로가 맞는다.</summary>
        static CardView FindTarget(FieldZone defender, int lane, out bool inFront)
        {
            var front = defender.SlotAt(true, lane);
            var frontUnit = front != null ? front.OccupantView : null;
            if (frontUnit != null && !frontUnit.IsDead) { inFront = true; return frontUnit; }

            var back = defender.SlotAt(false, lane);
            var backUnit = back != null ? back.OccupantView : null;
            inFront = false;
            if (backUnit != null && !backUnit.IsDead) return backUnit;
            return null;
        }

        static int DamageAfterDefense(CardView target, bool targetInFront, int damage)
        {
            if (targetInFront && target.data != null && target.data.HasKeyword(CardKeywords.Wall))
                damage -= CardKeywords.WallDamageReduction;
            return damage < 0 ? 0 : damage;
        }

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
