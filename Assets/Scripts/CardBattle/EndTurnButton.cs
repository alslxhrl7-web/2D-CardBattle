namespace CardBattle
{
    /// <summary>"턴 종료" 버튼. 누르면 레인 전투 후 다음 턴으로 넘어간다.</summary>
    public class EndTurnButton : ClickableButton
    {
        public CardManager manager;

        protected override ButtonColors Colors { get { return GamePalette.GoldButton; } }

        protected override void OnClick()
        {
            if (manager != null) manager.EndTurn();
        }
    }
}
