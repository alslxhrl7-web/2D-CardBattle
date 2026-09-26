namespace CardBattle
{
    /// <summary>"팩 열기" 버튼. 누르면 연결된 PackOpenerUI가 팩을 연다.</summary>
    public class PackOpenButton : ClickableButton
    {
        public PackOpenerUI ui; // 팩을 열 결과 패널 (인스펙터에서 연결)

        /// <summary>눈에 잘 띄는 금색 버튼.</summary>
        protected override ButtonColors Colors { get { return GamePalette.GoldButton; } }

        /// <summary>클릭 시: 팩 개봉.</summary>
        protected override void OnClick()
        {
            if (ui != null) ui.OpenPack();
        }
    }
}
