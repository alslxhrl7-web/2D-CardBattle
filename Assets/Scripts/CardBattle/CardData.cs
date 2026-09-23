using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 카드 한 장의 정적(불변) 데이터. 유닛의 이름/진영/스탯/키워드/아트 등 "이 카드가 무엇인가"를 정의한다.
    /// 실제로 씬에 보이는 카드는 이 데이터를 참조하는 CardView(런타임 표시용 컴포넌트)로 만들어지며,
    /// Assets/CardData/Joseon, Assets/CardData/Qing 폴더에 카드 1장당 asset 파일 1개씩 존재한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "CardBattle/CardData")]
    public class CardData : ScriptableObject
    {
        public string cardNameKo;              // 한글 카드명 (네임플레이트 하단 표기)
        public string cardNameEn;               // 영문 카드명 (네임플레이트 상단 표기 + GameObject 이름 "Card_<영문명>"으로도 쓰임)
        public Faction faction;                 // 소속 진영 — CardView가 프레임/네임플레이트 색상을 여기서 결정
        public CardKind cardKind = CardKind.Unit;
        public Rarity rarity = Rarity.Common;   // 희귀도 — 리본 표시 여부/색/등급 라벨 결정
        public int cost;                        // 군력(마나) 비용. CardManager.TryPlaceCard가 이 값을 소모 시도한다
        public int attack;                      // 공격력 (카드 우하단 빨간 다이아몬드 배지)
        public int health;                      // 체력 (카드 우하단 파란 원형 배지)
        [TextArea] public string keywordText;   // "키워드명: 설명" 형식. CardView가 콜론(:) 기준으로 나눠 능력 박스에 표시
        [TextArea] public string flavorText;    // 카드 맨 아래 플레이버 텍스트(설정용 짧은 문구, 전투에 영향 없음)
        public Sprite portrait;                 // 카드 초상화. 원본 텍스처마다 PPU/해상도가 달라도 CardView.Setup()에서 항상 같은 크기로 보이도록 자동 스케일링됨
    }
}
