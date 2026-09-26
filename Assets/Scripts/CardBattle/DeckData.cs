using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 하나의 "덱 구성"을 담는 에셋. 실제 카드 오브젝트가 아니라 "어떤 카드를 몇 장 넣을지"만
    /// 정의하는 설계도이고, 게임 시작 시 CardManager가 이 데이터를 실제 카드 낱장 리스트로 펼친 뒤
    /// 섞어서 "드로우 더미"를 만든다. 이렇게 데이터(덱 설계도)와 게임 중 상태(섞인 더미)를
    /// 분리해두면, 같은 덱 에셋으로 여러 판을 다시 시작해도 매번 새로 섞인다.
    ///
    /// 새 덱을 만들려면: 프로젝트 창에서 우클릭 → Create → CardBattle → Deck Data.
    /// 그 다음 인스펙터에서 entries 리스트에 카드(CardData)와 매수(count)를 채워 넣으면 된다.
    /// 게임 화면의 "덱 편집" 버튼으로도 플레이어 덱을 바꿀 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDeck", menuName = "CardBattle/Deck Data")]
    public class DeckData : ScriptableObject
    {
        /// <summary>덱에 들어가는 카드 한 종류 + 매수(같은 카드 몇 장인지).</summary>
        [System.Serializable]
        public class Entry
        {
            public CardData card;          // 넣을 카드
            [Min(1)] public int count = 1; // 넣을 장수
        }

        public string deckName;                          // 덱 이름 (표시용)
        public Faction faction;                          // 덱의 진영 (표시용)
        public List<Entry> entries = new List<Entry>();  // 카드와 매수 목록

        /// <summary>덱에 들어있는 카드 총 장수(중복 포함)를 계산한다.</summary>
        public int TotalCount()
        {
            int total = 0;
            foreach (var e in entries)
                if (e != null && e.card != null) total += Mathf.Max(1, e.count);
            return total;
        }

        /// <summary>
        /// entries(카드 + 매수)를 카드 한 장 한 장의 평평한 리스트로 펼친다.
        /// 이 리스트를 섞은 것이 한 판의 "드로우 더미" 처음 상태가 된다.
        /// 호출할 때마다 새 리스트를 만들어 돌려주므로 원본 entries가 바뀔 걱정이 없다.
        /// </summary>
        public List<CardData> BuildCardList()
        {
            var list = new List<CardData>();
            foreach (var e in entries)
            {
                if (e == null || e.card == null) continue; // 빈 칸은 건너뜀
                int c = Mathf.Max(1, e.count);
                for (int i = 0; i < c; i++) list.Add(e.card);
            }
            return list;
        }
    }
}
