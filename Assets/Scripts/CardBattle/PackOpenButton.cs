namespace CardBattle
{
    /// <summary>"팩 열기" 버튼. 누르면 연결된 PackOpenerUI가 팩을 연다.</summary>
    public class PackOpenButton : ClickableButton
    {
        public PackOpenerUI ui;

        protected override ButtonColors Colors { get { return GamePalette.GoldButton; } }

        protected override void OnClick()
        {
            if (ui != null) ui.OpenPack();
        }
    }
}
