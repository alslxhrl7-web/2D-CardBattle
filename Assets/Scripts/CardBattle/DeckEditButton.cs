namespace CardBattle
{
    /// <summary>"덱 편집" 버튼. 누르면 덱 빌더 패널을 열고 닫는다.</summary>
    public class DeckEditButton : ClickableButton
    {
        public DeckBuilderUI builder;

        protected override void OnClick()
        {
            if (builder != null) builder.TogglePanel();
        }
    }
}
