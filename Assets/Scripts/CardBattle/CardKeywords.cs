namespace CardBattle
{
    /// <summary>
    /// 전투 로직이 실제로 인식하는 키워드 이름 모음.
    /// CardData.keywordText는 "키워드명: 설명" 형식인데, 그중 "키워드명" 부분에 아래 단어가 들어있으면
    /// 해당 효과가 적용된다(대소문자 무시). 예: "Ranged: 후열에서 공격 가능", "Ranged, Hit and Run: ..."
    ///
    /// 새 키워드 효과를 추가하는 순서:
    ///   1) 여기에 이름 상수를 추가한다.
    ///   2) 효과가 발동하는 곳(대부분 LaneCombat.cs)에서 card.HasKeyword(CardKeywords.이름)으로 확인한다.
    /// 여기에 없는 키워드(Charge, Swarm, Rally 등)는 아직 카드 설명으로만 표시되고 효과는 없다.
    /// </summary>
    public static class CardKeywords
    {
        /// <summary>원거리: 후열에 있어도 공격할 수 있다. (이 키워드가 없으면 후열 유닛은 공격하지 않는다)</summary>
        public const string Ranged = "Ranged";

        /// <summary>방벽: 전열에 있을 때 받는 피해가 WallDamageReduction만큼 줄어든다.</summary>
        public const string Wall = "Wall";

        /// <summary>방벽(Wall)이 줄여주는 피해량.</summary>
        public const int WallDamageReduction = 1;
    }
}
