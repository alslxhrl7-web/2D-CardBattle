namespace CardBattle
{
    /// <summary>
    /// 전투 로직이 실제로 인식하는 키워드 이름 모음.
    /// CardData.keywordText는 "키워드명: 설명" 형식인데, 그중 "키워드명" 부분에 아래 단어가 들어있으면
    /// 해당 효과가 적용된다(대소문자 무시). 예: "Ranged: 상대 Wall 무시", "Ranged, Hit and Run: ..."
    ///
    /// 새 키워드 효과를 추가하는 순서:
    ///   1) 여기에 이름 상수를 추가한다.
    ///   2) 효과가 발동하는 곳(대부분 LaneCombat.cs)에서 card.HasKeyword(CardKeywords.이름)으로 확인한다.
    ///
    /// 효과가 일어나는 곳:
    ///   전투 계산(LaneCombat.cs): Ranged, Wall, Hit and Run, Momentum, Chain Cavalry, Plunder(드로우 장수만 셈), Rally
    ///   카드를 낼 때(CardManager.cs): Swarm(비용), Charge(바로 공격)
    ///   피해를 받을 때(CardView.cs): Last Stand
    /// 온라인 대전은 두 컴퓨터가 같은 계산을 해야 하므로, 키워드 효과에 무작위를 쓰지 않는다.
    /// </summary>
    public static class CardKeywords
    {
        /// <summary>원거리: 공격할 때 상대 Wall(피해 감소)을 무시한다. (공격은 누구나 전열에서만 한다)</summary>
        public const string Ranged = "Ranged";

        /// <summary>방벽: 전열에 있을 때 받는 피해가 WallDamageReduction만큼 줄어든다.</summary>
        public const string Wall = "Wall";

        /// <summary>방벽(Wall)이 줄여주는 피해량.</summary>
        public const int WallDamageReduction = 1;

        /// <summary>치고 빠지기: 같은 레인 적 전열 카드에게 받는 피해가 HitAndRunReduction만큼 준다(Ranged도 못 무시함, 전술 피해는 그대로).</summary>
        public const string HitAndRun = "Hit and Run";
        public const int HitAndRunReduction = 1;

        /// <summary>군집(의병): 이번 판에 같은 카드를 낸 수만큼 비용이 준다.</summary>
        public const string Swarm = "Swarm";

        /// <summary>돌격: 낼 때 같은 레인 적 전열(없으면 적 장수)을 바로 한 번 공격한다. 반격은 없다.</summary>
        public const string Charge = "Charge";

        /// <summary>기세: 전투 때 이 카드보다 왼쪽 레인에서 공격한 아군 1기당 공격력 +MomentumBonus.</summary>
        public const string Momentum = "Momentum";
        public const int MomentumBonus = 1;

        /// <summary>연쇄 돌격: 전투에서 같은 레인 적 전열을 쓰러뜨리면 적 장수도 한 번 더 공격한다.</summary>
        public const string ChainCavalry = "Chain Cavalry";

        /// <summary>약탈: 전투에서 적 카드를 쓰러뜨리면 카드를 1장 뽑는다.</summary>
        public const string Plunder = "Plunder";

        /// <summary>결집: 쓰러지면 체력이 가장 낮은 아군 1기(같으면 왼쪽)에게 +RallyBonus/+RallyBonus.</summary>
        public const string Rally = "Rally";
        public const int RallyBonus = 1;

        /// <summary>배수진: 체력이 LastStandHealth 미만이 되면 공격력 +LastStandBonus (한 번만).</summary>
        public const string LastStand = "Last Stand";
        public const int LastStandHealth = 3;
        public const int LastStandBonus = 3;
    }
}
