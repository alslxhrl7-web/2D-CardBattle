using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 카드팩 설계도. 어떤 카드들이(cardPool) 어떤 확률로(rarityWeights) 몇 장(cardsPerPack) 나오는지를
    /// 전부 인스펙터에서 정한다. 코드에 확률이 하드코딩되어 있지 않으므로, 확률 조정이나 새 테마 팩
    /// (예: 청나라팩)은 에셋만 만들거나 숫자만 바꾸면 된다.
    ///
    /// 새 팩 만들기: 프로젝트 창 우클릭 → Create → CardBattle → Card Pack Data
    ///   → autoFillFaction에 진영을 고르고, 인스펙터 우상단 ⋮(또는 에셋 이름 우클릭) → "자동 채우기"
    ///   → 그 진영의 카드가 전부 cardPool에 들어간다(카드가 추가되면 다시 누르면 갱신됨).
    /// </summary>
    [CreateAssetMenu(menuName = "CardBattle/Card Pack Data", fileName = "NewCardPack")]
    public class CardPackData : ScriptableObject
    {
        /// <summary>희귀도 하나와 그 가중치(뽑힐 상대적 확률) 한 쌍.</summary>
        [Serializable]
        public struct RarityWeight
        {
            public Rarity rarity; // 희귀도
            [Tooltip("상대적 가중치. 합이 100일 필요는 없고, 전체 합에 대한 비율로 계산된다.")]
            [Min(0)] public float weight; // 가중치 (클수록 잘 나옴)

            /// <summary>희귀도와 가중치를 한 번에 지정해서 만든다.</summary>
            public RarityWeight(Rarity rarity, float weight)
            {
                this.rarity = rarity;
                this.weight = weight;
            }
        }

        [Header("팩 정보")]
        public string packName = "새 카드팩";   // 화면에 표시할 팩 이름
        [Min(1)] public int cardsPerPack = 5;   // 팩 하나에서 나오는 장수

        [Header("카드 풀 (이 팩에서 나올 수 있는 카드)")]
        public List<CardData> cardPool = new List<CardData>(); // 나올 수 있는 카드 목록

        [Header("희귀도별 가중치 (카드 풀에 없는 희귀도는 자동 제외)")]
        public List<RarityWeight> rarityWeights = new List<RarityWeight>
        {
            new RarityWeight(Rarity.Common, 70f),    // 일반 70
            new RarityWeight(Rarity.Elite, 22f),     // 엘리트 22
            new RarityWeight(Rarity.Legendary, 6f),  // 전설 6
            new RarityWeight(Rarity.Hero, 2f),       // 히어로 2
        };

        [Header("보장 슬롯")]
        [Tooltip("체크하면 마지막 한 장은 아래 희귀도 '이상'이 나온다(레어 이상 확정 슬롯).")]
        public bool guaranteeLastSlot = true;               // 마지막 장 보장 사용 여부
        public Rarity guaranteedMinRarity = Rarity.Elite;   // 보장되는 최소 희귀도

        [Header("자동 채우기 (에디터 전용)")]
        [Tooltip("에셋 메뉴의 '자동 채우기'를 누르면 이 진영의 CardData 전부로 cardPool을 채운다.")]
        public Faction autoFillFaction = Faction.Joseon; // 자동 채우기에 쓸 진영

        /// <summary>cardPool 중 지정한 희귀도의 카드들만 모아서 돌려준다.</summary>
        public List<CardData> PoolFor(Rarity rarity)
        {
            var list = new List<CardData>();
            foreach (var card in cardPool)
                if (card != null && card.rarity == rarity) list.Add(card);
            return list;
        }

        /// <summary>
        /// 희귀도별 실제 등장 확률(0~1). 카드 풀에 없는 희귀도는 0으로 계산된다.
        /// 확률을 조정한 뒤 결과를 확인할 때 쓴다(메뉴 "카드팩 확률 확인"). 보장 슬롯은 반영하지 않음.
        /// </summary>
        public float ChanceOf(Rarity rarity)
        {
            float total = 0f, mine = 0f; // 전체 가중치 합, 이 희귀도의 가중치
            foreach (var rw in rarityWeights)
            {
                if (rw.weight <= 0f || PoolFor(rw.rarity).Count == 0) continue; // 실제로 나올 수 없는 항목 제외
                total += rw.weight;
                if (rw.rarity == rarity) mine += rw.weight;
            }
            return total > 0f ? mine / total : 0f;
        }

#if UNITY_EDITOR
        /// <summary>프로젝트 안의 CardData 중 autoFillFaction 진영인 것을 전부 찾아 cardPool을 채운다(에디터 전용).</summary>
        [ContextMenu("자동 채우기 (autoFillFaction 진영의 카드 전체)")]
        public void AutoFillFromFaction()
        {
            cardPool.Clear();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CardData"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null && card.faction == autoFillFaction) cardPool.Add(card);
            }
            cardPool.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // 이름순 정렬(보기 편하게)
            UnityEditor.EditorUtility.SetDirty(this); // 바뀐 내용을 저장 대상으로 표시
            Debug.Log(string.Format("[CardPackData] {0}: {1} 카드 {2}장으로 채웠습니다.", name, autoFillFaction, cardPool.Count));
        }
#endif
    }
}
