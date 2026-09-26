namespace CardBattle
{
    /// <summary>카드가 속한 진영. 조선(수성)과 청(기동전)이 현재 메인이고, 명나라는 시즌2 확장용으로 예약된 값.</summary>
    public enum Faction
    {
        Joseon, // 조선: 성벽·방어 중심
        Qing,   // 청(후금): 기병·속도 중심
        Ming    // 명나라: 확장용(아직 카드 없음)
    }

    /// <summary>카드의 큰 분류.</summary>
    public enum CardKind
    {
        Unit,      // 장수(유닛): 필드에 내는 카드
        Spell,     // 전술(스펠): 아직 미구현
        Weapon,    // 병기(무기): 아직 미구현
        Formation  // 진(陣): 지속효과 구조물/트랩, 아직 미구현
    }

    /// <summary>
    /// 카드 희귀도. 값이 클수록 높은 등급이다.
    /// 카드팩의 "이 등급 이상" 비교에 이 순서를 그대로 쓰므로 순서를 바꾸지 말 것.
    /// </summary>
    public enum Rarity
    {
        Common,    // 일반: 리본 표시 없음, 덱에 최대 3장
        Elite,     // 엘리트: 덱에 최대 2장
        Legendary, // 전설: 덱에 최대 1장
        Hero       // 히어로: 덱에 최대 1장
    }

    /// <summary>
    /// 대전의 한쪽 편. 플레이어/상대가 똑같이 하는 일(군력, 체력, 드로우 등)을
    /// "플레이어면 ~, 아니면 ~"로 두 번 쓰지 않고 Side 하나로 처리하기 위해 사용한다.
    /// </summary>
    public enum Side
    {
        Player, // 플레이어(조선, 마우스로 조작)
        Enemy   // 상대(청, AI가 자동으로 플레이)
    }

    /// <summary>Side에 붙여 쓰는 편의 함수 모음.</summary>
    public static class SideExtensions
    {
        /// <summary>반대편을 돌려준다. (Player ↔ Enemy)</summary>
        public static Side Opponent(this Side side)
        {
            return side == Side.Player ? Side.Enemy : Side.Player;
        }
    }
}
