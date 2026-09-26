using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 카드 한 장의 정적(변하지 않는) 데이터. 이름/진영/스탯/키워드/그림 등 "이 카드가 무엇인가"를 정의한다.
    /// 실제로 화면에 보이는 카드는 이 데이터를 참조하는 CardView로 만들어지며,
    /// Assets/CardData/Joseon, Assets/CardData/Qing 폴더에 카드 1장당 에셋 파일 1개씩 있다.
    ///
    /// 새 카드 추가: 프로젝트 창 우클릭 → Create → CardBattle → CardData → 아래 값들을 채운다.
    /// (카드팩에도 넣으려면 해당 CardPackData 에셋에서 "자동 채우기"를 한 번 더 누르면 된다)
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "CardBattle/CardData")]
    public class CardData : ScriptableObject
    {
        public string cardNameKo;              // 한글 카드명 (네임플레이트 아래쪽에 표시)
        public string cardNameEn;              // 영문 카드명 (네임플레이트 위쪽에 표시, 오브젝트 이름 "Card_영문명"에도 쓰임)
        public Faction faction;                // 소속 진영 — 카드 테두리/네임플레이트 색이 여기서 정해짐
        public CardKind cardKind = CardKind.Unit; // 카드 종류: 유닛 / 전술 / 장비 / 진
        public Rarity rarity = Rarity.Common;  // 희귀도 — 리본 표시, 덱 최대 매수, 팩 확률에 쓰임
        public int cost;                       // 군력(마나) 비용 — 이만큼 군력이 있어야 필드에 낼 수 있음
        public int attack;                     // 공격력 — 레인 전투에서 상대에게 주는 피해
        public int health;                     // 체력(최대치). 전투 중 실제로 깎이는 체력은 CardView.currentHealth
                                               // (장비 카드는 attack/health가 "붙인 유닛에게 더해 줄 값", 전술 카드는 쓰지 않음)
        public SpellEffect spellEffect;        // 전술 카드의 효과 종류 (전술이 아니면 무시)
        public int effectValue;                // 전술 카드 효과의 크기 (피해량, 드로우 장수 등)
        [TextArea] public string keywordText;  // "키워드명: 설명" 형식. 키워드명에 CardKeywords의 단어가 있으면 전투 효과가 적용됨
        [TextArea] public string flavorText;   // 카드 맨 아래 짧은 설정 문구 (전투에 영향 없음)
        public Sprite portrait;                // 카드 초상화. 원본 크기와 상관없이 CardView가 항상 같은 크기로 맞춘다

        /// <summary>필드 빈 칸에 내는 카드인지 (유닛, 진).</summary>
        public bool IsFieldCard { get { return cardKind == CardKind.Unit || cardKind == CardKind.Formation; } }

        /// <summary>유닛에게 붙이는 장비 카드인지.</summary>
        public bool IsEquipment { get { return cardKind == CardKind.Weapon; } }

        /// <summary>즉시 효과를 내고 사라지는 전술 카드인지.</summary>
        public bool IsSpell { get { return cardKind == CardKind.Spell; } }

        /// <summary>keywordText에서 콜론(:) 앞부분 = 키워드명. 콜론이 없으면 전체가 키워드명.</summary>
        public string KeywordName
        {
            get
            {
                if (string.IsNullOrEmpty(keywordText)) return "";
                int idx = keywordText.IndexOf(':'); // 첫 번째 콜론 위치
                return (idx >= 0 ? keywordText.Substring(0, idx) : keywordText).Trim();
            }
        }

        /// <summary>keywordText에서 콜론(:) 뒷부분 = 효과 설명. 콜론이 없으면 빈 문자열.</summary>
        public string KeywordDescription
        {
            get
            {
                if (string.IsNullOrEmpty(keywordText)) return "";
                int idx = keywordText.IndexOf(':'); // 첫 번째 콜론 위치
                return idx >= 0 ? keywordText.Substring(idx + 1).Trim() : "";
            }
        }

        /// <summary>
        /// 키워드명에 해당 키워드가 들어있는지 확인한다(대소문자 무시).
        /// "Ranged, Hit and Run"처럼 쉼표로 여러 개가 나열된 경우도 각각 인식한다.
        /// 사용 예: card.HasKeyword(CardKeywords.Ranged)
        /// </summary>
        public bool HasKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;
            // 쉼표로 나눠서 하나씩 비교한다
            foreach (var part in KeywordName.Split(','))
            {
                if (string.Equals(part.Trim(), keyword, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
