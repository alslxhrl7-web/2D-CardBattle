namespace CardBattle
{
    /// <summary>팩 결과 패널의 "닫기" 버튼.</summary>
    public class PackCloseButton : ClickableButton
    {
        public PackOpenerUI ui;

        protected override void OnClick()
        {
            if (ui != null) ui.ClosePanel();
        }
    }
}
