namespace CardBattle
{
    /// <summary>누르면 지정한 화면으로 이동하는 버튼 (예: 처음 화면의 "배틀 시작").</summary>
    public class ScreenButton : ClickableButton
    {
        public ScreenManager screens;                    // 화면 전환 담당 (인스펙터에서 연결)
        public GameScreen target = GameScreen.Battle;    // 이동할 화면

        /// <summary>눈에 잘 띄는 금색 버튼.</summary>
        protected override ButtonColors Colors { get { return GamePalette.GoldButton; } }

        /// <summary>클릭 시: 지정한 화면으로 이동.</summary>
        protected override void OnClick()
        {
            if (screens != null) screens.Show(target);
        }
    }
}
