namespace CardBattle
{
    /// <summary>
    /// "덱 편집" 버튼. screens가 연결돼 있으면 덱 편집 화면으로 이동하고,
    /// 연결이 없으면(예전 방식) 덱 빌더 패널을 그 자리에서 열고 닫는다.
    /// </summary>
    public class DeckEditButton : ClickableButton
    {
        public DeckBuilderUI builder;  // 열고 닫을 덱 빌더 (인스펙터에서 연결)
        public ScreenManager screens;  // 화면 전환 담당 (연결돼 있으면 덱 편집 "화면"으로 이동)

        /// <summary>클릭 시: 덱 편집 화면으로 이동 (또는 패널 열기/닫기).</summary>
        protected override void OnClick()
        {
            if (screens != null) screens.ShowDeckEdit();
            else if (builder != null) builder.TogglePanel();
        }
    }
}
