namespace CardBattle
{
    /// <summary>카드가 속한 진영. 조선(수성)과 청(기동전)이 현재 메인이고, 명나라는 시즌2 확장용으로 예약된 값.</summary>
    public enum Faction { Joseon, Qing, Ming }

    /// <summary>카드의 큰 분류: 장수(유닛) / 전술(스펠) / 병기(무기) / 진(지속효과 구조물, 트랩 등).</summary>
    public enum CardKind { Unit, Spell, Weapon, Formation }

    /// <summary>카드 희귀도. 값이 클수록 높은 등급이다(카드팩의 "이상" 비교에 이 순서를 사용하므로 순서를 바꾸지 말 것).</summary>
    public enum Rarity { Common, Elite, Legendary, Hero }

    /// <summary>
    /// 대전의 한쪽 편. 플레이어/상대로 똑같이 처리되는 로직(마나, 체력, 드로우 등)을
    /// "if 플레이어면 ~ else ~"로 두 번 쓰지 않고 Side 하나로 다루기 위해 사용한다.
    /// </summary>
    public enum Side { Player, Enemy }

    public static class SideExtensions
    {
        /// <summary>반대편을 돌려준다. (Player ↔ Enemy)</summary>
        public static Side Opponent(this Side side)
        {
            return side == Side.Player ? Side.Enemy : Side.Player;
        }
    }
}
