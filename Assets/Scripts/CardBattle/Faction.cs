namespace CardBattle
{
    /// <summary>카드가 속한 진영. 조선(수성)과 청(기동전)이 현재 메인이고, 명나라는 시즌2 확장용으로 예약된 값.</summary>
    public enum Faction { Joseon, Qing, Ming }

    /// <summary>카드의 큰 분류: 장수(유닛) / 전술(스펠) / 병기(무기) / 진(지속효과 구조물, 트랩 등).</summary>
    public enum CardKind { Unit, Spell, Weapon, Formation }

    /// <summary>카드 희귀도. CardView가 리본 색상/등급 라벨(HERO/LEGENDARY/ELITE)을 여기서 결정한다. Common은 리본 자체를 숨김.</summary>
    public enum Rarity { Common, Elite, Legendary, Hero }
}
