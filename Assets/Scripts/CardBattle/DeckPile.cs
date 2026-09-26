using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 덱을 버튼 대신 "카드 뒷면이 쌓인 더미"로 보여주는 컴포넌트.
    ///   Battle 모드 : 배틀 화면의 드로우 더미. 남은 장수가 줄면 더미 높이도 낮아진다. (클릭 동작 없음)
    ///   Lobby 모드  : 처음 화면의 내 덱. 클릭하면 덱 편집 화면으로 간다.
    /// 카드 뒷면 그림은 Assets/Resources/CardBacks/CardBack.png (없으면 짙은 빨간 사각형으로 대신 그림).
    /// 더미 모양(두께, 크기)은 아래 숫자들만 바꾸면 된다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DeckPile : MonoBehaviour
    {
        public enum PileMode { Battle, Lobby } // Battle = 드로우 더미, Lobby = 덱 편집 열기

        public PileMode mode = PileMode.Battle; // 이 더미의 용도
        public Side side = Side.Player;         // Battle 모드: 누구의 드로우 더미인지
        public CardManager manager;             // 장수를 읽어올 게임 매니저
        public ScreenManager screens;           // Lobby 모드: 클릭하면 덱 편집 화면으로 (없으면 builder 패널을 연다)
        public DeckBuilderUI builder;           // Lobby 모드 예비: 화면 전환이 없을 때 열 덱 빌더

        public const string BackResourcePath = "CardBacks/CardBack"; // Resources 폴더 기준 카드 뒷면 그림 경로
        static readonly Vector2 CardSize = new Vector2(1.35f, 2.05f);  // 카드 한 장 크기(월드 유닛, CardView와 같음)
        static readonly Vector3 LayerOffset = new Vector3(0.025f, 0.03f, -0.001f); // 한 층 쌓일 때마다 밀리는 거리
        const int MaxLayers = 6;          // 최대 몇 층까지 쌓아 보일지
        const int CardsPerLayer = 4;      // 몇 장마다 한 층씩 보일지
        const int SortingOrderBase = 10;  // 더미의 그리기 순서 (필드 슬롯보다 위, 손패 카드보다 아래)
        const float LabelHeight = 0.42f;  // 아래 글자 높이
        const float LabelGap = 0.2f;      // 더미 아래쪽과 글자 사이 간격
        static readonly Color FallbackBack = new Color(0.45f, 0.08f, 0.08f); // 뒷면 그림이 없을 때 색

        readonly List<SpriteRenderer> layers = new List<SpriteRenderer>(); // 쌓인 뒷면들 (아래층부터)
        TextMesh label;          // "덱 N장" 글자
        int shownCount = -1;     // 지금 화면에 반영된 장수 (바뀌었을 때만 다시 그림)
        bool hovering;           // 마우스가 올라와 있는지 (Lobby 모드에서 밝게 표시)

        /// <summary>시작할 때 클릭 영역을 카드 크기에 맞추고 처음 모양을 그린다.</summary>
        void Start()
        {
            var col = GetComponent<BoxCollider2D>();
            col.size = CardSize + (Vector2)(LayerOffset * MaxLayers);
            col.offset = (Vector2)(LayerOffset * MaxLayers * 0.5f);
            Refresh();
        }

        /// <summary>매 프레임 장수를 확인해서 바뀌었으면 다시 그린다. (다른 코드가 더미를 따로 신경 쓸 필요 없음)</summary>
        void Update()
        {
            if (CurrentCount() != shownCount) Refresh();
        }

        /// <summary>지금 보여줄 장수. Battle = 남은 드로우 더미, Lobby = 저장된 내 덱 총 장수.</summary>
        public int CurrentCount()
        {
            if (manager == null) return 0;
            if (mode == PileMode.Battle)
            {
                var pile = manager.DrawPileOf(side);
                return pile != null ? pile.Count : 0;
            }
            return manager.playerDeckData != null ? manager.playerDeckData.TotalCount() : 0;
        }

        /// <summary>장수에 맞게 뒷면 층 수와 글자를 다시 그린다.</summary>
        public void Refresh()
        {
            shownCount = CurrentCount();
            int layerCount = shownCount <= 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(shownCount / (float)CardsPerLayer), 1, MaxLayers);
            if (mode == PileMode.Lobby) layerCount = MaxLayers; // 처음 화면에서는 항상 두툼하게

            EnsureLayers(layerCount);
            for (int i = 0; i < layers.Count; i++)
            {
                bool on = i < layerCount;
                layers[i].gameObject.SetActive(on);
                layers[i].color = hovering ? GamePalette.PileHover : Color.white;
                if (!on) continue;
                layers[i].transform.localPosition = LayerOffset * i;
                layers[i].sortingOrder = SortingOrderBase + i;
            }

            EnsureLabel();
            if (label != null)
            {
                string format = mode == PileMode.Lobby ? GameTexts.PileLobby : GameTexts.PileBattle;
                label.text = string.Format(format, shownCount);
            }
        }

        /// <summary>필요한 만큼 뒷면 스프라이트를 만든다(이미 있으면 재사용).</summary>
        void EnsureLayers(int count)
        {
            Sprite back = Resources.Load<Sprite>(BackResourcePath);
            while (layers.Count < Mathf.Max(count, 1))
            {
                var go = new GameObject("Back_" + layers.Count);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (back != null)
                {
                    sr.sprite = back;
                    Vector2 native = back.bounds.size; // 그림 원본 크기에 상관없이 카드 크기로 맞춘다
                    go.transform.localScale = new Vector3(CardSize.x / native.x, CardSize.y / native.y, 1f);
                }
                else
                {
                    sr.sprite = HudFactory.WhiteSprite(); // 그림이 없으면 색 사각형
                    go.transform.localScale = new Vector3(CardSize.x, CardSize.y, 1f);
                    sr.color = FallbackBack;
                }
                layers.Add(sr);
            }
        }

        /// <summary>더미 아래 "덱 N장" 글자를 처음 한 번 만든다(군력 글자와 같은 글꼴).</summary>
        void EnsureLabel()
        {
            if (label != null) return;
            label = HudFactory.CreateLabel(manager != null ? manager.manaText : null, "PileLabel", transform.position);
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, -CardSize.y * 0.5f - LabelGap, -0.01f);
            label.transform.localScale = Vector3.one;
            label.anchor = TextAnchor.UpperCenter;
            label.alignment = TextAlignment.Center;
            label.color = GamePalette.PileLabel;
            HudFactory.SetHeight(label, LabelHeight, true);
            var mr = label.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = SortingOrderBase + MaxLayers + 1;
        }

        /// <summary>마우스가 올라오면(처음 화면에서만) 살짝 밝게.</summary>
        void OnMouseEnter()
        {
            if (mode != PileMode.Lobby) return;
            hovering = true;
            Refresh();
        }

        /// <summary>마우스가 나가면 원래 색으로.</summary>
        void OnMouseExit()
        {
            if (!hovering) return;
            hovering = false;
            Refresh();
        }

        /// <summary>클릭: 처음 화면의 덱 더미면 덱 편집으로 간다.</summary>
        void OnMouseUpAsButton()
        {
            if (mode != PileMode.Lobby) return;
            hovering = false;
            if (screens != null) screens.ShowDeckEdit();
            else if (builder != null) builder.OpenPanel();
        }
    }
}
