using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// CardPackData 설정대로 실제 카드를 뽑는 로직. 화면(UI)과 완전히 분리된 정적 클래스라서
    /// 확률 규칙을 바꾸거나(중복 방지, 천장 등) 다른 화면(상점, 보상)에서 재사용할 때 이 파일만 보면 된다.
    ///
    /// 뽑는 순서(한 장마다):
    ///   1) rarityWeights 가중치로 희귀도를 고른다. (마지막 장이 보장 슬롯이면 guaranteedMinRarity 이상 중에서만)
    ///   2) cardPool에서 그 희귀도의 카드 중 하나를 무작위로 고른다.
    ///   카드 풀에 없는 희귀도는 자동으로 제외되므로, 로스터가 작아도 오류 없이 항상 결과가 나온다.
    /// </summary>
    public static class CardPackOpener
    {
        /// <summary>팩 하나를 열어 뽑힌 카드들을 돌려준다(같은 카드가 여러 장 나올 수 있음).</summary>
        public static List<CardData> Open(CardPackData pack)
        {
            var result = new List<CardData>();
            if (pack == null || pack.cardPool == null || pack.cardPool.Count == 0) return result; // 뽑을 카드가 없음

            for (int i = 0; i < pack.cardsPerPack; i++)
            {
                bool guaranteedSlot = pack.guaranteeLastSlot && i == pack.cardsPerPack - 1; // 마지막 장이 보장 슬롯인지
                Rarity minRarity = guaranteedSlot ? pack.guaranteedMinRarity : Rarity.Common;

                var card = PickCard(pack, RollRarity(pack, minRarity));
                if (card != null) result.Add(card);
            }
            return result;
        }

        /// <summary>
        /// minRarity 이상이면서 카드 풀에 카드가 있는 희귀도들 중에서 가중치대로 하나를 고른다.
        /// 조건을 만족하는 희귀도가 하나도 없으면 조건 없이(Common 이상) 다시 고르고,
        /// 그래도 없으면 카드 풀 첫 카드의 희귀도를 쓴다.
        /// </summary>
        public static Rarity RollRarity(CardPackData pack, Rarity minRarity)
        {
            // 뽑을 수 있는 항목들의 가중치 합
            float total = 0f;
            foreach (var rw in pack.rarityWeights)
                if (IsRollable(pack, rw, minRarity)) total += rw.weight;

            if (total <= 0f)
            {
                if (minRarity != Rarity.Common) return RollRarity(pack, Rarity.Common); // 조건을 풀고 다시
                return pack.cardPool[0] != null ? pack.cardPool[0].rarity : Rarity.Common;
            }

            // 0~total 사이 무작위 값이 어느 구간에 떨어지는지로 희귀도를 정한다
            float roll = Random.value * total;
            float accumulated = 0f;
            Rarity last = minRarity;
            foreach (var rw in pack.rarityWeights)
            {
                if (!IsRollable(pack, rw, minRarity)) continue;
                accumulated += rw.weight;
                last = rw.rarity;
                if (roll < accumulated) return rw.rarity;
            }
            return last; // Random.value가 정확히 1.0일 때 대비
        }

        /// <summary>이 항목이 지금 뽑힐 수 있는지: 가중치가 있고, 최소 등급 이상이고, 풀에 카드가 있어야 한다.</summary>
        static bool IsRollable(CardPackData pack, CardPackData.RarityWeight rw, Rarity minRarity)
        {
            return rw.weight > 0f && rw.rarity >= minRarity && pack.PoolFor(rw.rarity).Count > 0;
        }

        /// <summary>해당 희귀도의 카드 중 하나를 무작위로 고른다.</summary>
        static CardData PickCard(CardPackData pack, Rarity rarity)
        {
            var pool = pack.PoolFor(rarity);
            if (pool.Count == 0) pool = pack.cardPool; // 안전장치: 해당 희귀도가 없으면 전체 풀에서
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
