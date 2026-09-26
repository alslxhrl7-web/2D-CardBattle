namespace CardBattle
{
    /// <summary>
    /// 화면에 표시되는 문구를 한 곳에 모아둔 파일. 문구를 바꾸거나 나중에 다국어를 붙일 때 이 파일만 보면 된다.
    /// {0}, {1} 자리에는 숫자 등이 순서대로 채워진다(string.Format 규칙). 자리 표시를 지우면 오류가 나니 주의.
    /// </summary>
    public static class GameTexts
    {
        // ---- 보드 상태 표시 ----
        public const string PlayerMana = "군력 {0} / {1}   (턴 {2})";   // {0}=현재 군력, {1}=최대 군력, {2}=턴 번호
        public const string EnemyMana = "적 군력 {0} / {1}";             // {0}=현재 군력, {1}=최대 군력
        public const string DeckCount = "덱 {0}장";                     // {0}=남은 덱 장수
        public const string PlayerHealth = "체력 {0} / {1}";             // {0}=현재 체력, {1}=최대 체력
        public const string EnemyHealth = "적 체력 {0} / {1}";           // {0}=현재 체력, {1}=최대 체력

        // ---- 승패 ----
        public const string Victory = "승리!";   // 상대 체력이 0이 됐을 때
        public const string Defeat = "패배...";  // 내 체력이 0이 됐을 때
        public const string Draw = "무승부";     // 양쪽이 동시에 0이 됐을 때
        public const string ReasonMyHealth = "내 체력이 모두 닳았습니다";         // 패배 이유: 체력 0
        public const string ReasonMyDeck = "더 이상 뽑을 카드가 없습니다";        // 패배 이유: 덱 소진
        public const string ReasonEnemyHealth = "상대 체력이 모두 닳았습니다";    // 승리 이유
        public const string ReasonEnemyDeck = "상대가 더 이상 뽑을 카드가 없습니다"; // 승리 이유
        public const string ReasonBoth = "양쪽이 동시에 패배 조건에 걸렸습니다";   // 무승부 이유
        public const string GameOverHint = "턴 종료 버튼을 누르면 계속"; // 승패 배너 아래 안내 (처음 화면이 있으면 처음 화면으로, 없으면 새 판)

        // ---- 덱 빌더 ----
        public const string DeckBuilderReady = "덱 카드 수: {0}장 (저장 가능)";            // {0}=총 장수
        public const string DeckBuilderTooSmall = "덱 카드 수: {0}장 (최소 {1}장 필요)";   // {0}=총 장수, {1}=최소 장수
        public const string TileCountBadge = "x{0}";                                        // {0}=이 카드를 넣은 매수

        // ---- 카드 종류 (카드 위쪽 가운데에 표시, 유닛은 표시 안 함) ----
        public const string KindWeapon = "장비";    // 유닛에게 붙이는 카드
        public const string KindSpell = "전술";     // 즉시 효과 카드
        public const string KindFormation = "진";   // 필드에 세우는 구조물

        /// <summary>카드 종류에 맞는 표시 문구. 유닛은 빈 문자열(표시 안 함).</summary>
        public static string KindLabel(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Weapon:    return KindWeapon;
                case CardKind.Spell:     return KindSpell;
                case CardKind.Formation: return KindFormation;
                default:                 return "";
            }
        }

        // ---- 덱 더미 ----
        public const string PileBattle = "덱 {0}장";           // 배틀 화면의 드로우 더미 아래 ({0}=남은 장수)
        public const string PileLobby = "덱 편집\n({0}장)";    // 처음 화면의 덱 더미 아래 ({0}=덱 총 장수)

        // ---- 카드팩 ----
        public const string PackOpened = "{0} 개봉! ({1}장)";   // {0}=팩 이름, {1}=나온 장수
    }
}
