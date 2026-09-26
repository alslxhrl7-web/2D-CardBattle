using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// "게임 종료" 버튼(배틀 화면).
    ///   PC 실행 파일: 게임 창을 닫는다.
    ///   웹(itch.io): 브라우저 게임은 스스로 닫을 수 없으므로 처음 화면으로 돌아간다.
    ///   Unity 에디터: Play를 멈춘다.
    /// </summary>
    public class QuitGameButton : ClickableButton
    {
        public ScreenManager screens; // 웹에서 돌아갈 처음 화면 (인스펙터에서 연결)

        /// <summary>회청색 버튼.</summary>
        protected override ButtonColors Colors { get { return GamePalette.NeutralButton; } }

        /// <summary>클릭 시: 실행 환경에 맞게 게임을 끝낸다.</summary>
        protected override void OnClick()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터에서는 Play 정지
#else
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (screens != null) screens.ShowLobby(); // 웹은 창을 못 닫으니 처음 화면으로
            }
            else
            {
                Application.Quit(); // PC 등: 게임 종료
            }
#endif
        }
    }
}
