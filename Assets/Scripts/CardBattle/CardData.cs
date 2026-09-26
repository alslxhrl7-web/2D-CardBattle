using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 카드 한 장의 정적(불변) 데이터. 유닛의 이름/진영/스탯/키워드/아트 등 "이 카드가 무엇인가"를 정의한다.
    /// 실제로 씬에 보이는 카드는 이 데이터를 참조하는 CardView(런타임 표시용 컴포넌트)로 만들어지며,
    /// Assets/CardData/Joseon, Assets/CardData/Qing 폴더에 카드 1장당 asset 파일 1개씩 존재한다.
    ///
    /// 새 카드 추가: 프로젝트 창 우클릭 → Create → CardBattle → CardData → 아래 값들을 채운다.
    /// (카드팩에 넣으려면 해당 CardPackData 에셋에서 "자동 채우기"를 한 번 더 누르면 된다)
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "CardBattle/CardData")]
    public class CardData : ScriptableObject
    {
        public string cardNameKo;              // 한글 카드명 (네임플레이트 하단 표기)
        public string cardNameEn;              // 영문 카드명 (네임플레이트 상단 표기 + GameObject 이름 "Card_<영문명>"으로도 쓰임)
        public Faction faction;                // 소속 진영 — 프레임/네임플레이트 색상이 여기서 결정됨
        public CardKind cardKind = CardKind.Unit;
        public Rarity rarity = Rarity.Common;  // 희귀도 — 리본 표시, 덱 최대 매수, 팩 확률에 쓰임
        public int cost;                       // 군력(마나) 비용
        public int attack;                     // 공격력
        public int health;                     // 체력(최대치). 전투 중 깎이는 실제 체력은 CardView.currentHealth
        [TextArea] public string keywordText;  // "키워드명: 설명" 형식. 키워드명에 CardKeywords의 단어가 있으면 전투 효과가 적용됨
        [TextArea] public string flavorText;   // 카드 맨 아래 플레이버 텍스트(전투에 영향 없음)
        public Sprite portrait;                // 카드 초상화. 원본 크기와 무관하게 CardView가 항상 같은 크기로 맞춘다

        /// <summary>keywordText의 콜론(:) 앞부분 = 키워드명. 콜론이 없으면 전체가 키워드명.</summary>
        public string KeywordName
        {
            get
            {
                if (string.IsNullOrEmpty(keywordText)) return "";
                int idx = keywordText.IndexOf(':');
                return (idx >= 0 ? keywordText.Substring(0, idx) : keywordText).Trim();
            }
        }

        /// <summary>keywordText의 콜론(:) 뒷부분 = 효과 설명. 콜론이 없으면 빈 문자열.</summary>
        public string KeywordDescription
        {
            get
            {
                if (string.IsNullOrEmpty(keywordText)) return "";
                int idx = keywordText.IndexOf(':');
                return idx >= 0 ? keywordText.Substring(idx + 1).Trim() : "";
            }
        }

        /// <summary>
        /// 키워드명에 해당 키워드가 들어있는지(대소문자 무시). "Ranged, Hit and Run"처럼 여러 개가
        /// 쉼표로 나열된 경우도 각각 인식한다. 사용 예: card.HasKeyword(CardKeywords.Ranged)
        /// </summary>
        public bool HasKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;
            foreach (var part in KeywordName.Split(','))
            {
                if (string.Equals(part.Trim(), keyword, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
