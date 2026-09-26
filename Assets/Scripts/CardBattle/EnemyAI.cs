using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 상대(청) AI의 "판단"만 담당한다. 실제로 카드를 옮기는 "실행"은 CardManager가 한다.
    /// 판단과 실행을 나눠두었기 때문에, AI를 똑똑하게 바꾸고 싶으면 이 파일만 고치면 된다.
    ///
    /// 현재 전략(단순 탐욕): 지금 군력으로 낼 수 있는 카드 중 가장 비싼 카드부터 낸다.
    /// 전술은 바로 쓴다.
    /// 유닛·진은 전열에만 놓고, 장비는 공격력이 가장 높은 내 전열 카드 뒤(후열)에 놓는다.
    /// </summary>
    public static class EnemyAI
    {
        /// <summary>
        /// 손패에서 이번에 낼 카드의 번호(인덱스)를 고른다. 낼 수 있는 카드가 없으면 -1.
        /// 비용이 같은 카드가 여러 장이면 손패 앞쪽 카드를 고른다.
        /// </summary>
        public static int ChooseCardToPlay(IList<CardData> hand, int availableMana)
        {
            int best = -1; // 지금까지 찾은 가장 좋은 카드 번호 (-1 = 아직 없음)
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card == null || card.cost > availableMana) continue;   // 못 내는 카드는 건너뜀
                if (best < 0 || card.cost > hand[best].cost) best = i;       // 더 비싼 카드면 교체
            }
            return best;
        }

        /// <summary>
        /// 카드 종류까지 고려해서 이번에 낼 카드를 고른다. 낼 수 없는 카드는 건너뛴다.
        ///   유닛·진: 전열 빈 칸이 있어야 함   장비: 앞에 카드가 있는 후열 빈 칸이 있어야 함   전술: 언제나 가능
        /// 그중 가장 비싼 카드를 고른다(같으면 손패 앞쪽). 없으면 -1.
        /// </summary>
        public static int ChooseCardToPlay(IList<CardData> hand, int availableMana, bool hasEmptyFront, bool hasEquipSlot)
        {
            int best = -1;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card == null || card.cost > availableMana) continue;
                if (card.IsFieldCard && !hasEmptyFront) continue;   // 놓을 전열 칸이 없음
                if (card.IsEquipment && !hasEquipSlot) continue;    // 장비를 놓을 후열 칸이 없음
                if (best < 0 || card.cost > hand[best].cost) best = i;
            }
            return best;
        }

        /// <summary>
        /// 장비를 놓을 후열 칸을 고른다: 비어 있고, 같은 레인 전열에 카드가 있는 칸 중 그 카드의 공격력이 가장 높은 칸.
        /// 그런 칸이 없으면 null.
        /// </summary>
        public static FieldSlot ChooseEquipSlot(FieldZone field)
        {
            FieldSlot best = null;
            int bestAttack = -1;
            for (int lane = 0; lane < field.LaneCount; lane++)
            {
                var back = field.SlotAt(false, lane);
                var front = field.SlotAt(true, lane);
                var unit = front != null ? front.OccupantView : null;
                if (back == null || !back.IsEmpty || unit == null || unit.IsDead) continue;
                if (unit.currentAttack > bestAttack) { best = back; bestAttack = unit.currentAttack; }
            }
            return best;
        }

        /// <summary>유닛·진을 놓을 칸을 고른다: 전열의 가장 왼쪽 빈 칸. 없으면 null. (후열은 장비 전용)</summary>
        public static FieldSlot ChooseSlot(FieldZone field, CardData card)
        {
            return field.GetFirstEmpty(true);
        }
    }
}
