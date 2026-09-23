using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 하나의 "덱 구성"을 담는 에셋. 실제 카드 오브젝트가 아니라 "어떤 카드를 몇 장 넣을지"만
    /// 정의하는 설계도이고, 게임 시작 시 CardManager가 이 데이터를 실제 카드 낱장 리스트로 펼친 뒤
    /// 셔플해서 "드로우 더미"를 만든다. 이렇게 데이터(덱 설계도)와 런타임 상태(셔플된 더미)를
    /// 분리해두면, 같은 덱 에셋으로 여러 판을 다시 시작해도 매번 새로 셔플된다.
    ///
    /// 새 덱을 만들려면: 프로젝트 창에서 우클릭 → Create → CardBattle → Deck Data.
    /// 그 다음 인스펙터에서 entries 리스트에 카드(CardData)와 매수(count)를 채워 넣으면 된다.
    /// 이것이 이 프로젝트에서 "덱 빌딩"에 해당한다 — 코드 수정 없이 에셋만으로 새로운 덱 조합을 만들 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDeck", menuName = "CardBattle/Deck Data")]
    public class DeckData : ScriptableObject
    {
        /// <summary>덱에 들어가는 카드 한 종류 + 매수(중복 포함 몇 장인지).</summary>
        [System.Serializable]
        public class Entry
        {
            public CardData card;
            [Min(1)] public int count = 1;
        }

        public string deckName;
        public Faction faction;
        public List<Entry> entries = new List<Entry>();

        /// <summary>덱에 포함된 카드 총 장수(중복 포함)를 계산한다. 인스펙터에서 확인용으로도 쓸 수 있다.</summary>
        public int TotalCount()
        {
            int total = 0;
            foreach (var e in entries)
                if (e != null && e.card != null) total += Mathf.Max(1, e.count);
            return total;
        }

        /// <summary>
        /// entries(카드 + 매수)를 실제 카드 한 장 한 장의 평평한 리스트로 펼친다.
        /// 이 리스트를 셔플한 것이 곧 한 판의 "드로우 더미" 초기 상태가 된다.
        /// 호출할 때마다 새 리스트를 만들어 반환하므로, 원본 entries를 실수로 건드릴 위험이 없다.
        /// </summary>
        public List<CardData> BuildCardList()
        {
            var list = new List<CardData>();
            foreach (var e in entries)
            {
                if (e == null || e.card == null) continue;
                int c = Mathf.Max(1, e.count);
                for (int i = 0; i < c; i++) list.Add(e.card);
            }
            return list;
        }
    }
}
