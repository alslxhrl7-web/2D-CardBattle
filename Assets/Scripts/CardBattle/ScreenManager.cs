using UnityEngine;

namespace CardBattle
{
    /// <summary>게임의 화면 종류.</summary>
    public enum GameScreen
    {
        Lobby,    // 처음 화면: 배틀 시작 / 덱 편집 / 팩 열기 버튼
        Battle,   // 배틀 화면: 카드와 턴 종료 버튼
        DeckEdit  // 덱 편집 화면: 덱 빌더 패널
    }

    /// <summary>
    /// 화면 전환 담당. 화면마다 보여줄 오브젝트 묶음을 켜고 끄기만 하는 단순한 방식이다.
    /// 어떤 오브젝트를 어느 화면에서 보여줄지는 인스펙터의 lobbyObjects / battleObjects 목록에서
    /// 오브젝트를 넣고 빼는 것으로 바꿀 수 있다(코드 수정 불필요).
    /// 이 목록은 메뉴 "CardBattle → 씬 자동 구성"이 처음 한 번 채워준다.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        [Header("시작 화면")]
        public GameScreen startScreen = GameScreen.Lobby; // Play를 눌렀을 때 처음 보이는 화면

        [Header("화면별로 켜고 끌 오브젝트")]
        public GameObject[] lobbyObjects;  // 처음 화면에서만 보이는 것 (제목, 버튼들)
        public GameObject[] battleObjects; // 배틀 화면에서만 보이는 것 (손패, 필드, 턴 종료 버튼, 군력/체력 표시 등)

        [Header("연결")]
        public CardManager manager;       // 배틀을 시작할 때 새 판을 준비시킬 매니저
        public DeckBuilderUI deckBuilder; // 덱 편집 화면 = 덱 빌더 패널
        public PackOpenerUI packOpener;   // 팩 결과 패널 (화면이 바뀌면 닫는다)

        /// <summary>지금 보이는 화면.</summary>
        public GameScreen Current { get; private set; }

        /// <summary>Play 시작 시 시작 화면을 보여준다.</summary>
        void Start()
        {
            Show(startScreen);
        }

        /// <summary>처음 화면으로 (버튼/다른 스크립트에서 호출).</summary>
        public void ShowLobby() { Show(GameScreen.Lobby); }
        /// <summary>배틀 화면으로 — 매번 새 판이 시작된다.</summary>
        public void ShowBattle() { Show(GameScreen.Battle); }
        /// <summary>덱 편집 화면으로.</summary>
        public void ShowDeckEdit() { Show(GameScreen.DeckEdit); }

        /// <summary>지정한 화면만 보이게 하고 나머지는 숨긴다.</summary>
        public void Show(GameScreen screen)
        {
            SetActive(lobbyObjects, screen == GameScreen.Lobby);
            SetActive(battleObjects, screen == GameScreen.Battle);

            if (packOpener != null) packOpener.ClosePanel(); // 팩 결과는 화면이 바뀌면 닫는다

            if (deckBuilder != null)
            {
                if (screen == GameScreen.DeckEdit) deckBuilder.OpenPanel();
                else deckBuilder.ClosePanel();
            }

            // 배틀 화면에 들어갈 때마다 새 판 시작 (오브젝트를 먼저 켠 다음에 해야 카드가 제자리로 간다)
            if (screen == GameScreen.Battle && manager != null) manager.ResetGame();

            Current = screen;
        }

        /// <summary>목록의 오브젝트를 전부 켜거나 끈다(빈 칸은 무시).</summary>
        static void SetActive(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            foreach (var go in objects)
                if (go != null) go.SetActive(active);
        }
    }
}
