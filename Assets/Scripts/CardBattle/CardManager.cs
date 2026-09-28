using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 한 판의 진행을 총괄하는 매니저(씬에 하나만 둔다). Play 버튼을 누르면 Start()에서 게임이 시작된다.
    ///
    /// 한 판의 흐름:
    ///   StartBattle(): 새 판 시작(ResetGame) + 덱 섞는 소리·배경음 — 배틀 시작/다시 시작 버튼이 부른다
    ///   ResetGame()  : 손패/필드 정리 → 군력·체력 초기화 → 덱 섞기 → 시작 손패 드로우 → 상대 첫 수
    ///   EndTurn()    : 레인 전투(LaneCombat, 연출은 CombatAnimation) → 승패 확인 → 다음 턴(군력 충전·양쪽 드로우) → 상대 수
    ///   TryPlaceCard : 손패 카드를 필드 슬롯에 내는 유일한 입구(군력 소모 포함)
    ///
    /// 다른 파일로 나눠둔 것:
    ///   규칙 수치 → GameRules.cs / 화면 문구 → GameTexts.cs / 군력 계산 → ManaPool.cs
    ///   상대 판단 → EnemyAI.cs  / 전투 규칙 → LaneCombat.cs
    ///
    /// 플레이어/상대가 똑같이 하는 일은 Side(Player/Enemy)를 받는 함수 하나로 처리한다.
    ///
    /// 게임 방식(mode):
    ///   VsAI      : 청은 AI(EnemyAI)가 턴 시작 때 바로 둔다
    ///   TwoPlayer : 한 컴퓨터에서 두 사람이 번갈아 둔다. 차례가 바뀔 때 가림막(TurnCurtain)이 뜨고,
    ///               차례가 아닌 쪽 손패는 뒷면으로 덮인다. 먼저 두는 편은 턴마다 바뀐다(홀수 턴 조선, 짝수 턴 청)
    ///   Tutorial  : 섞지 않은 튜토리얼 덱 + 청 AI. TutorialGuide가 단계마다 안내하고 시킨 행동만 허락한다
    ///   Online    : 서버(Server/server.js)로 다른 컴퓨터의 사람과 대전. 각자 화면 아래쪽이 나. 차례 규칙은 2인 대전과 같고,
    ///               내 행동은 OnlineMatch가 상대에게 보내고, 상대 행동은 OnlineMatch가 받아서 위쪽 편으로 똑같이 둔다
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        [Header("씬 연결")]
        public GameObject cardViewPrefab; // 카드 한 장을 만들 때 복제하는 프리팹 (Assets/Prefabs/CardView.prefab)
        public HandZone playerHand;       // 플레이어 손패 (화면 아래)
        public HandZone enemyHand;        // 상대 손패 (화면 위)
        public FieldZone playerField;     // 플레이어 필드
        public FieldZone enemyField;      // 상대 필드
        public OnlineMatch online;        // 온라인 대전 연결 (온라인 대전에서만 씀)

        [Header("덱 (DeckData 에셋을 연결)")]
        public DeckData playerDeckData;   // 플레이어가 쓰는 덱 (기본: 조선 시작 덱)
        public DeckData enemyDeckData;    // 상대가 쓰는 덱 (기본: 청 시작 덱)
        public DeckData tutorialPlayerDeck; // 튜토리얼 조선 덱 (섞지 않고 적힌 순서대로 뽑음)
        public DeckData tutorialEnemyDeck;  // 튜토리얼 청 덱 (섞지 않음)
        public int startingHandSize = GameRules.StartingHandSize; // 게임 시작 시 뽑는 장수

        [Header("게임 방식 (처음 화면의 버튼이 정한다)")]
        public GameMode mode = GameMode.VsAI;

        [Header("전투")]
        [Tooltip("끄면 턴 종료 시 레인 전투를 하지 않는다(카드 배치만 테스트할 때 사용)")]
        public bool enableLaneCombat = true; // 레인 전투 사용 여부
        [Tooltip("끄면 전투 연출(돌진·피해 숫자·사라짐) 없이 결과만 바로 반영한다")]
        public bool playCombatAnimation = true; // 전투 연출 사용 여부

        [Header("화면 표시 (연결 안 해도 동작함)")]
        public TextMesh manaText;            // "군력 X / Y (턴 N)"
        public TextMesh enemyManaText;       // "적 군력 X / Y"
        public TextMesh playerDeckCountText; // 플레이어 "덱 N장"
        public TextMesh enemyDeckCountText;  // 상대 "덱 N장"
        public TextMesh playerHealthText;    // "체력 N / 30"
        public TextMesh enemyHealthText;     // "적 체력 N / 30"
        public TextMesh gameOverText;        // 승패가 갈렸을 때만 켜지는 문구

        // ---- 실행 중 자동으로 만드는 화면 표시의 위치/크기 (씬에 이미 있으면 그걸 씀) ----
        public static readonly Vector3 PlayerHealthPos = new Vector3(-7.2f, -3.7f, 0f); // 플레이어 체력 표시 위치
        public static readonly Vector3 EnemyHealthPos = new Vector3(-7.2f, 3.7f, 0f);   // 상대 체력 표시 위치
        static readonly Vector3 ResultBannerPos = new Vector3(0f, 0.3f, 0f);     // 승패 배너 위치(화면 가운데)
        static readonly Vector2 ResultBackdropSize = new Vector2(30f, 3.2f);     // 승패 배너 뒤 어두운 띠 크기
        const float ResultTextHeight = 1.2f;   // "승리!" / "패배..." 글자 높이
        const float ResultHintHeight = 0.32f;  // 아래 안내 문구 글자 높이
        const int ResultSortingOrder = 32700;  // 모든 카드보다 위 (Unity 정렬 순서 최대값 32767 이하)
        static readonly Vector3 InfoPanelPos = new Vector3(8.4f, 0.5f, 0f);   // 오른쪽 안내판 (두 덱 더미 사이)
        static readonly Vector2 InfoPanelSize = new Vector2(5.6f, 3.4f);      // 안내판 배경 크기
        const float InfoTextHeight = 0.24f;    // 안내판 글자 높이
        const int InfoSortingOrder = 32400;    // 카드보다 위, 가림막보다 아래

        [Header("현재 상태 (확인용 — 게임 시작 시 초기화됨)")]
        public int turnNumber = 1;                        // 현재 턴 번호
        public ManaPool playerMana = new ManaPool();      // 플레이어 군력
        public ManaPool enemyMana = new ManaPool();       // 상대 군력
        public int playerHealth = GameRules.StartingHealth; // 플레이어 히어로 체력
        public int enemyHealth = GameRules.StartingHealth;  // 상대 히어로 체력

        // 섞인 드로우 더미(게임 중에만 존재). 덱 설계도(DeckData)와 달리 게임을 진행하며 줄어든다.
        [System.NonSerialized] public List<CardData> playerDrawPile = new List<CardData>();
        [System.NonSerialized] public List<CardData> enemyDrawPile = new List<CardData>();

        bool gameOver; // 승패가 갈렸는지
        bool playerDeckedOut; // 플레이어가 뽑을 카드가 없는데 뽑으려 했는지 (패배 조건)
        bool enemyDeckedOut;  // 상대가 뽑을 카드가 없는데 뽑으려 했는지 (패배 조건)

        // 이번 턴에 각 편이 낸 카드 수 (첫 턴 제한 GameRules.FirstTurnCardLimit 확인용, 턴이 바뀌면 0으로)
        int playerCardsPlayedThisTurn;
        int enemyCardsPlayedThisTurn;

        /// <summary>승패가 갈려서 게임이 끝났는지 (끝나면 턴 종료/카드 내기가 막힌다).</summary>
        public bool IsGameOver { get { return gameOver; } }

        bool busy; // 전투 연출이 진행 중인지 (진행 중엔 턴 종료/카드 내기/드래그를 막는다)

        Side activeSide = Side.Player; // 번갈아 두는 방식(2인·온라인)에서 지금 카드를 내는 편 (다른 방식에서는 항상 Player)
        int localSeat;                 // 내 자리: 0 = 조선(홀수 턴 먼저), 1 = 청. 온라인에서 청으로 매칭되면 1
        TutorialGuide tutorial;        // 튜토리얼 진행 (튜토리얼이 아니면 null)
        TurnCurtain curtain;           // 2인 대전 차례 가림막 (처음 쓸 때 만든다)
        TextMesh infoText;             // 오른쪽 안내판 (튜토리얼·2인 대전에서만, 처음 쓸 때 만든다)

        /// <summary>전투 연출 중이라 조작을 받지 않는 상태인지.</summary>
        public bool IsBusy { get { return busy; } }

        // ================= 게임 흐름 =================

        /// <summary>Unity가 Play 시작 시 자동으로 부른다 → 화면 준비 후 새 판을 시작한다. (게임의 시작점)</summary>
        void Start()
        {
            CardHoverPreview.manager = this;     // 카드 확대 미리보기가 카드 프리팹을 쓸 수 있게
            EnsureRuntimeDisplays();             // 체력 표시·승패 배너가 씬에 없으면 지금 만든다
            SceneBackground.Ensure(Camera.main); // 배경 그림 깔기 (Resources/Backgrounds/BattleBackground)
            ResetGame();
        }

        /// <summary>
        /// 체력 표시와 승패 배너가 씬에 없으면(메뉴 "씬 자동 구성"을 안 눌렀을 때) 실행 중에 만든다.
        /// 군력 표시(manaText)의 글꼴을 복제해서 만들기 때문에 같은 모양으로 보인다.
        /// </summary>
        void EnsureRuntimeDisplays()
        {
            if (playerHealthText == null) playerHealthText = HudFactory.CreateLabel(manaText, "PlayerHealthDisplay", PlayerHealthPos);
            if (enemyHealthText == null) enemyHealthText = HudFactory.CreateLabel(manaText, "EnemyHealthDisplay", EnemyHealthPos);
            if (gameOverText == null) gameOverText = HudFactory.CreateLabel(manaText, "GameOverText", ResultBannerPos);

            // 승패 배너 꾸미기: 큰 글자 + 뒤쪽 어두운 띠 + 아래 안내 문구
            UnityUtil.SetTextHeight(gameOverText, ResultTextHeight, true);
            gameOverText.anchor = TextAnchor.MiddleCenter;
            gameOverText.alignment = TextAlignment.Center;
            var mr = gameOverText.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = ResultSortingOrder;
            HudFactory.EnsureBackdrop(gameOverText.transform, "Backdrop", ResultBackdropSize, GamePalette.ResultBackdrop, ResultSortingOrder - 1);

            if (gameOverText.transform.Find("Hint") == null)
            {
                var hint = HudFactory.CreateLabel(manaText, "Hint", gameOverText.transform.position + new Vector3(0f, -1.0f, 0f));
                hint.transform.SetParent(gameOverText.transform, true);
                UnityUtil.SetTextHeight(hint, ResultHintHeight, false);
                hint.anchor = TextAnchor.MiddleCenter;
                hint.alignment = TextAlignment.Center;
                hint.color = GamePalette.ResultHint;
                hint.text = GameTexts.GameOverHint;
                var hmr = hint.GetComponent<MeshRenderer>();
                if (hmr != null) hmr.sortingOrder = ResultSortingOrder;
            }
            gameOverText.gameObject.SetActive(false);
        }

        /// <summary>
        /// 새 판을 시작한다. 씬이 어떤 상태로 저장돼 있었든 항상 같은 상태로 돌아가므로 몇 번 불러도 안전하다.
        /// </summary>
        public void ResetGame()
        {
            // 1) 이전 판 정리 + 턴/군력/체력/승패 초기화
            ClearBoard();
            ResetState();

            // 2) 드로우 더미를 만들고 시작 손패를 번갈아 뽑는다
            if (mode == GameMode.Online)
            {
                // 온라인: 양쪽이 섞어서 주고받은 더미를 그대로 쓴다 (그래야 두 컴퓨터의 게임이 똑같이 흘러간다)
                localSeat = online.LocalSeat;
                playerDrawPile = new List<CardData>(online.MyPile);
                enemyDrawPile = new List<CardData>(online.OpponentPile);
            }
            else
            {
                bool tutorialMode = mode == GameMode.Tutorial; // 튜토리얼 덱은 섞지 않는다
                playerDrawPile = BuildDeck(tutorialMode ? tutorialPlayerDeck : playerDeckData, !tutorialMode);
                enemyDrawPile = BuildDeck(tutorialMode ? tutorialEnemyDeck : enemyDeckData, !tutorialMode);
            }
            for (int i = 0; i < startingHandSize; i++)
            {
                DrawCard(Side.Player);
                DrawCard(Side.Enemy);
            }
            CheckGameOver(); // 덱이 시작 손패보다 적으면 바로 끝난다

            // 3) 화면 갱신 후 첫 턴 시작
            RefreshAllDisplays();
            if (!gameOver) BeginTurn();
        }

        /// <summary>진행 중인 연출을 멈추고 손패·필드의 카드를 모두 치운다.</summary>
        void ClearBoard()
        {
            StopAllCoroutines();
            CombatAnimation.ClearPopups();
            CardHoverPreview.Hide();
            busy = false;
            if (curtain != null) curtain.Hide();

            ClearHand(playerHand);
            ClearHand(enemyHand);
            ClearField(playerField);
            ClearField(enemyField);
        }

        /// <summary>턴·군력·체력·승패·차례를 새 판 상태로 되돌린다.</summary>
        void ResetState()
        {
            turnNumber = 1;
            playerMana.Reset();
            enemyMana.Reset();
            playerHealth = GameRules.StartingHealth;
            enemyHealth = GameRules.StartingHealth;
            playerDeckedOut = false;
            enemyDeckedOut = false;
            activeSide = Side.Player;
            localSeat = 0;
            tutorial = mode == GameMode.Tutorial ? new TutorialGuide() : null;
            SetGameOver(false, "", GamePalette.ResultDraw, "");
            ResetCardsPlayedThisTurn();
        }

        /// <summary>
        /// 턴의 첫 수. AI 대전·튜토리얼: 청 AI가 바로 카드를 내고 플레이어 차례가 된다.
        /// 2인 대전: 이번 턴에 먼저 둘 사람에게 가림막을 띄운다.
        /// </summary>
        void BeginTurn()
        {
            if (TakesTurns) PassTurnTo(FirstSideOfTurn());
            else PlayEnemyCards();
        }

        /// <summary>한 턴 안에서 두 편이 번갈아 두는 방식인지 (2인 대전, 온라인 대전).</summary>
        bool TakesTurns { get { return mode == GameMode.TwoPlayer || mode == GameMode.Online; } }

        /// <summary>
        /// 배틀 화면에서 새 판을 시작한다(배틀 시작·다시 시작 버튼). ResetGame()에 소리만 더한 것:
        /// 덱 섞는 소리 + 배틀 배경음. (Play 직후 Start()의 ResetGame은 처음 화면 뒤에서 조용히 준비만 한다)
        /// </summary>
        public void StartBattle()
        {
            if (mode == GameMode.Online) { WaitForOpponent(); return; }
            ResetGame();
            GameAudio.Play(GameAudio.Shuffle);
            if (!gameOver) GameAudio.StartAmbience(); // 시작하자마자 끝났으면 배경음은 켜지 않는다
        }

        /// <summary>
        /// 턴을 종료한다("턴 종료" 버튼이 호출). 레인 전투 → 승패 확인 → 다음 턴 시작 순서로 진행된다.
        /// Play 중이고 playCombatAnimation이 켜져 있으면 전투 연출을 보여준 뒤에 결과가 반영된다.
        /// (아직 플레이어 턴/상대 턴이 분리돼 있지 않아서, 매 턴 양쪽이 함께 드로우하고 상대는 곧바로 카드를 낸다)
        /// </summary>
        public void EndTurn()
        {
            if (gameOver || busy || IsCurtainShown) return; // 이미 끝났거나 연출 중이거나 가림막이 떠 있으면 무시

            if (mode == GameMode.Online)
            {
                if (!online.InGame || activeSide != Side.Player) return; // 상대 차례에는 못 누른다
                online.SendEndTurn();
            }

            if (tutorial != null)
            {
                if (!tutorial.AllowsEndTurn) return; // 튜토리얼: 시킨 행동을 먼저 해야 한다
                tutorial.OnTurnEnded();
                RefreshInfoPanel();
            }

            FinishTurn();
        }

        /// <summary>온라인 상대가 턴 종료를 눌렀을 때 (OnlineMatch가 부른다). 상대 차례가 아니면 false.</summary>
        public bool EndOpponentTurn()
        {
            if (gameOver || busy || activeSide != Side.Enemy) return false;
            FinishTurn();
            return true;
        }

        /// <summary>
        /// 지금 차례인 편의 턴을 끝낸다. 번갈아 두는 방식에서 먼저 둔 편이면 상대에게 차례를 넘기고,
        /// 나중에 둔 편이면(또는 AI 대전이면) 레인 전투 → 승패 확인 → 다음 턴.
        /// </summary>
        void FinishTurn()
        {
            if (TakesTurns && activeSide == FirstSideOfTurn())
            {
                PassTurnTo(activeSide.Opponent());
                return;
            }

            if (!enableLaneCombat) { StartNextTurn(); return; }

            var plan = LaneCombat.Plan(playerField, enemyField); // 누가 누구를 때리는지 계산만
            if (playCombatAnimation && Application.isPlaying)
            {
                StartCoroutine(CombatWithAnimation(plan)); // 연출 후 결과 반영
            }
            else
            {
                LaneCombat.ApplyDamage(plan);                // 연출 없이 바로 반영
                LaneCombat.RemoveDead(playerField, enemyField);
                FinishCombat(plan);
            }
        }

        /// <summary>전투 연출(돌진 → 피해 → 사라짐)을 보여준 다음 결과를 반영한다.</summary>
        IEnumerator CombatWithAnimation(LaneCombat.Result plan)
        {
            busy = true;
            RefreshHandFaces(); // 2인 대전: 전투를 보는 동안에는 양쪽 손패를 덮어 둔다
            yield return StartCoroutine(CombatAnimation.PlayAttacks(this, plan, HeroPoint(Side.Player), HeroPoint(Side.Enemy), manaText));
            LaneCombat.ApplyDamage(plan);                              // 이제 실제로 피해 적용 (체력 숫자 갱신)
            yield return StartCoroutine(CombatAnimation.PlayDeaths(WithEquipment(plan.deadUnits))); // 죽은 카드(+ 뒤의 장비)가 사라지는 연출
            LaneCombat.RemoveDead(playerField, enemyField);            // 실제 제거
            busy = false;
            FinishCombat(plan);
        }

        /// <summary>죽은 유닛 목록에 그 유닛 뒤에 붙어 있던 장비를 더한다 (유닛이 쓰러지면 장비도 같이 사라진다).</summary>
        List<CardView> WithEquipment(List<CardView> deadUnits)
        {
            var list = new List<CardView>(deadUnits);
            foreach (var field in new[] { playerField, enemyField })
                foreach (var slot in field.frontRow)
                {
                    if (!deadUnits.Contains(slot.OccupantView)) continue;
                    var equipment = EquipmentBehind(field, slot);
                    if (equipment != null) list.Add(equipment);
                }
            return list;
        }

        /// <summary>히어로 피해를 반영하고 승패를 확인한 뒤, 게임이 안 끝났으면 다음 턴으로 넘어간다.</summary>
        void FinishCombat(LaneCombat.Result plan)
        {
            ChangeHealth(Side.Player, -plan.damageToPlayerHero);
            ChangeHealth(Side.Enemy, -plan.damageToEnemyHero);
            CheckGameOver(); // 양쪽 피해를 모두 반영한 뒤 한 번에 판정 (동시에 0이면 무승부)
            if (!gameOver) StartNextTurn();
        }

        /// <summary>히어로가 맞을 때 피해 숫자를 띄울 위치(체력 표시 근처, 없으면 화면 위/아래).</summary>
        Vector3 HeroPoint(Side side)
        {
            var text = side == Side.Player ? playerHealthText : enemyHealthText;
            if (text != null) return text.transform.position;
            return new Vector3(0f, side == Side.Player ? -4f : 4f, 0f);
        }

        /// <summary>다음 턴 준비: 군력 충전 + 양쪽 1장씩 드로우 + 상대가 카드를 낸다.</summary>
        void StartNextTurn()
        {
            turnNumber++;
            ResetCardsPlayedThisTurn(); // 새 턴이므로 낸 장수 초기화
            playerMana.RefillForTurn(turnNumber);
            enemyMana.RefillForTurn(turnNumber);
            DrawCard(Side.Player);
            DrawCard(Side.Enemy);
            CheckGameOver(); // 양쪽이 다 뽑은 뒤 판정 (둘 다 덱이 바닥났으면 무승부)

            RefreshAllDisplays();
            if (!gameOver) BeginTurn();
        }

        // ================= 2인 대전 (한 컴퓨터에서 번갈아) =================

        /// <summary>
        /// 이번 턴에 먼저 두는 편. 한쪽만 늘 먼저 두면 불공평해서 턴마다 바꾼다 (홀수 턴은 자리 0 = 조선, 짝수 턴은 자리 1 = 청).
        /// 온라인에서 청으로 매칭된 쪽은 내가 자리 1이라 홀수 턴에 상대(Enemy)가 먼저다.
        /// </summary>
        Side FirstSideOfTurn()
        {
            int firstSeat = turnNumber % 2 == 1 ? 0 : 1;
            return firstSeat == localSeat ? Side.Player : Side.Enemy;
        }

        /// <summary>차례를 side에게 넘긴다. 2인 대전: 양쪽 손패를 덮고 가림막을 띄운다(클릭하면 OnCurtainClosed).</summary>
        void PassTurnTo(Side side)
        {
            activeSide = side;
            CardHoverPreview.Hide(); // 앞 사람 카드의 확대 미리보기가 남지 않게
            if (mode == GameMode.TwoPlayer) Curtain.Show(string.Format(GameTexts.CurtainTurn, SideName(side))); // 온라인은 각자 화면이라 가림막 없음
            RefreshHandFaces();
            RefreshInfoPanel();
        }

        /// <summary>가림막이 걷혔을 때(TurnCurtain이 부른다): 차례인 쪽 손패만 앞면으로.</summary>
        public void OnCurtainClosed()
        {
            RefreshHandFaces();
        }

        /// <summary>가림막이 화면을 덮고 있는지.</summary>
        bool IsCurtainShown { get { return curtain != null && curtain.IsShown; } }

        /// <summary>가림막 (처음 쓸 때 배틀 화면 아래에 만든다 → 처음 화면으로 가면 같이 숨는다).</summary>
        TurnCurtain Curtain
        {
            get
            {
                if (curtain == null) curtain = TurnCurtain.Create(this, manaText, BattleRoot());
                return curtain;
            }
        }

        /// <summary>이 편의 손패를 뒷면으로 가려야 하는지. 2인 대전: 가림막이 떠 있거나, 전투 연출 중이거나, 차례가 아닐 때.</summary>
        bool IsHandHidden(Side side)
        {
            if (mode == GameMode.Online) return side == Side.Enemy; // 온라인: 상대 손패는 항상 뒷면
            return mode == GameMode.TwoPlayer && (IsCurtainShown || busy || side != activeSide);
        }

        /// <summary>양쪽 손패 카드를 IsHandHidden에 맞춰 앞면/뒷면으로 바꾼다.</summary>
        void RefreshHandFaces()
        {
            foreach (var side in new[] { Side.Player, Side.Enemy })
                foreach (var card in HandOf(side).cards)
                    card.GetComponent<CardView>().SetFaceDown(IsHandHidden(side));
        }

        /// <summary>편 이름 ("조선" / "청").</summary>
        static string SideName(Side side)
        {
            return side == Side.Player ? GameTexts.JoseonName : GameTexts.QingName;
        }

        // ================= 온라인 대전 =================

        /// <summary>온라인 대전: 보드를 비우고 서버에 접속해 상대를 찾는다. 상대가 정해지면 OnlineMatch가 BeginOnlineGame을 부른다.</summary>
        void WaitForOpponent()
        {
            ClearBoard();
            ResetState();
            playerDrawPile.Clear();
            enemyDrawPile.Clear();
            GameAudio.StopAmbience();
            online.FindMatch();
            RefreshAllDisplays();
        }

        /// <summary>상대와 덱을 주고받았을 때 (OnlineMatch가 부른다): 새 판 시작.</summary>
        public void BeginOnlineGame()
        {
            ResetGame();
            GameAudio.Play(GameAudio.Shuffle);
            if (!gameOver) GameAudio.StartAmbience();
        }

        /// <summary>상대가 나갔거나 연결이 끊겨서 게임을 끝낸다. message는 배너에 뜨는 문구.</summary>
        public void EndByDisconnect(string message)
        {
            if (gameOver) return;
            SetGameOver(true, message, GamePalette.ResultDraw, "");
            GameAudio.StopAmbience();
        }

        /// <summary>온라인 연결을 끊는다 (배틀 화면을 떠날 때 ScreenManager가 부른다).</summary>
        public void LeaveOnline()
        {
            if (online != null) online.Leave();
        }

        // ================= 오른쪽 안내판 (튜토리얼 · 2인 대전 · 온라인) =================

        /// <summary>안내판 문구를 지금 상태에 맞춘다(문구가 없으면 숨긴다). OnlineMatch도 접속 상태가 바뀌면 부른다.</summary>
        public void RefreshInfoPanel()
        {
            string text = InfoMessage();
            if (text == null)
            {
                if (infoText != null) infoText.gameObject.SetActive(false);
                return;
            }
            InfoText.text = text;
            InfoText.gameObject.SetActive(true);
        }

        /// <summary>지금 안내판에 띄울 문구. AI 대전이거나 게임이 끝났으면 null(숨김).</summary>
        string InfoMessage()
        {
            if (mode == GameMode.Online && online.Status != null) return online.Status; // 접속 중·상대 기다리는 중·접속 실패
            if (gameOver) return null;
            if (tutorial != null) return tutorial.Message;
            if (mode == GameMode.TwoPlayer) return string.Format(GameTexts.TurnInfo, SideName(activeSide));
            if (mode == GameMode.Online) return activeSide == Side.Player ? GameTexts.OnlineMyTurn : GameTexts.OnlineTheirTurn;
            return null;
        }

        /// <summary>안내판 글자 (처음 쓸 때 만든다). 군력 표시와 같은 글꼴 + 반투명 배경.</summary>
        TextMesh InfoText
        {
            get
            {
                if (infoText != null) return infoText;
                infoText = HudFactory.CreateLabel(manaText, "InfoPanel", InfoPanelPos);
                infoText.transform.SetParent(BattleRoot(), true);
                UnityUtil.SetTextHeight(infoText, InfoTextHeight, false);
                infoText.anchor = TextAnchor.MiddleCenter;
                infoText.alignment = TextAlignment.Center;
                infoText.color = GamePalette.InfoText;
                infoText.GetComponent<MeshRenderer>().sortingOrder = InfoSortingOrder;
                HudFactory.EnsureBackdrop(infoText.transform, "Backdrop", InfoPanelSize, GamePalette.InfoBackdrop, InfoSortingOrder - 1);
                return infoText;
            }
        }

        /// <summary>배틀 화면 오브젝트 묶음(손패의 부모 = BoardRoot). 실행 중에 만드는 것들을 여기 넣어야 처음 화면에서 같이 숨는다.</summary>
        Transform BattleRoot()
        {
            return playerHand.transform.parent;
        }

        /// <summary>
        /// 손패의 유닛·진 카드를 필드 전열 칸에 내는 유일한 입구. 드래그(CardDragHandler)와 상대 AI가 모두 이 함수를 쓴다.
        /// 실패 조건: 게임 종료 / 카드·슬롯 없음 / 전열 칸이 아님(후열은 장비 칸) / 슬롯이 이미 참 / 군력 부족.
        /// 같은 레인 후열에 장비가 놓여 있으면 새로 들어온 카드가 그 장비 수치만큼 바로 강해진다.
        /// 카드에 spellEffect가 있으면 놓자마자 그 효과가 일어난다(등장 효과 — 전술과 같은 ApplySpell을 쓴다).
        /// </summary>
        public bool TryPlaceCard(CardView view, HandZone fromHand, FieldSlot slot)
        {
            if (!CanUseFromHand(view, fromHand) || !view.data.IsFieldCard) return false; // 장비·전술은 TryEquip / TryCastSpell
            Side side = SideOf(fromHand);
            var field = FieldOf(side);
            if (slot == null || !slot.IsEmpty || !field.IsFrontSlot(slot)) return false; // 유닛·진은 내 전열 빈 칸에만
            if (!ManaOf(side).TrySpend(view.data.cost)) return false;                    // 군력이 부족하면 실패

            int handIndex = TakeFromHand(view, fromHand, side);
            field.PlaceCard(view.transform, slot);
            GameAudio.Play(GameAudio.CardPlace);
            if (IsMyOnlineMove(side)) online.SendPlace(handIndex, LaneOf(field, slot));

            var equipment = EquipmentBehind(field, slot); // 같은 레인 후열 장비가 있으면 강화
            if (equipment != null) view.ApplyBuff(equipment.data.attack, equipment.data.health);

            if (view.data.spellEffect != SpellEffect.None) ApplySpell(side, view.data); // 등장 효과 (전설·히어로 카드)
            return true;
        }

        /// <summary>
        /// 장비 카드를 내 필드의 후열 빈 칸(slot)에 놓는다. 같은 레인 전열 카드가 장비 수치만큼 강해지고,
        /// 앞이 빈 칸에 놓아 두면 나중에 들어오는 카드가 강해진다. 앞 카드가 죽으면 장비도 함께 사라진다(LaneCombat.RemoveDead).
        /// 실패 조건: 게임 종료·연출 중 / 장비가 아님 / 내 후열 빈 칸이 아님 / 군력 부족.
        /// </summary>
        public bool TryEquip(CardView view, HandZone fromHand, FieldSlot slot)
        {
            if (!CanUseFromHand(view, fromHand) || !view.data.IsEquipment) return false;
            Side side = SideOf(fromHand);
            var field = FieldOf(side);
            if (slot == null || !slot.IsEmpty || !field.IsBackSlot(slot)) return false; // 내 후열 빈 칸에만
            if (!ManaOf(side).TrySpend(view.data.cost)) return false;

            int handIndex = TakeFromHand(view, fromHand, side);
            field.PlaceCard(view.transform, slot); // 장비 카드는 후열 칸에 그대로 남는다
            GameAudio.Play(GameAudio.Equip);
            if (IsMyOnlineMove(side)) online.SendEquip(handIndex, LaneOf(field, slot));

            bool front;
            var frontSlot = field.SlotAt(true, field.LaneOf(slot, out front));
            var unit = frontSlot != null ? frontSlot.OccupantView : null;
            if (unit != null) unit.ApplyBuff(view.data.attack, view.data.health); // 앞의 카드를 바로 강화
            return true;
        }

        /// <summary>전열 칸(frontSlot) 바로 뒤 후열에 놓인 장비 카드. 없으면 null.</summary>
        public static CardView EquipmentBehind(FieldZone field, FieldSlot frontSlot)
        {
            if (field == null) return null;
            bool front;
            int lane = field.LaneOf(frontSlot, out front);
            if (lane < 0 || !front) return null;
            var back = field.SlotAt(false, lane);
            var eq = back != null ? back.OccupantView : null;
            return eq != null && eq.data != null && eq.data.IsEquipment ? eq : null;
        }

        /// <summary>
        /// 전술 카드를 쓴다. 효과(spellEffect)가 바로 일어나고 카드는 사라진다.
        /// 실패 조건: 게임 종료·연출 중 / 전술이 아님 / 군력 부족.
        /// </summary>
        public bool TryCastSpell(CardView view, HandZone fromHand)
        {
            if (!CanUseFromHand(view, fromHand) || !view.data.IsSpell) return false;
            Side side = SideOf(fromHand);
            if (!ManaOf(side).TrySpend(view.data.cost)) return false;

            var data = view.data;
            int handIndex = TakeFromHand(view, fromHand, side); // 먼저 손패에서 없애고 (드로우 효과가 손패 자리를 쓸 수 있게)
            UnityUtil.DestroySafe(view.gameObject); // 전술 카드는 쓰면 사라진다
            GameAudio.Play(GameAudio.Spell);
            if (IsMyOnlineMove(side)) online.SendSpell(handIndex);
            ApplySpell(side, data);
            return true;
        }

        /// <summary>
        /// 전술 효과를 실제로 적용한다. side = 전술을 쓴 편.
        /// 새 효과를 추가하려면 SpellEffect에 이름을 넣고 여기에 case를 하나 추가한다.
        /// </summary>
        public void ApplySpell(Side side, CardData data)
        {
            if (data == null) return;
            Side enemy = side.Opponent();
            int v = data.effectValue;
            switch (data.spellEffect)
            {
                case SpellEffect.DamageEnemyHero:
                    DamageHero(enemy, v);
                    break;
                case SpellEffect.DamageAllEnemyUnits:
                    foreach (var unit in FieldUnits(FieldOf(enemy))) unit.ApplyDamage(v);
                    LaneCombat.RemoveDead(playerField, enemyField); // 체력이 0이 된 카드 제거
                    break;
                case SpellEffect.BuffAllAllies:
                    foreach (var unit in FieldUnits(FieldOf(side))) unit.ApplyBuff(v, v);
                    break;
                case SpellEffect.DrawCards:
                    for (int i = 0; i < v; i++) DrawCard(side);
                    CheckGameOver(); // 뽑을 카드가 없었으면 진다
                    break;
                case SpellEffect.HealHero:
                    ChangeHealth(side, Mathf.Min(v, GameRules.StartingHealth - HealthOf(side))); // 시작 체력까지만
                    break;
            }
        }

        /// <summary>손패에서 카드를 쓸 수 있는 기본 조건(게임 진행 중, 연출 중 아님, 첫 턴 장수 제한).</summary>
        bool CanUseFromHand(CardView view, HandZone fromHand)
        {
            if (gameOver || busy) return false;
            if (view == null || view.data == null || fromHand == null) return false;

            Side side = SideOf(fromHand);
            if (TakesTurns && (side != activeSide || IsCurtainShown)) return false; // 차례가 아닌 편
            if (tutorial != null && side == Side.Player && !tutorial.AllowsCard(view.data)) return false; // 튜토리얼: 시킨 카드만
            return CanPlayMoreThisTurn(side);
        }

        /// <summary>
        /// 카드를 낸 뒤 공통 처리: 손패에서 빼고, 드래그 연결을 끊고, 이번 턴 낸 장수를 세고, 군력 표시를 갱신한다.
        /// 돌려주는 값 = 카드가 있던 손패 번호 (온라인 상대에게 "몇 번째 카드를 냈다"고 알릴 때 씀).
        /// </summary>
        int TakeFromHand(CardView view, HandZone fromHand, Side side)
        {
            int handIndex = fromHand.cards.IndexOf(view.transform);
            fromHand.RemoveCard(view.transform);
            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null) drag.ClearHand(); // 이제 손패 카드가 아니므로 드래그 연결을 끊는다

            if (side == Side.Player) playerCardsPlayedThisTurn++;
            else enemyCardsPlayedThisTurn++;
            RefreshManaDisplay();

            if (tutorial != null && side == Side.Player)
            {
                tutorial.OnCardPlayed(view.data); // 시킨 카드를 냈으면 다음 단계로
                RefreshInfoPanel();
            }
            return handIndex;
        }

        /// <summary>온라인 대전에서 내(아래쪽)가 한 행동인지 = 상대에게 보내야 하는지.</summary>
        bool IsMyOnlineMove(Side side)
        {
            return mode == GameMode.Online && side == Side.Player;
        }

        /// <summary>필드 칸의 레인 번호 (왼쪽부터 0).</summary>
        static int LaneOf(FieldZone field, FieldSlot slot)
        {
            bool front;
            return field.LaneOf(slot, out front);
        }

        /// <summary>필드 전열에 놓인 유닛·진 카드들 (후열의 장비는 빼고). 전술 효과·AI가 사용.</summary>
        public static List<CardView> FieldUnits(FieldZone field)
        {
            var list = new List<CardView>();
            foreach (var c in FieldCards(field)) if (c.data != null && c.data.IsFieldCard) list.Add(c);
            return list;
        }

        /// <summary>필드의 모든 슬롯(전열+후열)에 놓인 카드들 (장비 포함).</summary>
        public static List<CardView> FieldCards(FieldZone field)
        {
            var list = new List<CardView>();
            if (field == null) return list;
            foreach (var row in new[] { field.frontRow, field.backRow })
            {
                if (row == null) continue;
                foreach (var slot in row)
                {
                    var unit = slot != null ? slot.OccupantView : null;
                    if (unit != null) list.Add(unit);
                }
            }
            return list;
        }

        /// <summary>
        /// 이번 턴에 이 편이 카드를 더 낼 수 있는지. 첫 턴(1턴)에는 GameRules.FirstTurnCardLimit장까지만 낼 수 있다.
        /// (2턴부터는 군력만 있으면 제한 없음)
        /// </summary>
        public bool CanPlayMoreThisTurn(Side side)
        {
            if (turnNumber != 1 || GameRules.FirstTurnCardLimit <= 0) return true; // 첫 턴이 아니거나 제한이 꺼져 있음
            int played = side == Side.Player ? playerCardsPlayedThisTurn : enemyCardsPlayedThisTurn;
            return played < GameRules.FirstTurnCardLimit;
        }

        /// <summary>양쪽의 "이번 턴에 낸 장수"를 0으로 되돌린다.</summary>
        void ResetCardsPlayedThisTurn()
        {
            playerCardsPlayedThisTurn = 0;
            enemyCardsPlayedThisTurn = 0;
        }

        /// <summary>
        /// 이 손패를 지금 마우스로 집을 수 있는지 (드래그·확대 미리보기가 묻는다).
        /// AI 대전·튜토리얼: 아래쪽 손패만. 2인 대전: 가림막이 걷힌 뒤, 차례인 쪽 손패만. 온라인: 내 차례일 때 내 손패만.
        /// </summary>
        public bool CanControlHand(HandZone hand)
        {
            if (mode == GameMode.TwoPlayer) return !IsCurtainShown && hand == HandOf(activeSide);
            if (mode == GameMode.Online) return hand == playerHand && activeSide == Side.Player;
            return hand == playerHand;
        }

        // ================= 드로우 =================

        /// <summary>
        /// 해당 편의 드로우 더미 맨 위에서 한 장을 뽑아 손패에 넣는다. 손패가 가득 찼으면 뽑은 카드는 버려진다(번).
        /// 더미가 비어 있으면 "덱 소진"으로 표시되고, 다음 CheckGameOver()에서 그 편이 진다.
        /// </summary>
        public void DrawCard(Side side)
        {
            var pile = DrawPileOf(side);
            var hand = HandOf(side);
            if (pile == null || pile.Count == 0)
            {
                if (side == Side.Player) playerDeckedOut = true; // 뽑을 카드가 없다 → 패배 조건
                else enemyDeckedOut = true;
            }
            else if (hand != null)
            {
                var card = pile[pile.Count - 1]; // 리스트 끝 = 더미 맨 위
                pile.RemoveAt(pile.Count - 1);
                if (hand.cards.Count < GameRules.MaxHandSize)
                {
                    SpawnCardToHand(card, hand);
                    GameAudio.Play(GameAudio.CardDraw); // 여러 장을 한꺼번에 뽑아도 소리는 한 번만 난다
                }
                // 손패가 가득 찼으면 카드는 그냥 사라진다(번)
            }
            RefreshDeckCountDisplay();
        }

        /// <summary>카드 프리팹을 만들어 손패에 넣고, 표시 초기화와 드래그 연결까지 한다.</summary>
        public CardView SpawnCardToHand(CardData data, HandZone hand)
        {
            var view = SpawnCardView(data, hand != null ? hand.transform.position : Vector3.zero);
            if (view == null || hand == null) return view;

            hand.AddCard(view.transform);
            view.SetFaceDown(IsHandHidden(SideOf(hand))); // 2인 대전에서 차례가 아닌 쪽이 뽑은 카드는 뒷면
            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null) drag.Init(this, hand); // 이 카드를 드래그해서 낼 수 있게 연결
            return view;
        }

        /// <summary>
        /// 카드 프리팹을 만들어 표시만 해둔다(손패·드래그 연결 없음). 덱 빌더 타일, 팩 개봉 결과처럼
        /// "보여주기만 하는 카드"를 만들 때도 이 함수를 쓴다. 드래그 기능은 연결이 안 되어 자동으로 꺼진 상태다.
        /// </summary>
        public CardView SpawnCardView(CardData data, Vector3 position, Transform parent = null)
        {
            if (cardViewPrefab == null || data == null) return null;
            var go = Object.Instantiate(cardViewPrefab, position, Quaternion.identity, parent);
            go.name = "Card_" + data.cardNameEn;
            var view = go.GetComponent<CardView>();
            if (view != null) view.Setup(data);
            CardHoverPreview.AttachTo(go); // 마우스를 올리면 크게 보이도록
            return view;
        }

        /// <summary>덱 설계도를 카드 낱장 리스트(드로우 더미)로 펼친다. 더미 맨 위 = 리스트 끝.</summary>
        public static List<CardData> BuildDeck(DeckData data, bool shuffle)
        {
            var list = data != null ? data.BuildCardList() : new List<CardData>();
            if (shuffle) Shuffle(list);
            else list.Reverse(); // 섞지 않으면 덱에 적힌 순서대로 뽑히게 뒤집어 둔다
            return list;
        }

        /// <summary>피셔-예이츠 방식으로 리스트를 제자리에서 무작위로 섞는다.</summary>
        static void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1); // 0~i 중 무작위 위치와 교환
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        // ================= 상대 AI 실행 (무엇을 낼지 "판단"은 EnemyAI.cs) =================

        /// <summary>상대가 낼 수 있는 카드가 없을 때까지 한 장씩 필드에 낸다.</summary>
        void PlayEnemyCards()
        {
            if (gameOver || enemyHand == null || enemyField == null) return;

            // 한 번 돌 때마다 카드를 한 장 내거나 멈추므로 무한 반복이 될 수 없다.
            while (true)
            {
                // 지금 손패를 AI가 판단할 수 있는 목록으로 만든다
                var handCards = new List<CardData>();
                var handViews = new List<CardView>();
                foreach (var tf in enemyHand.cards)
                {
                    var view = tf != null ? tf.GetComponent<CardView>() : null;
                    handViews.Add(view);
                    handCards.Add(view != null ? view.data : null);
                }

                bool hasEmptyFront = enemyField.GetFirstEmpty(true) != null;       // 유닛·진을 놓을 칸
                bool hasEquipSlot = EnemyAI.ChooseEquipSlot(enemyField) != null;   // 장비를 놓을 칸 (앞에 유닛이 있는 후열)
                int pick = EnemyAI.ChooseCardToPlay(handCards, enemyMana.current, hasEmptyFront, hasEquipSlot); // 낼 카드 고르기
                if (pick < 0) break; // 낼 카드 없음

                var card = handCards[pick];
                bool played;
                if (card.IsSpell) played = TryCastSpell(handViews[pick], enemyHand);                                     // 전술
                else if (card.IsEquipment) played = TryEquip(handViews[pick], enemyHand, EnemyAI.ChooseEquipSlot(enemyField)); // 장비
                else played = TryPlaceCard(handViews[pick], enemyHand, EnemyAI.ChooseSlot(enemyField));             // 유닛·진
                if (!played) break; // 안전장치 (못 냈으면 멈춘다)
            }
        }

        // ================= 전투 / 체력 / 승패 =================

        /// <summary>히어로에게 피해를 준다(카드 효과 등에서 사용). 체력이 0이 되면 게임이 끝난다.</summary>
        public void DamageHero(Side side, int amount)
        {
            if (gameOver || amount <= 0) return;
            ChangeHealth(side, -amount);
            CheckGameOver();
        }

        /// <summary>해당 편 히어로의 현재 체력.</summary>
        public int HealthOf(Side side)
        {
            return side == Side.Player ? playerHealth : enemyHealth;
        }

        /// <summary>체력을 delta만큼 바꾼다(음수면 피해). 0 아래로는 내려가지 않는다.</summary>
        void ChangeHealth(Side side, int delta)
        {
            if (delta == 0) return;
            int value = Mathf.Max(0, HealthOf(side) + delta);
            if (side == Side.Player) playerHealth = value;
            else enemyHealth = value;
            RefreshHealthDisplay();
        }

        /// <summary>
        /// 패배 조건을 확인해서 승리/패배/무승부를 정한다.
        /// 패배 조건: 체력이 0이 됨 / 뽑을 카드가 없는데 뽑아야 함(덱 소진). 양쪽이 동시에 걸리면 무승부.
        /// </summary>
        void CheckGameOver()
        {
            if (gameOver) return;
            bool playerLost = playerHealth <= 0 || playerDeckedOut;
            bool enemyLost = enemyHealth <= 0 || enemyDeckedOut;
            if (!playerLost && !enemyLost) return; // 아직 둘 다 버티는 중

            GameAudio.StopAmbience(); // 배경음을 끄고 결과 소리를 들려준다
            if (playerLost && enemyLost)
            {
                SetGameOver(true, GameTexts.Draw, GamePalette.ResultDraw, GameTexts.ReasonBoth);
                GameAudio.Play(GameAudio.Draw);
            }
            else if (mode == GameMode.TwoPlayer)
            {
                // 2인 대전: "조선 승리!" / "청 승리!" — 둘 중 누군가는 이겼으니 승리 소리
                Side loser = playerLost ? Side.Player : Side.Enemy;
                string reason = HealthOf(loser) <= 0 ? GameTexts.ReasonSideHealth : GameTexts.ReasonSideDeck;
                SetGameOver(true, string.Format(GameTexts.SideWins, SideName(loser.Opponent())), GamePalette.ResultVictory,
                            string.Format(reason, SideName(loser)));
                GameAudio.Play(GameAudio.Victory);
            }
            else if (playerLost)
            {
                SetGameOver(true, GameTexts.Defeat, GamePalette.ResultDefeat, playerHealth <= 0 ? GameTexts.ReasonMyHealth : GameTexts.ReasonMyDeck);
                GameAudio.Play(GameAudio.Defeat);
            }
            else
            {
                SetGameOver(true, GameTexts.Victory, GamePalette.ResultVictory, enemyHealth <= 0 ? GameTexts.ReasonEnemyHealth : GameTexts.ReasonEnemyDeck);
                GameAudio.Play(GameAudio.Victory);
            }
        }

        /// <summary>게임 종료 상태를 바꾸고, 승패 배너(문구+색+이유)를 보이거나 숨긴다.</summary>
        void SetGameOver(bool over, string message, Color color, string reason)
        {
            gameOver = over;
            RefreshInfoPanel(); // 게임이 끝나면 안내판은 숨긴다
            if (gameOverText == null) return;
            gameOverText.text = message;
            gameOverText.color = color;
            var hint = gameOverText.transform.Find("Hint");
            var hintText = hint != null ? hint.GetComponent<TextMesh>() : null;
            if (hintText != null)
                hintText.text = string.IsNullOrEmpty(reason) ? GameTexts.GameOverHint : reason + "\n" + GameTexts.GameOverHint;
            gameOverText.gameObject.SetActive(over);
        }

        // ================= 화면 표시 =================

        /// <summary>군력/덱 장수/체력 표시를 전부 갱신한다.</summary>
        void RefreshAllDisplays()
        {
            RefreshManaDisplay();
            RefreshDeckCountDisplay();
            RefreshHealthDisplay();
        }

        /// <summary>양쪽 군력 표시를 갱신한다.</summary>
        void RefreshManaDisplay()
        {
            bool twoPlayer = mode == GameMode.TwoPlayer; // 2인 대전은 "적" 대신 편 이름으로
            UnityUtil.SetText(manaText, string.Format(twoPlayer ? GameTexts.PlayerManaTwoPlayer : GameTexts.PlayerMana, playerMana.current, playerMana.max, turnNumber));
            UnityUtil.SetText(enemyManaText, string.Format(twoPlayer ? GameTexts.EnemyManaTwoPlayer : GameTexts.EnemyMana, enemyMana.current, enemyMana.max));
        }

        /// <summary>양쪽 남은 덱 장수 표시를 갱신한다.</summary>
        void RefreshDeckCountDisplay()
        {
            UnityUtil.SetText(playerDeckCountText, string.Format(GameTexts.DeckCount, playerDrawPile.Count));
            UnityUtil.SetText(enemyDeckCountText, string.Format(GameTexts.DeckCount, enemyDrawPile.Count));
        }

        /// <summary>양쪽 히어로 체력 표시를 갱신한다.</summary>
        void RefreshHealthDisplay()
        {
            bool twoPlayer = mode == GameMode.TwoPlayer;
            UnityUtil.SetText(playerHealthText, string.Format(twoPlayer ? GameTexts.PlayerHealthTwoPlayer : GameTexts.PlayerHealth, playerHealth, GameRules.StartingHealth));
            UnityUtil.SetText(enemyHealthText, string.Format(twoPlayer ? GameTexts.EnemyHealthTwoPlayer : GameTexts.EnemyHealth, enemyHealth, GameRules.StartingHealth));
        }

        // ================= 편(Side)별 참조 도우미 — 플레이어/상대 분기는 이 아래에만 모아둔다 =================

        /// <summary>이 손패가 어느 편 것인지.</summary>
        public Side SideOf(HandZone hand) { return hand == playerHand ? Side.Player : Side.Enemy; }
        /// <summary>해당 편의 손패.</summary>
        public HandZone HandOf(Side side) { return side == Side.Player ? playerHand : enemyHand; }
        /// <summary>해당 편의 필드.</summary>
        public FieldZone FieldOf(Side side) { return side == Side.Player ? playerField : enemyField; }
        /// <summary>해당 편의 군력.</summary>
        public ManaPool ManaOf(Side side) { return side == Side.Player ? playerMana : enemyMana; }
        /// <summary>해당 편의 드로우 더미.</summary>
        public List<CardData> DrawPileOf(Side side) { return side == Side.Player ? playerDrawPile : enemyDrawPile; }

        // ================= 정리 =================

        /// <summary>손패를 비운다. 리스트가 아니라 실제 자식 오브젝트 기준으로 지워서 "고아 카드"가 남지 않게 한다.</summary>
        static void ClearHand(HandZone hand)
        {
            if (hand == null) return;
            UnityUtil.DestroyChildren(hand.transform);
            hand.cards.Clear();
        }

        /// <summary>필드의 카드를 치운다. FieldZone의 자식에는 슬롯 오브젝트도 있으므로 슬롯은 남기고 카드만 지운다.</summary>
        static void ClearField(FieldZone field)
        {
            if (field == null) return;

            // 남겨야 할 슬롯 목록을 모으면서 슬롯을 비운다
            var slotTransforms = new HashSet<Transform>();
            foreach (var row in new[] { field.frontRow, field.backRow })
            {
                if (row == null) continue;
                foreach (var slot in row)
                {
                    if (slot == null) continue;
                    slotTransforms.Add(slot.transform);
                    slot.occupant = null;
                }
            }
            UnityUtil.DestroyChildren(field.transform, child => slotTransforms.Contains(child)); // 슬롯이 아닌 자식만 파괴
        }
    }
}
