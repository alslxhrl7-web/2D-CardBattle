using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 상대(청) AI의 "판단"만 담당한다. 실제로 카드를 옮기는 "실행"은 CardManager가 한다.
    /// 판단과 실행을 나눠두었기 때문에, AI를 똑똑하게 바꾸고 싶으면 이 파일만 고치면 된다.
    ///
    /// 현재 전략(단순 탐욕): 지금 군력으로 낼 수 있는 카드 중 가장 비싼 카드부터 낸다.
    /// 근접 유닛은 전열부터, Ranged(원거리) 유닛은 후열부터 채운다
    /// (원거리 유닛은 후열에서도 공격할 수 있으므로 전열 자리를 근접 유닛에게 양보).
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

        /// <summary>카드를 놓을 슬롯을 고른다. 빈 슬롯이 하나도 없으면 null.</summary>
        public static FieldSlot ChooseSlot(FieldZone field, CardData card)
        {
            bool preferBack = card != null && card.HasKeyword(CardKeywords.Ranged); // 원거리면 후열 우선
            var first = field.GetFirstEmpty(!preferBack);                           // 원하는 줄에서 먼저 찾고
            return first != null ? first : field.GetFirstEmpty(preferBack);        // 없으면 반대 줄에서 찾는다
        }
    }
}
