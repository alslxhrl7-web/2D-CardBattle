using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 게임 안의 덱 빌더 패널. 로스터 카드(타일)를 클릭해 매수를 정하고 "저장"하면 DeckData 에셋에 반영된다.
    /// 조선 덱과 청 덱을 바꿔 가며 편집한다(패널의 "○ 덱 편집" 버튼). 타일은 진영마다 따로 있고, 지금 편집 중인 진영 것만 보인다.
    ///
    ///   OpenPanel()     : 덱 편집 화면에 들어갈 때 ScreenManager가 부른다. 두 덱에 저장된 내용으로 타일을 다시 맞춘다
    ///                     (저장 안 하고 나간 편집 내용은 버려짐).
    ///   SwitchFaction() : 조선 ↔ 청. 바꿔도 편집 중인 매수는 타일에 남아 있다.
    ///   SaveDeck()      : 두 덱 중 장수가 GameRules.MinDeckSize 이상인 덱을 저장한다. 그리고 처음 화면으로 돌아간다.
    ///
    /// 저장 위치: 에디터에서는 덱 에셋 파일, 게임(웹·PC 빌드)에서는 PlayerPrefs("카드이름:장수,…", 진영마다 따로).
    /// 빌드는 에셋 파일을 고칠 수 없어서, 게임이 켜질 때 PlayerPrefs에 저장된 덱을 덱 에셋에 다시 채워 넣는다.
    /// (웹에서는 PlayerPrefs가 브라우저 저장소에 남는다. 브라우저 사이트 데이터를 지우면 시작 덱으로 돌아감)
    /// </summary>
    public class DeckBuilderUI : MonoBehaviour
    {
        public GameObject panelRoot;                 // 열고 닫을 패널 전체
        public DeckData targetDeck;                  // 조선 덱 (= CardManager.playerDeckData)
        public DeckData qingDeck;                    // 청 덱 (= CardManager.enemyDeckData)
        public List<DeckBuilderTile> tiles = new List<DeckBuilderTile>(); // 패널에 깔린 카드 타일들 (두 진영 모두)
        public TextMesh statusText;                  // "덱 카드 수: N장" 안내
        public TextMesh titleText;                   // "덱 편집 - 조선"
        public TextMesh switchLabel;                 // 진영 바꾸기 버튼 글자
        public ScreenManager screens;                // 저장 후 처음 화면으로 돌아가기 위한 화면 전환 담당
        public Faction editing = Faction.Joseon;     // 지금 편집 중인 진영

        public const string SaveKey = "CardBattle.PlayerDeck";   // PlayerPrefs 이름: 조선 덱
        public const string QingSaveKey = "CardBattle.QingDeck"; // PlayerPrefs 이름: 청 덱
        static readonly Faction[] Factions = { Faction.Joseon, Faction.Qing };

        /// <summary>게임 시작 시 패널은 닫힌 상태로 둔다. 빌드에서는 저장해 둔 덱을 불러온다.</summary>
        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
#if !UNITY_EDITOR
            LoadSavedDeck(); // 에디터는 덱 에셋 파일에 바로 저장되므로 불러올 필요가 없다
#endif
        }

        /// <summary>진영의 덱 에셋.</summary>
        DeckData DeckOf(Faction faction) { return faction == Faction.Qing ? qingDeck : targetDeck; }
        /// <summary>진영의 PlayerPrefs 이름.</summary>
        static string KeyOf(Faction faction) { return faction == Faction.Qing ? QingSaveKey : SaveKey; }

        /// <summary>
        /// PlayerPrefs에 저장된 두 덱("카드이름:장수,…")을 덱 에셋에 채운다. 카드는 그 진영 타일에서 이름으로 찾는다.
        /// 저장된 게 없거나, 모르는 카드뿐이거나, 장수가 최소 장수보다 적으면 그 덱 에셋은 그대로 둔다.
        /// </summary>
        public void LoadSavedDeck()
        {
            foreach (var faction in Factions)
            {
                string saved = PlayerPrefs.GetString(KeyOf(faction), "");
                if (DeckOf(faction) == null || saved == "") continue;

                var byName = new Dictionary<string, DeckBuilderTile>();
                foreach (var tile in TilesOf(faction)) byName[tile.card.name] = tile;

                var entries = new List<DeckData.Entry>();
                int total = 0;
                foreach (var item in saved.Split(','))
                {
                    string[] parts = item.Split(':');
                    DeckBuilderTile tile;
                    int count;
                    if (parts.Length != 2 || !byName.TryGetValue(parts[0], out tile) || !int.TryParse(parts[1], out count)) continue;
                    count = Mathf.Clamp(count, 0, GameRules.MaxCopies(tile.card.rarity)); // 규칙보다 많이 저장돼 있으면 줄인다
                    if (count <= 0) continue;
                    entries.Add(new DeckData.Entry { card = tile.card, count = count });
                    total += count;
                }
                if (total >= GameRules.MinDeckSize) DeckOf(faction).entries = entries;
            }
        }

        /// <summary>패널을 연다(저장된 덱 내용을 다시 불러옴). 덱 편집 화면으로 들어갈 때 ScreenManager가 부른다.</summary>
        public void OpenPanel()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
            LoadFromDecks();
            ShowFaction(editing);
        }

        /// <summary>패널을 닫는다(저장 안 한 편집 내용은 버려짐).</summary>
        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>조선 덱 ↔ 청 덱 편집 전환 (진영 바꾸기 버튼이 부른다).</summary>
        public void SwitchFaction()
        {
            ShowFaction(editing == Faction.Joseon ? Faction.Qing : Faction.Joseon);
        }

        /// <summary>타일 매수가 바뀔 때마다 DeckBuilderTile이 호출한다 → 안내 문구 갱신.</summary>
        public void OnTileCountChanged()
        {
            RefreshStatus();
        }

        /// <summary>장수가 충분한 덱을 저장한다. 저장 여부와 상관없이 처음 화면으로 돌아간다(패널도 같이 닫힘).</summary>
        public void SaveDeck()
        {
            foreach (var faction in Factions)
                if (DeckOf(faction) != null && TotalOf(faction) >= GameRules.MinDeckSize) WriteTilesToDeck(faction);
            if (screens != null) screens.ShowLobby();
            else ClosePanel();
        }

        // ================= 내부 처리 =================

        /// <summary>그 진영 카드의 타일들.</summary>
        List<DeckBuilderTile> TilesOf(Faction faction)
        {
            return tiles.FindAll(t => t != null && t.card != null && t.card.faction == faction);
        }

        /// <summary>그 진영 타일만 보이게 하고 제목·버튼 글자·안내 문구를 맞춘다.</summary>
        void ShowFaction(Faction faction)
        {
            editing = faction;
            foreach (var tile in tiles) if (tile != null && tile.card != null) tile.gameObject.SetActive(tile.card.faction == faction);
            UnityUtil.SetText(titleText, string.Format(GameTexts.DeckBuilderTitle, FactionName(faction)));
            UnityUtil.SetText(switchLabel, string.Format(GameTexts.DeckBuilderSwitch, FactionName(faction == Faction.Joseon ? Faction.Qing : Faction.Joseon)));
            RefreshStatus();
        }

        /// <summary>진영 이름 ("조선" / "청").</summary>
        static string FactionName(Faction faction) { return faction == Faction.Qing ? GameTexts.QingName : GameTexts.JoseonName; }

        /// <summary>두 덱에 저장된 내용을 읽어 각 타일의 매수와 최대 매수를 맞춘다.</summary>
        void LoadFromDecks()
        {
            // 카드별 저장된 매수표를 만든다 (두 덱 모두)
            var counts = new Dictionary<CardData, int>();
            foreach (var faction in Factions)
            {
                var deck = DeckOf(faction);
                if (deck == null) continue;
                foreach (var entry in deck.entries)
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
        }

        /// <summary>그 진영 타일 매수를 덱 에셋에 그대로 써넣고 저장한다(에디터: 에셋 파일, 게임: PlayerPrefs).</summary>
        void WriteTilesToDeck(Faction faction)
        {
            var deck = DeckOf(faction);
            deck.entries = new List<DeckData.Entry>();
            var saved = new List<string>(); // PlayerPrefs에 남길 "카드이름:장수"
            foreach (var tile in TilesOf(faction))
            {
                if (tile.count <= 0) continue; // 0장인 카드는 넣지 않음
                deck.entries.Add(new DeckData.Entry { card = tile.card, count = tile.count });
                saved.Add(tile.card.name + ":" + tile.count);
            }
            // 게임(빌드)에서도 다음에 켤 때 남아 있도록 PlayerPrefs에 저장 (웹은 Save를 불러야 브라우저에 기록된다)
            PlayerPrefs.SetString(KeyOf(faction), string.Join(",", saved.ToArray()));
            PlayerPrefs.Save();
#if UNITY_EDITOR
            // 에디터에서는 에셋 파일에도 저장해서 Play를 멈춰도 유지되게 한다
            UnityEditor.EditorUtility.SetDirty(deck);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>그 진영 타일 매수의 합.</summary>
        int TotalOf(Faction faction)
        {
            int total = 0;
            foreach (var tile in TilesOf(faction)) total += tile.count;
            return total;
        }

        /// <summary>"덱 카드 수: N장" 문구와 색(충족=연두, 미달=빨강)을 지금 편집 중인 덱 기준으로 갱신한다.</summary>
        void RefreshStatus()
        {
            if (statusText == null) return;
            int total = TotalOf(editing);
            bool ok = total >= GameRules.MinDeckSize;
            statusText.text = ok
                ? string.Format(GameTexts.DeckBuilderReady, total)
                : string.Format(GameTexts.DeckBuilderTooSmall, total, GameRules.MinDeckSize);
            statusText.color = ok ? GamePalette.TextOk : GamePalette.TextWarn;
        }
    }
}
