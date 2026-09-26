namespace CardBattle
{
    /// <summary>덱 빌더 패널의 "저장" 버튼. 누르면 덱을 저장하고(장수가 충분할 때) 패널을 닫는다.</summary>
    public class DeckSaveButton : ClickableButton
    {
        public DeckBuilderUI builder;

        protected override ButtonColors Colors { get { return GamePalette.GreenButton; } }

        protected override void OnClick()
        {
            if (builder != null) builder.SaveDeck();
        }
    }
}
