namespace CardBattle
{
    /// <summary>
    /// 게임 규칙 수치를 한 곳에 모아둔 파일.
    /// "체력을 40으로 바꾸고 싶다", "손패 최대치를 8장으로" 같은 밸런스 조정은 이 파일만 고치면 된다.
    /// (카드 한 장 한 장의 스탯은 CardData 에셋, 덱 구성은 DeckData 에셋, 팩 확률은 CardPackData 에셋에서 조정)
    /// </summary>
    public static class GameRules
    {
        // ---- 손패 / 덱 ----
        public const int StartingHandSize = 5;   // 게임 시작 시 뽑는 카드 수 (CardManager 인스펙터 값이 있으면 그쪽이 우선)
        public const int MaxHandSize = 10;       // 손패 최대 장수. 가득 찬 상태에서 드로우하면 그 카드는 버려진다(번)
        public const int MinDeckSize = 10;       // 덱 빌더에서 저장할 수 있는 최소 장수

        // ---- 카드 내기 ----
        public const int FirstTurnCardLimit = 1; // 첫 턴(1턴)에 각자 낼 수 있는 카드 수 (0으로 바꾸면 제한 없음)

        // ---- 군력(마나) ----
        public const int StartingMana = 1;       // 1턴의 최대 군력
        public const int ManaCap = 10;           // 군력 최대치. 턴이 계속 늘어도 이 값을 넘지 않는다

        // ---- 체력 ----
        public const int StartingHealth = 30;    // 히어로 시작 체력

        /// <summary>
        /// 덱에 같은 카드를 최대 몇 장까지 넣을 수 있는지(희귀도별). 덱 빌더 타일의 최대 매수도 여기서 정해진다.
        /// </summary>
        public static int MaxCopies(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Hero:      return 1; // 히어로 1장
                case Rarity.Legendary: return 1; // 전설 1장
                case Rarity.Elite:     return 2; // 엘리트 2장
                default:               return 3; // 일반 3장
            }
        }

        /// <summary>
        /// 턴 번호에 따른 최대 군력. 1턴 = StartingMana이고, 이후 턴마다 1씩 늘어나며 ManaCap을 넘지 않는다.
        /// </summary>
        public static int MaxManaForTurn(int turnNumber)
        {
            int value = StartingMana + (turnNumber - 1); // 턴마다 1씩 증가
            if (value > ManaCap) value = ManaCap;        // 최대치 제한
            if (value < 0) value = 0;                    // 잘못된 턴 번호 방어
            return value;
        }
    }
}
