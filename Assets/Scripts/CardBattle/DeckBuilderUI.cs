using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 인게임 덱 빌더 패널. 로스터 카드(타일)를 클릭해 매수를 정하고 "저장"하면 DeckData 에셋에 반영된다.
    ///
    ///   TogglePanel() : 패널 열기/닫기. 열 때마다 targetDeck에 저장된 내용으로 타일을 다시 맞춘다
    ///                   (저장 안 하고 닫은 편집 내용은 버려짐).
    ///   SaveDeck()    : 총 장수가 GameRules.MinDeckSize 이상이면 저장하고 새 덱으로 게임을 다시 시작한다.
    ///                   장수와 상관없이 패널은 항상 닫힌다.
    ///
    /// 최소 장수/카드별 최대 매수 규칙은 GameRules.cs, 안내 문구는 GameTexts.cs에서 바꾼다.
    /// </summary>
    public class DeckBuilderUI : MonoBehaviour
    {
        public GameObject panelRoot;                 // 열고 닫을 패널 루트
        public DeckData targetDeck;                  // 편집 대상 덱 (현재는 플레이어 덱)
        public List<DeckBuilderTile> tiles = new List<DeckBuilderTile>();
        public TextMesh statusText;                  // "덱 카드 수: N장" 안내
        public CardManager manager;                  // 저장 후 게임 재시작용

        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void TogglePanel()
        {
            if (panelRoot == null) return;
            bool open = !panelRoot.activeSelf;
            panelRoot.SetActive(open);
            if (open) LoadFromDeck();
        }

        /// <summary>타일 매수가 바뀔 때마다 DeckBuilderTile이 호출한다.</summary>
        public void OnTileCountChanged()
        {
            RefreshStatus();
        }

        public void SaveDeck()
        {
            if (targetDeck != null && CurrentTotal() >= GameRules.MinDeckSize)
            {
                WriteTilesToDeck();
                if (manager != null) manager.ResetGame(); // 바뀐 덱으로 바로 새 판 시작
            }

            // 저장 성공 여부와 무관하게 항상 닫는다(예전엔 장수 미달이면 안 닫혀서 버튼이 고장난 것처럼 보였음)
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ------------------------------------------------------------------

        void LoadFromDeck()
        {
            var counts = new Dictionary<CardData, int>();
            if (targetDeck != null)
            {
                foreach (var entry in targetDeck.entries)
                    if (entry != null && entry.card != null) counts[entry.card] = entry.count;
            }

            foreach (var tile in tiles)
            {
                if (tile == null || tile.card == null) continue;
                tile.maxCount = GameRules.MaxCopies(tile.card.rarity);
                int saved;
                counts.TryGetValue(tile.card, out saved);
                tile.SetCount(saved);
            }
            RefreshStatus();
        }

        void WriteTilesToDeck()
        {
            targetDeck.entries = new List<DeckData.Entry>();
            foreach (var tile in tiles)
            {
                if (tile == null || tile.card == null || tile.count <= 0) continue;
                targetDeck.entries.Add(new DeckData.Entry { card = tile.card, count = tile.count });
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(targetDeck);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        int CurrentTotal()
        {
            int total = 0;
            foreach (var tile in tiles) if (tile != null) total += tile.count;
            return total;
        }

        void RefreshStatus()
        {
            if (statusText == null) return;
            int total = CurrentTotal();
            bool ok = total >= GameRules.MinDeckSize;
            statusText.text = ok
                ? string.Format(GameTexts.DeckBuilderReady, total)
                : string.Format(GameTexts.DeckBuilderTooSmall, total, GameRules.MinDeckSize);
            statusText.color = ok ? GamePalette.TextOk : GamePalette.TextWarn;
        }
    }
}
