namespace CardBattle
{
    /// <summary>
    /// "다시 시작" 버튼(배틀 화면). 누르면 지금 판을 버리고 덱을 새로 섞어 처음부터 다시 시작한다.
    /// 전투 연출 중이거나 게임이 끝난 뒤에도 누를 수 있다.
    /// </summary>
    public class RestartButton : ClickableButton
    {
        public CardManager manager; // 새 판을 시작할 게임 매니저 (인스펙터에서 연결)

        /// <summary>눈에 덜 띄는 회청색 버튼 (턴 종료 버튼과 헷갈리지 않게).</summary>
        protected override ButtonColors Colors { get { return GamePalette.NeutralButton; } }

        /// <summary>클릭 시: 새 판 시작 (배경음도 처음부터).</summary>
        protected override void OnClick()
        {
            if (manager != null) manager.StartBattle();
        }
    }
}
