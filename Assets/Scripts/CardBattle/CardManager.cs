using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 카드게임 한 판의 진행을 총괄하는 매니저.
    /// - 플레이어/상대의 "덱 설계도"(DeckData)를 받아 실제 셔플된 드로우 더미를 만들고,
    ///   게임 시작 시 시작 손패를 뽑고, 매 턴 종료마다 카드를 한 장씩 드로우한다.
    /// - 군력(마나) 자원과 턴 번호를 관리하며,
    /// - 손패의 카드를 필드에 배치하는 유일한 진입점(TryPlaceCard)을 제공한다.
    /// 씬에는 하나만 존재하는 것을 전제로 작성됨(별도 싱글턴 처리는 없고, 필요한 참조는 전부 인스펙터에서 직접 연결).
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        public GameObject cardViewPrefab;
        public HandZone playerHand;
        public HandZone enemyHand;
        public FieldZone playerField;
        public FieldZone enemyField;

        [Header("덱 (덱 빌딩은 DeckData 에셋을 만들어 여기에 연결하는 방식으로 한다)")]
        public DeckData playerDeckData;
        public DeckData enemyDeckData;
        public int startingHandSize = 5;     // 게임 시작 시 처음 뽑는 카드 수
        public const int MaxHandSize = 10;   // 손패 최대 장수. 가득 찬 상태에서 드로우하면 그 카드는 "번(burn)"되어 사라진다(실제 TCG의 표준 규칙)

        // 런타임 전용 상태: 셔플된 드로우 더미. DeckData(설계도)와 달리 게임을 진행하며 계속 줄어든다.
        // ResetGame()에서 DeckData.BuildCardList()로 새로 만들어지므로 인스펙터에 노출할 필요는 없지만,
        // 디버깅 편의를 위해 public으로 둔다.
        [System.NonSerialized] public List<CardData> playerDrawPile = new List<CardData>();
        [System.NonSerialized] public List<CardData> enemyDrawPile = new List<CardData>();

        [Header("군력(마나) - 플레이어")]
        public int turnNumber = 1;
        public int maxMana = 1;
        public int currentMana = 1;
        public const int ManaCap = 10;   // 군력 최대치. 턴이 아무리 늘어도 이 값을 넘지 않는다
        public TextMesh manaText;        // 화면에 "군력 X / Y (턴 N)"을 표시하는 3D 텍스트(ManaDisplay 오브젝트)

        [Header("군력(마나) - 상대(AI)")]
        public int maxEnemyMana = 1;
        public int currentEnemyMana = 1;
        public TextMesh enemyManaText;   // 화면에 "적 군력 X / Y"를 표시하는 3D 텍스트(EnemyManaDisplay 오브젝트)
        // 플레이어와 상대는 완전히 별도의 군력 풀을 갖는다 — 한쪽이 카드를 낸다고 다른 쪽 자원이 줄어들면 안 되기 때문.

        [Header("덱 매수 표시(선택)")]
        public TextMesh playerDeckCountText; // "덱 N장" 표시용(플레이어)
        public TextMesh enemyDeckCountText;  // "덱 N장" 표시용(상대)

        [Header("체력(HP) - 레인 전투")]
        public int playerHealth = StartingHealth;
        public int enemyHealth = StartingHealth;
        public const int StartingHealth = 30;
        public TextMesh playerHealthText;
        public TextMesh enemyHealthText;
        public TextMesh gameOverText; // 승패가 갈리면 텍스트를 채우고 활성화, 평소엔 비활성 상태로 둔다
        bool gameOver = false;

        void Start()
        {
            ResetGame();
        }

        /// <summary>
        /// 손패/필드에 남아있는 카드를 전부 정리하고 군력·턴을 기본값으로 되돌린 뒤,
        /// 양쪽 DeckData로부터 드로우 더미를 새로 셔플해서 만들고 시작 손패를 뽑는다.
        /// 씬이 저장된 시점의 상태가 무엇이었든 항상 같은 절차로 수렴하는, 몇 번을 호출해도 안전한(idempotent) 리셋 함수.
        /// </summary>
        public void ResetGame()
        {
            ClearHand(playerHand);
            ClearHand(enemyHand);
            ClearField(playerField);
            ClearField(enemyField);

            turnNumber = 1;
            maxMana = 1;
            currentMana = 1;
            maxEnemyMana = 1;
            currentEnemyMana = 1;

            playerHealth = StartingHealth;
            enemyHealth = StartingHealth;
            gameOver = false;
            if (gameOverText != null) gameOverText.gameObject.SetActive(false);
            UpdateHealthDisplay();

            // 덱 설계도(DeckData)를 실제 카드 낱장 리스트로 펼친 뒤 셔플해서 이번 판의 드로우 더미로 삼는다.
            playerDrawPile = BuildShuffledDeck(playerDeckData);
            enemyDrawPile = BuildShuffledDeck(enemyDeckData);

            for (int i = 0; i < startingHandSize; i++)
            {
                DrawCard(playerHand, playerDrawPile);
                DrawCard(enemyHand, enemyDrawPile);
            }

            UpdateManaDisplay();
            UpdateDeckCountDisplay();

            EnemyTryPlayCards(); // 상대도 시작하자마자 자기 첫 군력으로 낼 수 있는 카드가 있으면 바로 낸다
        }

        /// <summary>DeckData를 카드 낱장 리스트로 펼치고 Fisher-Yates로 섞은 새 리스트를 반환한다.</summary>
        static List<CardData> BuildShuffledDeck(DeckData data)
        {
            var list = data != null ? data.BuildCardList() : new List<CardData>();
            Shuffle(list);
            return list;
        }

        /// <summary>표준 Fisher-Yates 셔플. 리스트를 제자리에서(in-place) 무작위로 섞는다.</summary>
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

        /// <summary>
        /// 드로우 더미 맨 위(리스트 끝 인덱스)에서 카드 한 장을 뽑아 손패에 스폰한다.
        /// 더미가 비어있으면 아무 일도 일어나지 않는다(피로/탈진 데미지 같은 페널티는 아직 미구현 — 백로그 항목).
        /// 손패가 이미 최대 장수(10장)라면 카드는 뽑히자마자 "번(burn)"되어 사라진다(실제 TCG의 표준 규칙).
        /// </summary>
        public void DrawCard(HandZone hand, List<CardData> pile)
        {
            if (hand == null || pile == null || pile.Count == 0)
            {
                UpdateDeckCountDisplay();
                return;
            }

            var data = pile[pile.Count - 1];
            pile.RemoveAt(pile.Count - 1);

            if (hand.cards.Count < MaxHandSize)
                SpawnCardToHand(data, hand);
            // else: 손패가 가득 차서 카드가 번(burn)됨 — 더미에서는 이미 제거되었으므로 그냥 사라진 것으로 처리

            UpdateDeckCountDisplay();
        }

        void UpdateDeckCountDisplay()
        {
            if (playerDeckCountText != null)
                playerDeckCountText.text = "덱 " + playerDrawPile.Count + "장";
            if (enemyDeckCountText != null)
                enemyDeckCountText.text = "덱 " + enemyDrawPile.Count + "장";
        }

        /// <summary>
        /// Object.Destroy는 플레이 모드에서만 동작하고, 에디터 스크립트로 에디트 모드에서 호출하면
        /// "Destroy may not be called from edit mode!" 오류를 낸다. 두 모드 모두에서 안전하게 쓰기 위한 헬퍼.
        /// </summary>
        static void DestroyGameObject(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 손패를 완전히 비운다. cards 리스트에 "추적된" 카드만 지우는 것이 아니라 HandZone의 실제
        /// 자식 오브젝트 전부를 지운다 — 과거 테스트/리셋 코드가 리스트에 등록하지 않은 채 카드를 자식으로
        /// 남겨두면(고아 오브젝트) 겉보기엔 멀쩡해 보여도 카드가 겹쳐 쌓이는 원인이 되므로, 리스트가 아니라
        /// 하이어라키 자체를 기준으로 청소해서 이런 고아가 절대 남지 않게 한다.
        /// </summary>
        static void ClearHand(HandZone hand)
        {
            if (hand == null) return;
            for (int i = hand.transform.childCount - 1; i >= 0; i--)
                DestroyGameObject(hand.transform.GetChild(i).gameObject);
            hand.cards.Clear();
        }

        /// <summary>
        /// 필드에 놓인 카드를 전부 치운다. 주의: FieldZone의 자식에는 카드뿐 아니라 "슬롯"
        /// 오브젝트 자체(FrontSlot_-4 등, 필드의 고정 구조물)도 함께 들어있으므로, HandZone처럼
        /// 자식 전체를 지우면 슬롯 자체가 파괴되어 필드가 통째로 망가진다(실제로 이 버그 때문에
        /// 슬롯이 전부 사라져서 카드가 하나도 안 내지는 사고가 있었음). 그래서 frontRow/backRow에
        /// 등록된 슬롯 트랜스폼은 제외하고, 그 외의 자식(=카드)만 지운다.
        /// </summary>
        static void ClearField(FieldZone field)
        {
            if (field == null) return;

            var slotTransforms = new HashSet<Transform>();
            if (field.frontRow != null) foreach (var s in field.frontRow) if (s != null) slotTransforms.Add(s.transform);
            if (field.backRow != null) foreach (var s in field.backRow) if (s != null) slotTransforms.Add(s.transform);

            for (int i = field.transform.childCount - 1; i >= 0; i--)
            {
                var child = field.transform.GetChild(i);
                if (slotTransforms.Contains(child)) continue; // 슬롯 고정 구조물은 건드리지 않는다
                DestroyGameObject(child.gameObject);
            }

            foreach (var row in new[] { field.frontRow, field.backRow })
            {
                if (row == null) continue;
                foreach (var slot in row) if (slot != null) slot.occupant = null;
            }
        }

        /// <summary>카드 프리팹을 인스턴스화해 지정한 손패에 넣고, CardView 표시 초기화와 드래그 핸들러 연결까지 처리한다.</summary>
        public CardView SpawnCardToHand(CardData data, HandZone hand)
        {
            if (cardViewPrefab == null || data == null || hand == null) return null;
            GameObject go = Object.Instantiate(cardViewPrefab, hand.transform.position, Quaternion.identity);
            go.name = "Card_" + data.cardNameEn;
            var view = go.GetComponent<CardView>();
            if (view != null) view.Setup(data);
            hand.AddCard(go.transform);

            var drag = go.GetComponent<CardDragHandler>();
            if (drag != null) drag.Init(this, hand);

            return view;
        }

        /// <summary>드래그 가능 여부 판단용: 이 손패가 "플레이어" 손패인지(상대 손패는 아직 드래그 조작 불가).</summary>
        public bool IsPlayerHand(HandZone hand)
        {
            return hand == playerHand;
        }

        /// <summary>
        /// 지정한 손패(플레이어/상대) 쪽의 군력 풀에서 비용을 소모한다. 충분하면 소모하고 true,
        /// 부족하면 아무 것도 하지 않고 false. 어느 쪽 군력을 쓸지는 hand가 playerHand인지로 판단한다.
        /// </summary>
        bool TrySpendMana(HandZone hand, int amount)
        {
            if (amount < 0) amount = 0;
            bool isPlayer = (hand == playerHand);
            if (isPlayer)
            {
                if (amount > currentMana) return false;
                currentMana -= amount;
            }
            else
            {
                if (amount > currentEnemyMana) return false;
                currentEnemyMana -= amount;
            }
            UpdateManaDisplay();
            return true;
        }

        /// <summary>
        /// 턴을 종료한다: 턴 번호를 올리고, 양쪽(플레이어/상대)의 최대 군력을 (턴 번호, ManaCap) 중
        /// 작은 값으로 올린 뒤 각자의 현재 군력을 최대치로 가득 채우고, 양쪽 다 드로우 더미에서
        /// 카드를 한 장씩 뽑는다. 그 다음 상대 AI가 자신의 군력으로 낼 수 있는 카드를 필드에 낸다.
        /// (아직 진짜 "턴 교대"가 구현되어 있지 않아 — 플레이어 턴/상대 턴이 분리돼 있지 않음 — 임시로
        /// 매 턴 종료 시 양쪽 모두 드로우 + 상대는 즉시 카드까지 내게 해뒀다. 실제 턴 교대 시스템이
        /// 생기면 각자 자기 턴에만 드로우/플레이하도록 바뀔 예정.)
        /// 화면의 "턴 종료" 버튼(EndTurnButton)이 클릭 시 이 메서드를 호출한다.
        /// </summary>
        public void EndTurn()
        {
            turnNumber++;
            maxMana = Mathf.Min(ManaCap, turnNumber);
            currentMana = maxMana;
            maxEnemyMana = Mathf.Min(ManaCap, turnNumber);
            currentEnemyMana = maxEnemyMana;

            DrawCard(playerHand, playerDrawPile);
            DrawCard(enemyHand, enemyDrawPile);

            UpdateManaDisplay();

            EnemyTryPlayCards();
        }

        void UpdateManaDisplay()
        {
            if (manaText != null)
                manaText.text = string.Format("군력 {0} / {1}   (턴 {2})", currentMana, maxMana, turnNumber);
            if (enemyManaText != null)
                enemyManaText.text = string.Format("적 군력 {0} / {1}", currentEnemyMana, maxEnemyMana);
        }

        /// <summary>
        /// 아주 단순한 상대(청) AI: 자신의 군력이 허용하는 한, 손패에서 낼 수 있는 카드 중
        /// "지금 낼 수 있는 것 중 가장 비싼(=군력을 가장 알차게 쓰는) 카드"를 골라 빈 슬롯에 반복해서 낸다
        /// (전열을 우선 채우고, 전열이 다 차면 후열). 낼 카드가 없거나 빈 슬롯이 없으면 멈춘다 —
        /// 매 반복마다 손패가 한 장씩 줄어들거나(카드를 냄) 즉시 break하므로 무한 루프가 될 수 없다.
        /// 우선순위/포지셔닝 전략을 갖춘 정식 AI가 생기기 전까지의 임시 동작.
        /// </summary>
        void EnemyTryPlayCards()
        {
            if (enemyHand == null || enemyField == null) return;

            while (true)
            {
                CardView bestView = null;

                foreach (var tf in enemyHand.cards)
                {
                    var view = tf.GetComponent<CardView>();
                    if (view == null || view.data == null) continue;
                    if (view.data.cost > currentEnemyMana) continue;
                    if (bestView == null || view.data.cost > bestView.data.cost)
                        bestView = view;
                }

                if (bestView == null) break; // 지금 군력으로 더 낼 수 있는 카드가 없음

                var slot = enemyField.GetFirstEmpty(true) ?? enemyField.GetFirstEmpty(false);
                if (slot == null) break; // 필드가 꽉 참

                if (!TryPlaceCard(bestView, enemyHand, slot))
                    break; // 안전장치: 혹시라도 실패하면 무한 루프를 막기 위해 중단
            }
        }

        /// <summary>
        /// 손패 카드를 필드 슬롯에 배치하는 유일한 진입점. 마나 체크 → 손패에서 제거(재배치 트리거) →
        /// 필드존에 배치 → 드래그 핸들러의 손패 참조 해제까지 한 번에 처리한다.
        /// 실패 조건: 카드/슬롯이 없거나, 슬롯이 이미 차 있거나, 군력이 부족한 경우.
        /// </summary>
        public bool TryPlaceCard(CardView view, HandZone fromHand, FieldSlot slot)
        {
            if (view == null || view.data == null || fromHand == null || slot == null || !slot.IsEmpty)
                return false;

            if (!TrySpendMana(fromHand, view.data.cost))
                return false;

            fromHand.RemoveCard(view.transform);

            var field = (fromHand == playerHand) ? playerField : enemyField;
            field.PlaceCard(view.transform, slot);

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null) drag.ClearHand();

            return true;
        }
    }
}
