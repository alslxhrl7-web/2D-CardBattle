using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 한 판의 진행을 총괄하는 매니저(씬에 하나만 둔다).
    ///
    /// 한 판의 흐름:
    ///   ResetGame()  : 손패/필드 정리 → 군력·체력 초기화 → 덱 셔플 → 시작 손패 드로우 → 상대 첫 수
    ///   EndTurn()    : 레인 전투(LaneCombat) → 승패 확인 → 다음 턴(군력 충전·양쪽 드로우) → 상대 수
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
        public GameObject cardViewPrefab;
        public HandZone playerHand;
        public HandZone enemyHand;
        public FieldZone playerField;
        public FieldZone enemyField;

        [Header("덱 (DeckData 에셋을 연결)")]
        public DeckData playerDeckData;
        public DeckData enemyDeckData;
        public int startingHandSize = GameRules.StartingHandSize;

        [Header("전투")]
        [Tooltip("끄면 턴 종료 시 레인 전투를 하지 않는다(카드 배치만 테스트할 때 사용)")]
        public bool enableLaneCombat = true;

        [Header("화면 표시 (연결 안 해도 동작함)")]
        public TextMesh manaText;            // "군력 X / Y (턴 N)"
        public TextMesh enemyManaText;       // "적 군력 X / Y"
        public TextMesh playerDeckCountText; // "덱 N장"
        public TextMesh enemyDeckCountText;
        public TextMesh playerHealthText;    // "체력 N / 30"
        public TextMesh enemyHealthText;
        public TextMesh gameOverText;        // 승패가 갈렸을 때만 켜진다

        [Header("현재 상태 (확인용 — 게임 시작 시 초기화됨)")]
        public int turnNumber = 1;
        public ManaPool playerMana = new ManaPool();
        public ManaPool enemyMana = new ManaPool();
        public int playerHealth = GameRules.StartingHealth;
        public int enemyHealth = GameRules.StartingHealth;

        // 셔플된 드로우 더미(런타임 전용). 덱 설계도(DeckData)와 달리 게임을 진행하며 줄어든다.
        [System.NonSerialized] public List<CardData> playerDrawPile = new List<CardData>();
        [System.NonSerialized] public List<CardData> enemyDrawPile = new List<CardData>();

        bool gameOver;
        public bool IsGameOver { get { return gameOver; } }

        // ==================================================================
        // 게임 흐름
        // ==================================================================

        void Start()
        {
            ResetGame();
        }

        /// <summary>
        /// 새 판을 시작한다. 씬이 어떤 상태로 저장돼 있었든 항상 같은 상태로 수렴하므로 몇 번 불러도 안전하다.
        /// (덱 빌더에서 덱을 저장하면 이 함수가 호출된다)
        /// </summary>
        public void ResetGame()
        {
            ClearHand(playerHand);
            ClearHand(enemyHand);
            ClearField(playerField);
            ClearField(enemyField);

            turnNumber = 1;
            playerMana.Reset();
            enemyMana.Reset();
            playerHealth = GameRules.StartingHealth;
            enemyHealth = GameRules.StartingHealth;
            SetGameOver(false, "");

            playerDrawPile = BuildShuffledDeck(playerDeckData);
            enemyDrawPile = BuildShuffledDeck(enemyDeckData);
            for (int i = 0; i < startingHandSize; i++)
            {
                DrawCard(Side.Player);
                DrawCard(Side.Enemy);
            }

            RefreshAllDisplays();
            PlayEnemyCards(); // 상대는 시작하자마자 첫 군력으로 낼 수 있는 카드를 낸다
        }

        /// <summary>
        /// 턴을 종료한다("턴 종료" 버튼이 호출). 레인 전투 → 승패 확인 → 다음 턴 시작 순서로 진행된다.
        /// (아직 플레이어 턴/상대 턴이 분리돼 있지 않아서, 매 턴 양쪽이 함께 드로우하고 상대는 곧바로 카드를 낸다)
        /// </summary>
        public void EndTurn()
        {
            if (gameOver) return;

            if (enableLaneCombat) ResolveCombat();
            if (gameOver) return;

            turnNumber++;
            playerMana.RefillForTurn(turnNumber);
            enemyMana.RefillForTurn(turnNumber);
            DrawCard(Side.Player);
            DrawCard(Side.Enemy);

            RefreshAllDisplays();
            PlayEnemyCards();
        }

        /// <summary>
        /// 손패 카드를 필드 슬롯에 내는 유일한 입구. 드래그(CardDragHandler)와 상대 AI가 모두 이 함수를 쓴다.
        /// 실패 조건: 게임 종료 / 카드·슬롯 없음 / 슬롯이 이미 참 / 군력 부족.
        /// </summary>
        public bool TryPlaceCard(CardView view, HandZone fromHand, FieldSlot slot)
        {
            if (gameOver) return false;
            if (view == null || view.data == null || fromHand == null || slot == null || !slot.IsEmpty) return false;

            Side side = SideOf(fromHand);
            if (!ManaOf(side).TrySpend(view.data.cost)) return false;

            fromHand.RemoveCard(view.transform);
            FieldOf(side).PlaceCard(view.transform, slot);

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null) drag.ClearHand();

            RefreshManaDisplay();
            return true;
        }

        /// <summary>드래그 가능 여부 판단용: 이 손패가 플레이어 손패인지(상대 손패는 조작 불가).</summary>
        public bool IsPlayerHand(HandZone hand)
        {
            return hand == playerHand;
        }

        // ==================================================================
        // 드로우
        // ==================================================================

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
                var card = pile[pile.Count - 1];
                pile.RemoveAt(pile.Count - 1);
                if (hand.cards.Count < GameRules.MaxHandSize)
                    SpawnCardToHand(card, hand);
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
            if (drag != null) drag.Init(this, hand);
            return view;
        }

        /// <summary>
        /// 카드 프리팹을 만들어 표시만 해둔다(손패·드래그 연결 없음). 덱 빌더 타일, 팩 개봉 결과처럼
        /// "보여주기만 하는 카드"를 만들 때도 이 함수를 쓴다. 드래그 핸들러는 Init이 안 되어 자동으로 비활성이다.
        /// </summary>
        public CardView SpawnCardView(CardData data, Vector3 position, Transform parent = null)
        {
            if (cardViewPrefab == null || data == null) return null;
            var go = Object.Instantiate(cardViewPrefab, position, Quaternion.identity, parent);
            go.name = "Card_" + data.cardNameEn;
            var view = go.GetComponent<CardView>();
            if (view != null) view.Setup(data);
            return view;
        }

        static List<CardData> BuildShuffledDeck(DeckData data)
        {
            var list = data != null ? data.BuildCardList() : new List<CardData>();
            Shuffle(list);
            return list;
        }

        /// <summary>Fisher-Yates 셔플(제자리에서 섞음).</summary>
        static void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        // ==================================================================
        // 상대 AI 실행 (무엇을 낼지 "판단"은 EnemyAI.cs)
        // ==================================================================

        void PlayEnemyCards()
        {
            if (gameOver || enemyHand == null || enemyField == null) return;

            // 한 번 돌 때마다 카드를 한 장 내거나 멈추므로 무한 반복이 될 수 없다.
            while (true)
            {
                var handCards = new List<CardData>();
                var handViews = new List<CardView>();
                foreach (var tf in enemyHand.cards)
                {
                    var view = tf != null ? tf.GetComponent<CardView>() : null;
                    handViews.Add(view);
                    handCards.Add(view != null ? view.data : null);
                }

                int pick = EnemyAI.ChooseCardToPlay(handCards, enemyMana.current);
                if (pick < 0) break;

                var slot = EnemyAI.ChooseSlot(enemyField, handCards[pick]);
                if (slot == null) break;

                if (!TryPlaceCard(handViews[pick], enemyHand, slot)) break;
            }
        }

        // ==================================================================
        // 전투 / 체력 / 승패
        // ==================================================================

        void ResolveCombat()
        {
            var result = LaneCombat.Resolve(playerField, enemyField);
            ChangeHealth(Side.Player, -result.damageToPlayerHero);
            ChangeHealth(Side.Enemy, -result.damageToEnemyHero);
            CheckGameOver();
        }

        /// <summary>히어로에게 피해를 준다(카드 효과 등에서 사용). 체력이 0이 되면 게임이 끝난다.</summary>
        public void DamageHero(Side side, int amount)
        {
            if (gameOver || amount <= 0) return;
            ChangeHealth(side, -amount);
            CheckGameOver();
        }

        public int HealthOf(Side side)
        {
            return side == Side.Player ? playerHealth : enemyHealth;
        }

        void ChangeHealth(Side side, int delta)
        {
            if (delta == 0) return;
            int value = Mathf.Max(0, HealthOf(side) + delta);
            if (side == Side.Player) playerHealth = value;
            else enemyHealth = value;
            RefreshHealthDisplay();
        }

        void CheckGameOver()
        {
            bool playerDead = playerHealth <= 0;
            bool enemyDead = enemyHealth <= 0;
            if (!playerDead && !enemyDead) return;

            string message = playerDead && enemyDead ? GameTexts.Draw
                           : playerDead ? GameTexts.Defeat
                           : GameTexts.Victory;
            SetGameOver(true, message);
        }

        void SetGameOver(bool over, string message)
        {
            gameOver = over;
            if (gameOverText == null) return;
            gameOverText.text = message;
            gameOverText.gameObject.SetActive(over);
        }

        // ==================================================================
        // 화면 표시
        // ==================================================================

        void RefreshAllDisplays()
        {
            RefreshManaDisplay();
            RefreshDeckCountDisplay();
            RefreshHealthDisplay();
        }

        void RefreshManaDisplay()
        {
            UnityUtil.SetText(manaText, string.Format(GameTexts.PlayerMana, playerMana.current, playerMana.max, turnNumber));
            UnityUtil.SetText(enemyManaText, string.Format(GameTexts.EnemyMana, enemyMana.current, enemyMana.max));
        }

        void RefreshDeckCountDisplay()
        {
            UnityUtil.SetText(playerDeckCountText, string.Format(GameTexts.DeckCount, playerDrawPile.Count));
            UnityUtil.SetText(enemyDeckCountText, string.Format(GameTexts.DeckCount, enemyDrawPile.Count));
        }

        void RefreshHealthDisplay()
        {
            UnityUtil.SetText(playerHealthText, string.Format(GameTexts.PlayerHealth, playerHealth, GameRules.StartingHealth));
            UnityUtil.SetText(enemyHealthText, string.Format(GameTexts.EnemyHealth, enemyHealth, GameRules.StartingHealth));
        }

        // ==================================================================
        // 편(Side)별로 알맞은 참조를 돌려주는 도우미 — 플레이어/상대 분기를 이 아래에만 모아둔다
        // ==================================================================

        public Side SideOf(HandZone hand) { return hand == playerHand ? Side.Player : Side.Enemy; }
        public HandZone HandOf(Side side) { return side == Side.Player ? playerHand : enemyHand; }
        public FieldZone FieldOf(Side side) { return side == Side.Player ? playerField : enemyField; }
        public ManaPool ManaOf(Side side) { return side == Side.Player ? playerMana : enemyMana; }
        public List<CardData> DrawPileOf(Side side) { return side == Side.Player ? playerDrawPile : enemyDrawPile; }

        // ==================================================================
        // 정리
        // ==================================================================

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
            UnityUtil.DestroyChildren(field.transform, child => slotTransforms.Contains(child));
        }
    }
}
