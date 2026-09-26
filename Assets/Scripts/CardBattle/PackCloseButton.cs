namespace CardBattle
{
    /// <summary>팩 결과 패널의 "닫기" 버튼.</summary>
    public class PackCloseButton : ClickableButton
    {
        public PackOpenerUI ui; // 닫을 결과 패널 (인스펙터에서 연결)

        /// <summary>클릭 시: 결과 패널 닫기.</summary>
        protected override void OnClick()
        {
            if (ui != null) ui.ClosePanel();
        }
    }
}
