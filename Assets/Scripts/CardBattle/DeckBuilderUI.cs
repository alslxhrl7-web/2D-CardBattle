using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 로스터(보유한 모든 카드)를 화면에 펼쳐두고, 카드를 클릭해서 매수를 정해 실제로 "덱을 구성"하는
    /// 가장 단순한 형태의 인게임 덱 빌더. "덱 편집" 버튼(DeckEditButton)으로 패널을 열고 닫으며,
    /// "저장" 버튼(DeckSaveButton)을 누르면 지금 타일들의 count를 그대로 targetDeck(DeckData 에셋)의
    /// entries에 반영해서 저장한다. 저장 즉시 CardManager.ResetGame()을 호출해 새 덱 구성으로
    /// 게임을 다시 시작하므로, 저장하자마자 바뀐 덱으로 플레이해볼 수 있다.
    /// </summary>
    public class DeckBuilderUI : MonoBehaviour
    {
        public GameObject panelRoot;            // 배경/타일/버튼/문구를 전부 묶은 루트(열고 닫는 대상)
        public DeckData targetDeck;              // 저장 대상 덱 에셋 (현재는 플레이어 덱 하나만 지원)
        public List<DeckBuilderTile> tiles = new List<DeckBuilderTile>();
        public TextMesh statusText;               // "덱 카드 수: N장" 안내 문구
        public CardManager manager;               // 저장 후 즉시 반영(ResetGame)하기 위한 참조

        public const int MinDeckSize = 10;        // 이보다 적으면 저장 버튼이 동작하지 않는다(최소 덱 크기)

        static readonly Color OkColor = new Color(0.55f, 0.85f, 0.45f);
        static readonly Color WarnColor = new Color(0.85f, 0.45f, 0.35f);

        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>패널을 열고 닫는다. 열릴 때는 항상 targetDeck의 최신 내용으로 타일들을 다시 맞춰준다(저장하지 않고 닫았던 편집 내용은 버려짐).</summary>
        public void TogglePanel()
        {
            if (panelRoot == null) return;
            bool next = !panelRoot.activeSelf;
            panelRoot.SetActive(next);
            if (next) LoadFromDeck();
        }

        /// <summary>덱 에셋에 지금 들어있는 매수를 읽어와 각 타일의 count/배지에 반영한다.</summary>
        void LoadFromDeck()
        {
            var counts = new Dictionary<CardData, int>();
            if (targetDeck != null)
            {
                foreach (var e in targetDeck.entries)
                    if (e != null && e.card != null) counts[e.card] = e.count;
            }

            foreach (var tile in tiles)
            {
                int c;
                counts.TryGetValue(tile.card, out c);
                tile.count = c;
                tile.UpdateBadge();
            }
            UpdateStatus();
        }

        /// <summary>타일 하나의 count가 바뀔 때마다(클릭할 때마다) DeckBuilderTile이 호출해준다.</summary>
        public void OnTileCountChanged()
        {
            UpdateStatus();
        }

        int CurrentTotal()
        {
            int total = 0;
            foreach (var tile in tiles) total += tile.count;
            return total;
        }

        void UpdateStatus()
        {
            if (statusText == null) return;
            int total = CurrentTotal();
            bool ok = total >= MinDeckSize;
            statusText.text = ok
                ? string.Format("덱 카드 수: {0}장 (저장 가능)", total)
                : string.Format("덱 카드 수: {0}장 (최소 {1}장 필요)", total, MinDeckSize);
            statusText.color = ok ? OkColor : WarnColor;
        }

        /// <summary>
        /// 지금 타일 상태(각 카드의 count)를 targetDeck 에셋의 entries로 그대로 옮겨 저장한다.
        /// "저장" 버튼은 누르는 즉시 항상 패널을 닫는다(최소 매수 미달이면 저장은 건너뛰고 닫기만 함) —
        /// 예전엔 최소 매수 미달 시 저장도 안 되고 패널도 안 닫혀서, 사용자가 보기엔 "저장 버튼이 안 먹힌다"처럼
        /// 보이는 문제가 있었음. 이제는 항상 닫히므로 그 문제가 재발하지 않는다.
        /// </summary>
        public void SaveDeck()
        {
            if (targetDeck != null && CurrentTotal() >= MinDeckSize)
            {
                targetDeck.entries = new List<DeckData.Entry>();
                foreach (var tile in tiles)
                {
                    if (tile.count <= 0) continue;
                    targetDeck.entries.Add(new DeckData.Entry { card = tile.card, count = tile.count });
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(targetDeck);
                UnityEditor.AssetDatabase.SaveAssets();
#endif

                if (manager != null) manager.ResetGame(); // 새로 구성한 덱으로 즉시 게임을 다시 시작해서 바로 확인할 수 있게 함
            }

            if (panelRoot != null) panelRoot.SetActive(false); // 저장 성공 여부와 무관하게 "저장" 버튼을 누르면 항상 패널을 닫는다
        }
    }
}
