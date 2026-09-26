namespace CardBattle
{
    /// <summary>
    /// 화면에 표시되는 문구를 한 곳에 모아둔 파일. 문구를 바꾸거나 나중에 다국어를 붙일 때 이 파일만 보면 된다.
    /// {0}, {1} 자리에는 숫자 등이 순서대로 채워진다(string.Format 규칙).
    /// </summary>
    public static class GameTexts
    {
        // ---- 보드 상태 표시 ----
        public const string PlayerMana = "군력 {0} / {1}   (턴 {2})";   // 현재, 최대, 턴
        public const string EnemyMana = "적 군력 {0} / {1}";             // 현재, 최대
        public const string DeckCount = "덱 {0}장";                     // 남은 장수
        public const string PlayerHealth = "체력 {0} / {1}";             // 현재, 최대
        public const string EnemyHealth = "적 체력 {0} / {1}";           // 현재, 최대

        // ---- 승패 ----
        public const string Victory = "승리!";
        public const string Defeat = "패배...";
        public const string Draw = "무승부";

        // ---- 덱 빌더 ----
        public const string DeckBuilderReady = "덱 카드 수: {0}장 (저장 가능)";            // 총 장수
        public const string DeckBuilderTooSmall = "덱 카드 수: {0}장 (최소 {1}장 필요)";   // 총 장수, 최소 장수
        public const string TileCountBadge = "x{0}";                                        // 매수

        // ---- 카드팩 ----
        public const string PackOpened = "{0} 개봉! ({1}장)";   // 팩 이름, 장수
    }
}
