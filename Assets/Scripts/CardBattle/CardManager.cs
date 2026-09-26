using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 한 판의 진행을 총괄하는 매니저(씬에 하나만 둔다). Play 버튼을 누르면 Start()에서 게임이 시작된다.
    ///
    /// 한 판의 흐름:
    ///   ResetGame()  : 손패/필드 정리 → 군력·체력 초기화 → 덱 섞기 → 시작 손패 드로우 → 상대 첫 수
    ///   EndTurn()    : 레인 전투(LaneCombat, 연출은 CombatAnimation) → 승패 확인 → 다음 턴(군력 충전·양쪽 드로우) → 상대 수
    ///   TryPlaceCard : 손패 카드를 필드 슬롯에 내는 유일한 입구(군력 소모 포함)
    ///
    /// 다른 파일로 나눠둔 것:
    ///   규칙 수치 → GameRules.cs / 화면 문구 → GameTexts.cs / 군력 계산 → ManaPool.cs
    ///   상대 판단 → EnemyAI.cs  / 전투 규칙 → LaneCombat.cs
    ///
    /// 플레이어/상대가 똑같이 하는 일은 Side(Player/Enemy)를 받는 함수 하나로 처리한다.
    /// 인스펙터에 연결된 필드 이름은 씬 연결이 끊기지 않도록 예전 이름 그대로 두었다.
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        [Header("씬 연결")]
        public GameObject cardViewPrefab; // 카드 한 장을 만들 때 복제하는 프리팹 (Assets/Prefabs/CardView.prefab)
        public HandZone playerHand;       // 플레이어 손패 (화면 아래)
        public HandZone enemyHand;        // 상대 손패 (화면 위)
        public FieldZone playerField;     // 플레이어 필드
        public FieldZone enemyField;      // 상대 필드

        [Header("덱 (DeckData 에셋을 연결)")]
        public DeckData playerDeckData;   // 플레이어가 쓰는 덱 (기본: 조선 시작 덱)
        public DeckData enemyDeckData;    // 상대가 쓰는 덱 (기본: 청 시작 덱)
        public int startingHandSize = GameRules.StartingHandSize; // 게임 시작 시 뽑는 장수

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
        static readonly Vector3 PlayerHealthPos = new Vector3(-7.2f, -3.7f, 0f); // 플레이어 체력 표시 위치
        static readonly Vector3 EnemyHealthPos = new Vector3(-7.2f, 3.7f, 0f);   // 상대 체력 표시 위치
        static readonly Vector3 ResultBannerPos = new Vector3(0f, 0.3f, 0f);     // 승패 배너 위치(화면 가운데)
        static readonly Vector2 ResultBackdropSize = new Vector2(30f, 3.2f);     // 승패 배너 뒤 어두운 띠 크기
        const float ResultTextHeight = 1.2f;   // "승리!" / "패배..." 글자 높이
        const float ResultHintHeight = 0.32f;  // 아래 안내 문구 글자 높이
        const int ResultSortingOrder = 32700;  // 모든 카드보다 위 (Unity 정렬 순서 최대값 32767 이하)

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

        // 이번 턴에 각 편이 낸 카드 수 (첫 턴 제한 GameRules.FirstTurnCardLimit 확인용, 턴이 바뀌면 0으로)
        int playerCardsPlayedThisTurn;
        int enemyCardsPlayedThisTurn;

        /// <summary>승패가 갈려서 게임이 끝났는지 (끝나면 턴 종료/카드 내기가 막힌다).</summary>
        public bool IsGameOver { get { return gameOver; } }

        bool busy; // 전투 연출이 진행 중인지 (진행 중엔 턴 종료/카드 내기/드래그를 막는다)

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
            HudFactory.SetHeight(gameOverText, ResultTextHeight, true);
            gameOverText.anchor = TextAnchor.MiddleCenter;
            gameOverText.alignment = TextAlignment.Center;
            var mr = gameOverText.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = ResultSortingOrder;
            HudFactory.EnsureBackdrop(gameOverText.transform, "Backdrop", ResultBackdropSize, GamePalette.ResultBackdrop, ResultSortingOrder - 1);

            if (gameOverText.transform.Find("Hint") == null)
            {
                var hint = HudFactory.CreateLabel(manaText, "Hint", gameOverText.transform.position + new Vector3(0f, -1.0f, 0f));
                hint.transform.SetParent(gameOverText.transform, true);
                HudFactory.SetHeight(hint, ResultHintHeight, false);
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
        /// (덱 빌더에서 덱을 저장해도 이 함수가 호출된다)
        /// </summary>
        public void ResetGame()
        {
            // 0) 진행 중인 전투 연출이 있으면 멈추고 정리
            StopAllCoroutines();
            CombatAnimation.ClearPopups();
            CardHoverPreview.Hide();
            busy = false;

            // 1) 이전 판의 카드 정리
            ClearHand(playerHand);
            ClearHand(enemyHand);
            ClearField(playerField);
            ClearField(enemyField);

            // 2) 턴/군력/체력/승패 초기화
            turnNumber = 1;
            playerMana.Reset();
            enemyMana.Reset();
            playerHealth = GameRules.StartingHealth;
            enemyHealth = GameRules.StartingHealth;
            SetGameOver(false, "", GamePalette.ResultDraw);
            ResetCardsPlayedThisTurn();

            // 3) 덱을 섞어 드로우 더미를 만들고 시작 손패를 번갈아 뽑는다
            playerDrawPile = BuildShuffledDeck(playerDeckData);
            enemyDrawPile = BuildShuffledDeck(enemyDeckData);
            for (int i = 0; i < startingHandSize; i++)
            {
                DrawCard(Side.Player);
                DrawCard(Side.Enemy);
            }

            // 4) 화면 갱신 후 상대 첫 수
            RefreshAllDisplays();
            PlayEnemyCards(); // 상대는 시작하자마자 첫 군력으로 낼 수 있는 카드를 낸다
        }

        /// <summary>
        /// 턴을 종료한다("턴 종료" 버튼이 호출). 레인 전투 → 승패 확인 → 다음 턴 시작 순서로 진행된다.
        /// Play 중이고 playCombatAnimation이 켜져 있으면 전투 연출을 보여준 뒤에 결과가 반영된다.
        /// (아직 플레이어 턴/상대 턴이 분리돼 있지 않아서, 매 턴 양쪽이 함께 드로우하고 상대는 곧바로 카드를 낸다)
        /// </summary>
        public void EndTurn()
        {
            if (gameOver || busy) return; // 이미 끝났거나 연출 중이면 무시

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
            yield return StartCoroutine(CombatAnimation.PlayAttacks(this, plan, HeroPoint(Side.Player), HeroPoint(Side.Enemy), manaText));
            LaneCombat.ApplyDamage(plan);                              // 이제 실제로 피해 적용 (체력 숫자 갱신)
            yield return StartCoroutine(CombatAnimation.PlayDeaths(plan.deadUnits)); // 죽은 카드가 사라지는 연출
            LaneCombat.RemoveDead(playerField, enemyField);            // 실제 제거
            busy = false;
            FinishCombat(plan);
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

            RefreshAllDisplays();
            PlayEnemyCards(); // 상대가 이번 턴 카드를 낸다
        }

        /// <summary>
        /// 손패 카드를 필드 슬롯에 내는 유일한 입구. 드래그(CardDragHandler)와 상대 AI가 모두 이 함수를 쓴다.
        /// 실패 조건: 게임 종료 / 카드·슬롯 없음 / 슬롯이 이미 참 / 군력 부족.
        /// </summary>
        public bool TryPlaceCard(CardView view, HandZone fromHand, FieldSlot slot)
        {
            if (gameOver || busy) return false; // 게임이 끝났거나 연출 중이면 못 냄
            if (view == null || view.data == null || fromHand == null || slot == null || !slot.IsEmpty) return false;

            Side side = SideOf(fromHand);                              // 어느 편의 카드인지
            if (!CanPlayMoreThisTurn(side)) return false;              // 첫 턴 장수 제한에 걸리면 실패
            if (!ManaOf(side).TrySpend(view.data.cost)) return false;  // 군력이 부족하면 실패

            fromHand.RemoveCard(view.transform);            // 손패에서 빼고
            FieldOf(side).PlaceCard(view.transform, slot);  // 필드 슬롯에 놓는다

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null) drag.ClearHand(); // 이제 손패 카드가 아니므로 드래그 연결을 끊는다

            if (side == Side.Player) playerCardsPlayedThisTurn++; // 이번 턴에 낸 장수 +1
            else enemyCardsPlayedThisTurn++;

            RefreshManaDisplay();
            return true;
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

        /// <summary>드래그 가능 여부 판단용: 이 손패가 플레이어 손패인지(상대 손패는 조작 불가).</summary>
        public bool IsPlayerHand(HandZone hand)
        {
            return hand == playerHand;
        }

        // ================= 드로우 =================

        /// <summary>
        /// 해당 편의 드로우 더미 맨 위에서 한 장을 뽑아 손패에 넣는다.
        /// 더미가 비었으면 아무 일도 없고(탈진 페널티는 미구현), 손패가 가득 찼으면 뽑은 카드는 버려진다(번).
        /// </summary>
        public void DrawCard(Side side)
        {
            var pile = DrawPileOf(side);
            var hand = HandOf(side);
            if (hand != null && pile != null && pile.Count > 0)
            {
                var card = pile[pile.Count - 1]; // 리스트 끝 = 더미 맨 위
                pile.RemoveAt(pile.Count - 1);
                if (hand.cards.Count < GameRules.MaxHandSize)
                    SpawnCardToHand(card, hand);
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

        /// <summary>덱 설계도를 카드 낱장 리스트로 펼치고 섞어서 돌려준다.</summary>
        static List<CardData> BuildShuffledDeck(DeckData data)
        {
            var list = data != null ? data.BuildCardList() : new List<CardData>();
            Shuffle(list);
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

                int pick = EnemyAI.ChooseCardToPlay(handCards, enemyMana.current); // 낼 카드 고르기
                if (pick < 0) break; // 낼 카드 없음

                var slot = EnemyAI.ChooseSlot(enemyField, handCards[pick]); // 놓을 자리 고르기
                if (slot == null) break; // 필드가 꽉 참

                if (!TryPlaceCard(handViews[pick], enemyHand, slot)) break; // 안전장치
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

        /// <summary>어느 쪽 체력이 0이 됐는지 보고 승리/패배/무승부를 정한다.</summary>
        void CheckGameOver()
        {
            bool playerDead = playerHealth <= 0;
            bool enemyDead = enemyHealth <= 0;
            if (!playerDead && !enemyDead) return; // 아직 둘 다 살아있음

            if (playerDead && enemyDead) SetGameOver(true, GameTexts.Draw, GamePalette.ResultDraw);   // 무승부
            else if (playerDead) SetGameOver(true, GameTexts.Defeat, GamePalette.ResultDefeat);     // 졌다
            else SetGameOver(true, GameTexts.Victory, GamePalette.ResultVictory);                    // 이겼다
        }

        /// <summary>게임 종료 상태를 바꾸고, 승패 배너(문구+색)를 보이거나 숨긴다.</summary>
        void SetGameOver(bool over, string message, Color color)
        {
            gameOver = over;
            if (gameOverText == null) return;
            gameOverText.text = message;
            gameOverText.color = color;
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
            UnityUtil.SetText(manaText, string.Format(GameTexts.PlayerMana, playerMana.current, playerMana.max, turnNumber));
            UnityUtil.SetText(enemyManaText, string.Format(GameTexts.EnemyMana, enemyMana.current, enemyMana.max));
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
            UnityUtil.SetText(playerHealthText, string.Format(GameTexts.PlayerHealth, playerHealth, GameRules.StartingHealth));
            UnityUtil.SetText(enemyHealthText, string.Format(GameTexts.EnemyHealth, enemyHealth, GameRules.StartingHealth));
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

        /// <summary>
        /// 필드의 카드를 치운다. 주의: FieldZone의 자식에는 슬롯 오브젝트 자체도 있으므로,
        /// 슬롯은 남기고 그 외 자식(=카드)만 지운다. (예전에 슬롯까지 지워서 카드가 안 내지던 버그가 있었음)
        /// </summary>
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
