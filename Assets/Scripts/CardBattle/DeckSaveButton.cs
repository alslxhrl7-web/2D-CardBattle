namespace CardBattle
{
    /// <summary>덱 빌더 패널의 "저장" 버튼. 누르면 덱을 저장하고(장수가 충분할 때) 패널을 닫는다.</summary>
    public class DeckSaveButton : ClickableButton
    {
        public DeckBuilderUI builder; // 저장할 덱 빌더 (인스펙터에서 연결)

        /// <summary>"확정" 의미의 초록 버튼.</summary>
        protected override ButtonColors Colors { get { return GamePalette.GreenButton; } }

        /// <summary>클릭 시: 덱 저장.</summary>
        protected override void OnClick()
        {
            if (builder != null) builder.SaveDeck();
        }
    }
}
