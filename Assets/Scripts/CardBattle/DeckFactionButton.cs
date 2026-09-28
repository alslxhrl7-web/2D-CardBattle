namespace CardBattle
{
    /// <summary>덱 편집 화면의 "청 덱 편집" / "조선 덱 편집" 버튼. 누르면 조선 덱 ↔ 청 덱 편집을 바꾼다.</summary>
    public class DeckFactionButton : ClickableButton
    {
        public DeckBuilderUI builder; // 덱 빌더 (자동 적용이 연결)

        protected override void OnClick()
        {
            if (builder != null) builder.SwitchFaction();
        }
    }
}
