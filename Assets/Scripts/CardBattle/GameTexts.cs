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

        // ---- 2인 대전 (한 컴퓨터에서 번갈아) ----
        public const string JoseonName = "조선";   // 아래쪽 편 이름
        public const string QingName = "청";       // 위쪽 편 이름
        public const string PlayerManaTwoPlayer = "조선 군력 {0} / {1}   (턴 {2})"; // 2인 대전에서 아래쪽 군력
        public const string EnemyManaTwoPlayer = "청 군력 {0} / {1}";               // 2인 대전에서 위쪽 군력
        public const string PlayerHealthTwoPlayer = "조선 체력 {0} / {1}";          // 2인 대전에서 아래쪽 체력
        public const string EnemyHealthTwoPlayer = "청 체력 {0} / {1}";             // 2인 대전에서 위쪽 체력
        public const string CurtainTurn = "{0} 차례입니다\n\n상대는 화면을 보지 않게 자리를 넘겨 주세요\n(화면을 클릭하면 시작)"; // {0}=편 이름
        public const string TurnInfo = "지금: {0} 차례\n\n카드를 다 냈으면\n[턴 종료]를 누르세요"; // 오른쪽 안내판
        public const string SideWins = "{0} 승리!";                       // {0}=이긴 편
        public const string ReasonSideHealth = "{0}의 체력이 모두 닳았습니다";   // {0}=진 편
        public const string ReasonSideDeck = "{0}은 더 이상 뽑을 카드가 없습니다"; // {0}=진 편

        // ---- 온라인 대전 (오른쪽 안내판, 승패 배너) ----
        public const string OnlineConnecting = "서버에 연결하는 중…\n\n(서버가 잠들어 있으면\n깨어나는 데 1분쯤 걸려요)";
        public const string OnlineWaiting = "상대를 기다리는 중…\n\n다른 사람이 [온라인 대전]을\n누르면 바로 시작합니다";
        public const string OnlineFailed = "서버에 연결하지 못했습니다\n\n[다시 시작]을 누르면\n다시 시도합니다";
        public const string OnlineMyTurn = "내 차례\n\n카드를 다 냈으면\n[턴 종료]를 누르세요";
        public const string OnlineTheirTurn = "상대 차례\n\n상대가 카드를 내는 중…";
        public const string OnlineOpponentLeft = "상대가 나갔습니다";   // 승패 배너
        public const string OnlineConnectionLost = "연결이 끊겼습니다"; // 승패 배너
        public const string ChooseFaction = "진영을 고르세요";         // 배틀 시작 때 장수 고르기 화면 제목

        /// <summary>장수 판의 이름 ("조선 · 인조").</summary>
        public static string HeroTitle(Faction faction, CardData hero)
        {
            string side = faction == Faction.Qing ? QingName : JoseonName;
            return hero != null ? side + " · " + hero.cardNameKo : side;
        }

        /// <summary>장수 판 아래 영웅 능력 설명 (이름 (비용) + 효과).</summary>
        public static string HeroPower(Faction faction)
        {
            return faction == Faction.Qing ? "Charge Order (2)\n가장 강한 아군 바로 공격" : "Fortify (1)\n가장 약한 아군 체력 +3";
        }

        public const string OnlineDesync = "두 화면이 어긋났습니다";    // 승패 배너 (상대 행동을 적용할 수 없을 때)

        // ---- 튜토리얼 안내판 (오른쪽) ----
        public const string TutorialPlaceUnit = "튜토리얼 1/4 · 유닛 내기\n\n손패의 [의병]을 끌어서\n앞줄(전열) 빈 칸에 놓으세요.\n\n카드 왼쪽 위 숫자가\n필요한 군력입니다.";
        public const string TutorialEndFirstTurn = "좋아요!\n\n이제 [턴 종료]를 누르세요.\n같은 줄 카드끼리 싸우고,\n앞이 비어 있으면\n적 장수를 칩니다.";
        public const string TutorialEquip = "튜토리얼 2/4 · 장비\n\n[편전]을 의병 바로 뒤\n뒷줄(후열) 칸에 놓으면\n앞 카드 공격력이 오릅니다.";
        public const string TutorialEndSecondTurn = "앞 카드가 쓰러지면\n뒤의 장비도 함께\n사라집니다.\n\n[턴 종료]를 누르세요.";
        public const string TutorialSpell = "튜토리얼 3/4 · 전술\n\n[봉화]를 손패에서 위쪽\n(전장 쪽)으로 끌어 놓으면\n바로 효과가 납니다.\n(카드 2장 뽑기)";
        public const string TutorialFreePlay = "튜토리얼 4/4 · 실전\n\n이제 자유롭게 싸워서\n청군의 체력을 0으로\n만드세요!";

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
