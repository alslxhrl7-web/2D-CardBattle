using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 게임 안의 덱 빌더 패널. 로스터 카드(타일)를 클릭해 매수를 정하고 "저장"하면 DeckData 에셋에 반영된다.
    ///
    ///   OpenPanel() : 덱 편집 화면에 들어갈 때 ScreenManager가 부른다. targetDeck에 저장된 내용으로 타일을 다시 맞춘다
    ///                 (저장 안 하고 나간 편집 내용은 버려짐).
    ///   SaveDeck()  : 총 장수가 GameRules.MinDeckSize 이상이면 저장한다. 장수와 상관없이 처음 화면으로 돌아간다.
    ///
    /// 최소 장수/카드별 최대 매수 규칙은 GameRules.cs, 안내 문구는 GameTexts.cs에서 바꾼다.
    /// </summary>
    public class DeckBuilderUI : MonoBehaviour
    {
        public GameObject panelRoot;                 // 열고 닫을 패널 전체
        public DeckData targetDeck;                  // 편집 대상 덱 (현재는 플레이어 덱)
        public List<DeckBuilderTile> tiles = new List<DeckBuilderTile>(); // 패널에 깔린 카드 타일들
        public TextMesh statusText;                  // "덱 카드 수: N장" 안내
        public ScreenManager screens;                // 저장 후 처음 화면으로 돌아가기 위한 화면 전환 담당

        /// <summary>게임 시작 시 패널은 닫힌 상태로 둔다.</summary>
        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>패널을 연다(저장된 덱 내용을 다시 불러옴). 덱 편집 화면으로 들어갈 때 ScreenManager가 부른다.</summary>
        public void OpenPanel()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
            LoadFromDeck();
        }

        /// <summary>패널을 닫는다(저장 안 한 편집 내용은 버려짐).</summary>
        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>타일 매수가 바뀔 때마다 DeckBuilderTile이 호출한다 → 안내 문구 갱신.</summary>
        public void OnTileCountChanged()
        {
            RefreshStatus();
        }

        /// <summary>장수가 충분하면 덱을 저장한다. 저장 여부와 상관없이 처음 화면으로 돌아간다(패널도 같이 닫힘).</summary>
        public void SaveDeck()
        {
            if (targetDeck != null && CurrentTotal() >= GameRules.MinDeckSize) WriteTilesToDeck();
            if (screens != null) screens.ShowLobby();
            else ClosePanel();
        }

        // ================= 내부 처리 =================

        /// <summary>저장된 덱 내용을 읽어 각 타일의 매수와 최대 매수를 맞춘다.</summary>
        void LoadFromDeck()
        {
            // 카드별 저장된 매수표를 만든다
            var counts = new Dictionary<CardData, int>();
            if (targetDeck != null)
            {
                foreach (var entry in targetDeck.entries)
                    if (entry != null && entry.card != null) counts[entry.card] = entry.count;
            }

            foreach (var tile in tiles)
            {
                if (tile == null || tile.card == null) continue;
                CardHoverPreview.AttachTo(tile.gameObject); // 타일도 마우스를 올리면 크게 보이도록
                tile.maxCount = GameRules.MaxCopies(tile.card.rarity); // 희귀도 규칙으로 최대 매수 설정
                int saved;
                counts.TryGetValue(tile.card, out saved);            // 덱에 없으면 0
                tile.SetCount(saved);
            }
            RefreshStatus();
        }

        /// <summary>현재 타일 매수를 덱 에셋에 그대로 써넣고 파일로 저장한다.</summary>
        void WriteTilesToDeck()
        {
            targetDeck.entries = new List<DeckData.Entry>();
            foreach (var tile in tiles)
            {
                if (tile == null || tile.card == null || tile.count <= 0) continue; // 0장인 카드는 넣지 않음
                targetDeck.entries.Add(new DeckData.Entry { card = tile.card, count = tile.count });
            }
#if UNITY_EDITOR
            // 에디터에서는 에셋 파일에도 저장해서 Play를 멈춰도 유지되게 한다
            UnityEditor.EditorUtility.SetDirty(targetDeck);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>모든 타일 매수의 합.</summary>
        int CurrentTotal()
        {
            int total = 0;
            foreach (var tile in tiles) if (tile != null) total += tile.count;
            return total;
        }

        /// <summary>"덱 카드 수: N장" 문구와 색(충족=연두, 미달=빨강)을 갱신한다.</summary>
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
