using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 레인 전투 규칙. 턴을 종료할 때 CardManager가 호출한다.
    /// 전투 규칙을 바꾸고 싶으면 이 파일만 고치면 된다(키워드 이름은 CardKeywords.cs, 연출은 CombatAnimation.cs).
    ///
    /// 현재 규칙:
    ///  1) 필드는 레인(세로 줄) 단위로 싸운다. 내 레인 N은 상대 레인 N과만 싸운다.
    ///  2) 공격은 전열 카드만 한다. 후열은 장비 칸이라 공격하지도, 공격받지도 않는다.
    ///  3) 공격 대상: 상대 레인의 전열 카드 → 없으면 상대 히어로. (그래서 전열 카드가 히어로를 지켜준다)
    ///  4) 모든 공격은 동시에 계산한 뒤 한꺼번에 적용한다. 서로 때리는 두 유닛은 같이 죽을 수 있다.
    ///  5) Wall 키워드 카드는 받는 피해가 CardKeywords.WallDamageReduction만큼 줄어든다. 단 Ranged 카드의 공격은 Wall을 무시한다.
    ///  6) 체력이 0 이하가 된 유닛은 필드에서 제거된다.
    ///
    /// 연출을 넣을 수 있도록 세 단계로 나눠져 있다 (CardManager가 순서대로 부른다):
    ///   Plan()        : 누가 누구를 얼마나 때리는지 계산만 한다 (아직 아무도 다치지 않음)
    ///   ApplyDamage() : 계산한 피해를 유닛에 적용한다 (죽은 유닛은 아직 필드에 남아있음)
    ///   RemoveDead()  : 죽은 유닛을 필드에서 치운다
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
            public List<Hit> hits = new List<Hit>();           // 모든 공격 기록 (레인 순서)
            public List<CardView> deadUnits = new List<CardView>(); // ApplyDamage 후 죽은 유닛들
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

        /// <summary>양쪽 필드에서 체력이 0 이하인 유닛을 치운다.</summary>
        public static void RemoveDead(FieldZone playerField, FieldZone enemyField)
        {
            RemoveDeadUnits(playerField);
            RemoveDeadUnits(enemyField);
        }

        /// <summary>
        /// attacker 필드의 한 레인이 defender 필드의 같은 레인을 공격하는 것을 계산해 result에 기록한다.
        /// 막는 유닛이 없으면 defenderSide 히어로 피해로 더한다.
        /// </summary>
        static void CollectLaneAttacks(FieldZone attacker, FieldZone defender, int lane, Side defenderSide, Result result)
        {
            var unit = FrontCard(attacker, lane); // 공격은 전열 카드만 한다 (후열은 장비 칸)
            if (!CanAttack(unit)) return;

            var target = FrontCard(defender, lane); // 맞는 것도 전열 카드만. 없으면 null = 히어로가 맞는다
            int damage = target != null ? DamageAfterDefense(unit, target, unit.currentAttack) : unit.currentAttack;
            result.hits.Add(new Hit { lane = lane, attacker = unit, target = target, targetSide = defenderSide, damage = damage });

            if (target == null) // 막는 유닛이 없으면 히어로가 맞는다
            {
                if (defenderSide == Side.Player) result.damageToPlayerHero += damage;
                else result.damageToEnemyHero += damage;
            }
        }

        /// <summary>이 카드가 공격할 수 있는지: 살아있는 유닛·진이고 공격력이 있어야 한다.</summary>
        static bool CanAttack(CardView unit)
        {
            return unit != null && unit.data != null && !unit.IsDead && unit.data.IsFieldCard && unit.currentAttack > 0;
        }

        /// <summary>해당 레인 전열에 있는 살아있는 카드. 없으면 null.</summary>
        static CardView FrontCard(FieldZone field, int lane)
        {
            var slot = field.SlotAt(true, lane);
            var unit = slot != null ? slot.OccupantView : null;
            return unit != null && !unit.IsDead ? unit : null;
        }

        /// <summary>
        /// 맞는 쪽의 방어 효과(현재는 Wall)를 반영한 최종 피해. 0 아래로는 내려가지 않는다.
        /// 공격하는 쪽이 Ranged(원거리)면 Wall 효과를 무시한다.
        /// </summary>
        static int DamageAfterDefense(CardView attacker, CardView target, int damage)
        {
            bool ignoresWall = attacker.data.HasKeyword(CardKeywords.Ranged);
            if (!ignoresWall && target.data != null && target.data.HasKeyword(CardKeywords.Wall))
                damage -= CardKeywords.WallDamageReduction;
            return damage < 0 ? 0 : damage;
        }

        /// <summary>전열에서 체력이 0 이하인 카드를 제거하고, 그 카드 뒤(같은 레인 후열)의 장비도 함께 치운다.</summary>
        static void RemoveDeadUnits(FieldZone field)
        {
            if (field == null || field.frontRow == null) return;
            for (int lane = 0; lane < field.LaneCount; lane++)
            {
                var front = field.SlotAt(true, lane);
                var unit = front != null ? front.OccupantView : null;
                if (unit == null || !unit.IsDead) continue;
                field.DestroyCardAt(front);
                field.DestroyCardAt(field.SlotAt(false, lane)); // 뒤에 붙어 있던 장비도 같이 사라진다
            }
        }
    }
}
