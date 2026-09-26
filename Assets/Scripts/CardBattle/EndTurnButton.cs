namespace CardBattle
{
    /// <summary>
    /// "턴 종료" 버튼. 누르면 레인 전투 후 다음 턴으로 넘어간다.
    /// 승패가 이미 갈린 뒤에 누르면 처음 화면으로 돌아간다.
    /// </summary>
    public class EndTurnButton : ClickableButton
    {
        public CardManager manager;   // 턴을 넘길 게임 매니저 (인스펙터에서 연결)
        public ScreenManager screens; // 게임이 끝났을 때 돌아갈 화면 전환 담당

        /// <summary>눈에 잘 띄는 금색 버튼.</summary>
        protected override ButtonColors Colors { get { return GamePalette.GoldButton; } }

        /// <summary>클릭 시: 게임이 끝났으면 처음 화면으로, 아니면 턴 종료.</summary>
        protected override void OnClick()
        {
            if (manager == null) return;
            if (!manager.IsGameOver) manager.EndTurn();
            else if (screens != null) screens.ShowLobby();
        }
    }
}
