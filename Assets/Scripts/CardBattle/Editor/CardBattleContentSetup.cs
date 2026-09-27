using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardBattle.EditorTools
{
    /// <summary>
    /// 카드(유닛·장비·전술·진), 시작 덱·튜토리얼 덱, 덱 편집 타일, 덱 더미, 배틀 버튼, 처음 화면의 튜토리얼·2인 대전·온라인 대전 버튼을 한 번에 씬·에셋에 넣는 도구.
    /// 메뉴 "CardBattle/새 카드·덱·덱 더미 적용"으로 실행하며, CardBattleAutoApply가 컴파일 뒤 자동으로 한 번 불러준다.
    /// 여러 번 실행해도 안전하다: 이미 있는 카드 에셋과 이미 채워진 덱은 건드리지 않는다.
    ///
    /// 카드 수치를 바꾸고 싶으면: 에셋(Assets/CardData/...)을 인스펙터에서 직접 고치면 된다.
    ///   (아래 표는 "카드 에셋이 없을 때 처음 만드는 값"이다)
    /// </summary>
    public static class CardBattleContentSetup
    {
        /// <summary>카드 한 장을 만들 때 필요한 값 모음.</summary>
        class Spec
        {
            public string file;        // 에셋/그림 파일 이름 (확장자 없이)
            public string portrait;    // Assets/Portraits/ 안의 그림 파일 이름
            public string ko, en;      // 한글/영문 이름
            public Faction faction;    // 진영
            public CardKind kind;      // 종류
            public Rarity rarity;      // 희귀도
            public int cost, atk, hp;  // 비용, 공격력, 체력 (장비는 더해 줄 값)
            public string keyword;     // "키워드: 설명"
            public string flavor;      // 설정 문구
            public SpellEffect effect; // 전술 효과
            public int value;          // 전술 효과 크기
        }

        /// <summary>카드 하나를 표에 넣기 위한 짧은 도우미.</summary>
        static Spec S(string file, string portrait, string ko, string en, Faction f, CardKind k, Rarity r, int cost, int atk, int hp,
                      string keyword, string flavor, SpellEffect effect = SpellEffect.None, int value = 0)
        {
            return new Spec { file = file, portrait = portrait, ko = ko, en = en, faction = f, kind = k, rarity = r,
                              cost = cost, atk = atk, hp = hp, keyword = keyword, flavor = flavor, effect = effect, value = value };
        }

        // ================= 새 카드 표 (그림은 VARCO 3D로 생성) =================
        static readonly Spec[] NewCards =
        {
            // ---- 조선: 수성·방어 ----
            S("Joseon_Archer", "JoseonArcher", "조선 궁수", "Joseon Archer", Faction.Joseon, CardKind.Unit, Rarity.Common, 2, 2, 2,
              "Ranged: 상대 Wall 무시", "각궁 한 자루면 백 보 밖도 두렵지 않다."),
            S("Warrior_Monk", "WarriorMonk", "승병", "Warrior Monk", Faction.Joseon, CardKind.Unit, Rarity.Common, 3, 2, 5,
              "Wall: 받는 피해 -1", "산문을 닫고 나라를 지키러 내려왔다."),
            S("Pyeonjeon", "Pyeonjeon", "편전", "Pyeonjeon Arrows", Faction.Joseon, CardKind.Weapon, Rarity.Common, 1, 2, 0,
              "장비: 후열에 놓으면 앞 카드 공격력 +2", "통아에 넣어 쏘는 짧은 화살, 애기살."),
            S("Dujeonggap", "Dujeonggap", "두정갑", "Dujeonggap Armor", Faction.Joseon, CardKind.Weapon, Rarity.Elite, 2, 0, 3,
              "장비: 후열에 놓으면 앞 카드 체력 +3", "징을 박은 갑옷이 칼날을 튕겨낸다."),
            S("Singijeon", "Singijeon", "신기전", "Singijeon Barrage", Faction.Joseon, CardKind.Spell, Rarity.Elite, 4, 0, 0,
              "전술: 적 필드 전체에 2 피해", "화차에서 불화살 백 발이 솟구친다.", SpellEffect.DamageAllEnemyUnits, 2),
            S("Beacon_Fire", "BeaconFire", "봉수", "Beacon Fire", Faction.Joseon, CardKind.Spell, Rarity.Common, 2, 0, 0,
              "전술: 카드 2장 드로우", "다섯 줄기 연기, 적이 국경을 넘었다.", SpellEffect.DrawCards, 2),
            S("Wooden_Palisade", "Palisade", "목책", "Wooden Palisade", Faction.Joseon, CardKind.Formation, Rarity.Common, 2, 0, 6,
              "Wall: 받는 피해 -1", "뾰족한 말뚝이 기병의 발을 묶는다."),

            // ---- 청: 기동·공격 ----
            S("Banner_Spearman", "BannerSpearman", "팔기 창병", "Banner Spearman", Faction.Qing, CardKind.Unit, Rarity.Common, 2, 2, 3,
              "", "깃발 아래 창끝이 한 줄로 선다."),
            S("Mongol_Horse_Archer", "MongolHorseArcher", "몽골 기병", "Mongol Horse Archer", Faction.Qing, CardKind.Unit, Rarity.Elite, 4, 4, 3,
              "Ranged: 상대 Wall 무시", "달리는 말 위에서 뒤돌아 쏜다."),
            S("Manchu_Bow", "ManchuBow", "만주 각궁", "Manchu Bow", Faction.Qing, CardKind.Weapon, Rarity.Common, 2, 2, 1,
              "장비: 후열에 놓으면 앞 카드 +2/+1", "무거운 화살이 갑옷을 꿰뚫는다."),
            S("Iron_Barding", "IronBarding", "철갑 마갑", "Iron Barding", Faction.Qing, CardKind.Weapon, Rarity.Common, 2, 1, 2,
              "장비: 후열에 놓으면 앞 카드 +1/+2", "쇠 비늘을 두른 말은 화살을 두려워하지 않는다."),
            S("Hongyipo_Bombard", "Hongyipo", "홍이포 포격", "Hongyipo Bombard", Faction.Qing, CardKind.Spell, Rarity.Elite, 3, 0, 0,
              "전술: 적 히어로에게 4 피해", "성벽째 무너뜨리는 서양식 대포.", SpellEffect.DamageEnemyHero, 4),
            S("Surprise_Raid", "SurpriseRaid", "기습", "Surprise Raid", Faction.Qing, CardKind.Spell, Rarity.Common, 2, 0, 0,
              "전술: 아군 필드 전체 +1/+1", "눈보라를 뚫고 기병이 들이닥친다.", SpellEffect.BuffAllAllies, 1),
            S("Shield_Cart", "ShieldCart", "방패차", "Shield Cart", Faction.Qing, CardKind.Formation, Rarity.Common, 2, 0, 5,
              "Wall: 받는 피해 -1", "화살이 박혀도 수레는 멈추지 않는다."),
        };

        // ================= 시작 덱 (카드 파일 이름, 장수) — 각 21장 =================
        static readonly KeyValuePair<string, int>[] JoseonDeck =
        {
            D("Cannoneer", 2), D("Wall_Guard", 2), D("Righteous_Army_Militia", 2), D("Kim_Sangyong", 1), D("Im_Gyeongeop", 1),
            D("Joseon_Archer", 2), D("Warrior_Monk", 2), D("Pyeonjeon", 2), D("Dujeonggap", 2), D("Singijeon", 1),
            D("Beacon_Fire", 2), D("Wooden_Palisade", 2),
        };
        static readonly KeyValuePair<string, int>[] QingDeck =
        {
            D("Bannerman_Rider", 2), D("Mounted_Archer", 2), D("Steppe_Raider", 2), D("Dodo", 1), D("Yonggoldae", 1),
            D("Banner_Spearman", 2), D("Mongol_Horse_Archer", 2), D("Manchu_Bow", 2), D("Iron_Barding", 2), D("Hongyipo_Bombard", 1),
            D("Surprise_Raid", 2), D("Shield_Cart", 2),
        };
        static KeyValuePair<string, int> D(string file, int count) { return new KeyValuePair<string, int>(file, count); }

        // ================= 튜토리얼 덱 — 섞지 않고 위에서부터 순서대로 뽑는다 =================
        // 첫 손패 5장에 의병(1턴)·편전(2턴)·봉수(3턴)가 들어오도록 맨 앞에 둔다 (TutorialGuide 단계 순서).
        static readonly KeyValuePair<string, int>[] TutorialJoseonDeck =
        {
            D("Righteous_Army_Militia", 1), D("Pyeonjeon", 1), D("Beacon_Fire", 1), D("Wall_Guard", 1), D("Joseon_Archer", 1),
            D("Cannoneer", 1), D("Warrior_Monk", 1), D("Righteous_Army_Militia", 1), D("Kim_Sangyong", 1), D("Dujeonggap", 1),
            D("Wooden_Palisade", 1), D("Joseon_Archer", 1), D("Wall_Guard", 1), D("Singijeon", 1), D("Cannoneer", 1),
            D("Warrior_Monk", 1), D("Im_Gyeongeop", 1),
        };
        // 청 첫 손패에는 비용 1 카드가 없다 → 1턴에 청은 아무것도 못 내서, 의병이 살아남아 장비 단계를 할 수 있다.
        static readonly KeyValuePair<string, int>[] TutorialQingDeck =
        {
            D("Banner_Spearman", 1), D("Shield_Cart", 1), D("Banner_Spearman", 1), D("Mounted_Archer", 1), D("Bannerman_Rider", 1),
            D("Iron_Barding", 1), D("Banner_Spearman", 1), D("Steppe_Raider", 1), D("Mounted_Archer", 1), D("Shield_Cart", 1),
            D("Bannerman_Rider", 1), D("Banner_Spearman", 1), D("Steppe_Raider", 1), D("Mongol_Horse_Archer", 1),
        };

        const string CardFolder = "Assets/CardData";              // 카드 에셋 폴더 (진영별 하위 폴더)
        const string PortraitFolder = "Assets/Portraits";         // 초상화 폴더
        const string JoseonDeckPath = "Assets/DeckData/JoseonStarterDeck.asset"; // 플레이어 시작 덱
        const string QingDeckPath = "Assets/DeckData/QingStarterDeck.asset";     // 상대 시작 덱
        const string JoseonPackPath = "Assets/PackData/JoseonBasicPack.asset";   // 조선 기본팩
        const string TutorialJoseonPath = "Assets/DeckData/Tutorial_Joseon.asset"; // 튜토리얼 조선 덱
        const string TutorialQingPath = "Assets/DeckData/Tutorial_Qing.asset";     // 튜토리얼 청 덱

        // ---- 덱 편집 화면 타일 배치 (9칸 × 3줄 = 27칸) ----
        const int TileColumns = 9;           // 한 줄에 놓을 타일 수
        const float TileSpacingX = 2.05f;    // 타일 가로 간격
        static readonly float[] TileRowsY = { 3.5f, 0.9f, -1.7f }; // 줄마다의 y 위치 (위에서 아래로)
        const float TileScale = 0.55f;       // 타일 크기 (기존 타일과 같음)
        const float PanelMinWidth = 19.5f;   // 덱 편집 배경판 최소 가로 폭 (9칸이 들어가도록 넓힘)

        // ---- 덱 더미 위치 ----
        static readonly Vector3 PlayerPilePos = new Vector3(9.9f, -2.4f, 0f);  // 배틀: 내 드로우 더미 (오른쪽 아래)
        static readonly Vector3 EnemyPilePos = new Vector3(9.9f, 3.6f, 0f);    // 배틀: 상대 드로우 더미 (오른쪽 위)
        static readonly Vector3 LobbyPilePos = new Vector3(4.6f, -0.2f, 0f);   // 처음 화면: 내 덱 (클릭 → 덱 편집)
        const float PileScale = 0.8f;        // 배틀 화면 덱 더미 크기 배율
        static readonly Vector3 RestartButtonPos = new Vector3(7.2f, 5.85f, 0f); // 배틀: "다시 시작" (오른쪽 위)
        static readonly Vector3 QuitButtonPos = new Vector3(9.7f, 5.85f, 0f);    // 배틀: "게임 종료" (오른쪽 위 끝)
        const float LobbyPileScale = 1.1f;   // 처음 화면 덱 더미 크기 배율 (클릭하기 쉽게 조금 크게)
        static readonly Vector3 TutorialButtonPos = new Vector3(0f, -2.4f, 0f);  // 처음 화면: "튜토리얼" (팩 열기 아래)
        static readonly Vector3 TwoPlayerButtonPos = new Vector3(0f, -3.9f, 0f); // 처음 화면: "2인 대전"
        static readonly Vector3 OnlineButtonPos = new Vector3(0f, -5.4f, 0f);    // 처음 화면: "온라인 대전" (맨 아래)

        /// <summary>메뉴에서 실행: 모든 단계를 적용하고 씬을 저장한다.</summary>
        [MenuItem("CardBattle/새 카드·덱·덱 더미 적용", false, 30)]
        public static void ApplyFromMenu()
        {
            var log = new List<string>();
            Apply(log);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[CardBattle] 새 카드·덱·덱 더미 적용\n- " + string.Join("\n- ", log.ToArray()));
        }

        /// <summary>카드 에셋 → 덱 → 카드팩 → 덱 편집 타일 → 덱 더미 순서로 적용한다. (씬 저장은 호출한 쪽에서)</summary>
        public static void Apply(List<string> log)
        {
            CreateCards(log);
            WriteDeck(JoseonDeckPath, JoseonDeck, log);
            WriteDeck(QingDeckPath, QingDeck, log);
            RefillPack(log);
            var tutorialJoseon = WriteTutorialDeck(TutorialJoseonPath, "튜토리얼 조선", Faction.Joseon, TutorialJoseonDeck, log);
            var tutorialQing = WriteTutorialDeck(TutorialQingPath, "튜토리얼 청", Faction.Qing, TutorialQingDeck, log);
            AssetDatabase.SaveAssets();
            LayoutDeckBuilderTiles(log);
            SetupDeckPiles(log);
            SetupBattleButtons(log);
            SetupModeButtons(tutorialJoseon, tutorialQing, log);
        }

        // ================= 1) 카드 에셋 =================

        /// <summary>표의 카드 중 에셋이 없는 것만 만든다. (이미 있는 카드는 인스펙터에서 고친 값을 지키기 위해 건드리지 않음)</summary>
        static void CreateCards(List<string> log)
        {
            foreach (var s in NewCards)
            {
                string folder = CardFolder + "/" + s.faction;
                EnsureFolder(folder);
                string path = folder + "/" + s.file + ".asset";
                if (AssetDatabase.LoadAssetAtPath<CardData>(path) != null) continue; // 이미 있음

                var card = ScriptableObject.CreateInstance<CardData>();
                AssetDatabase.CreateAsset(card, path);
                card.cardNameKo = s.ko; card.cardNameEn = s.en; card.faction = s.faction; card.cardKind = s.kind;
                card.rarity = s.rarity; card.cost = s.cost; card.attack = s.atk; card.health = s.hp;
                card.keywordText = s.keyword; card.flavorText = s.flavor; card.spellEffect = s.effect; card.effectValue = s.value;
                var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitFolder + "/" + s.portrait + ".png");
                if (portrait != null) card.portrait = portrait;
                else log.Add("그림 없음: " + s.portrait + ".png (카드는 그림 없이 만들어짐)");
                EditorUtility.SetDirty(card);
                log.Add("카드 생성: " + path);
            }
        }

        /// <summary>진영 폴더(예: Assets/CardData/Qing)가 없으면 만든다.</summary>
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        /// <summary>카드 파일 이름으로 에셋을 찾는다(조선/청 폴더 모두).</summary>
        static CardData FindCard(string file)
        {
            foreach (var f in new[] { "Joseon", "Qing", "Ming" })
            {
                var c = AssetDatabase.LoadAssetAtPath<CardData>(CardFolder + "/" + f + "/" + file + ".asset");
                if (c != null) return c;
            }
            return null;
        }

        // ================= 2) 덱 =================

        /// <summary>덱이 비어 있으면 표대로 채운다. (이미 카드가 있으면 덱 편집 화면에서 고친 내용을 지키기 위해 건드리지 않음)</summary>
        static void WriteDeck(string path, KeyValuePair<string, int>[] list, List<string> log)
        {
            var deck = AssetDatabase.LoadAssetAtPath<DeckData>(path);
            if (deck == null) { log.Add("덱 없음: " + path); return; }
            if (deck.TotalCount() > 0) { log.Add("덱 유지: " + path + " (" + deck.TotalCount() + "장)"); return; }
            deck.entries = new List<DeckData.Entry>();
            foreach (var kv in list)
            {
                var card = FindCard(kv.Key);
                if (card == null) { log.Add("덱에 넣을 카드 없음: " + kv.Key); continue; }
                deck.entries.Add(new DeckData.Entry { card = card, count = kv.Value });
            }
            EditorUtility.SetDirty(deck);
            log.Add("덱 구성: " + path + " (" + deck.TotalCount() + "장)");
        }

        /// <summary>튜토리얼 덱은 순서가 중요해서 매번 표대로 다시 쓴다(없으면 만든다). 덱 편집 화면에는 나오지 않는다.</summary>
        static DeckData WriteTutorialDeck(string path, string deckName, Faction faction, KeyValuePair<string, int>[] list, List<string> log)
        {
            var deck = AssetDatabase.LoadAssetAtPath<DeckData>(path);
            if (deck == null)
            {
                deck = ScriptableObject.CreateInstance<DeckData>();
                AssetDatabase.CreateAsset(deck, path);
            }
            deck.deckName = deckName;
            deck.faction = faction;
            deck.entries = new List<DeckData.Entry>();
            foreach (var kv in list)
            {
                var card = FindCard(kv.Key);
                if (card == null) { log.Add("튜토리얼 덱에 넣을 카드 없음: " + kv.Key); continue; }
                deck.entries.Add(new DeckData.Entry { card = card, count = kv.Value });
            }
            EditorUtility.SetDirty(deck);
            log.Add("튜토리얼 덱: " + path + " (" + deck.TotalCount() + "장)");
            return deck;
        }

        /// <summary>조선 기본팩에 새 조선 카드도 들어가도록 카드 풀을 다시 채운다.</summary>
        static void RefillPack(List<string> log)
        {
            var pack = AssetDatabase.LoadAssetAtPath<CardPackData>(JoseonPackPath);
            if (pack == null) return;
            pack.AutoFillFromFaction();
            EditorUtility.SetDirty(pack);
            log.Add("조선 기본팩 카드 풀: " + pack.cardPool.Count + "장");
        }

        // ================= 3) 덱 편집 타일 =================

        /// <summary>
        /// 덱 편집 화면에 모든 카드(히어로 제외 없이 전부)의 타일이 있도록 기존 타일을 복제해 추가하고,
        /// 진영 → 종류 → 비용 순서로 9칸 × 3줄에 다시 배치한다.
        /// </summary>
        static void LayoutDeckBuilderTiles(List<string> log)
        {
            var ui = Object.FindAnyObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);
            if (ui == null || ui.tiles.Count == 0 || ui.tiles[0] == null) { log.Add("덱 편집 타일을 찾지 못함"); return; }
            var template = ui.tiles[0];
            var grid = template.transform.parent;

            // 모든 카드 에셋 모으기
            var all = new List<CardData>();
            foreach (var guid in AssetDatabase.FindAssets("t:CardData"))
            {
                var c = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                if (c != null) all.Add(c);
            }
            all.Sort(CompareForTiles);

            // 없는 카드의 타일 만들기
            var byCard = new Dictionary<CardData, DeckBuilderTile>();
            foreach (var t in ui.tiles) if (t != null && t.card != null) byCard[t.card] = t;
            int added = 0;
            foreach (var card in all)
            {
                if (byCard.ContainsKey(card)) continue;
                var go = Object.Instantiate(template.gameObject, grid);
                go.name = "Tile_" + card.cardNameEn;
                var tile = go.GetComponent<DeckBuilderTile>();
                tile.card = card;
                tile.ui = ui;
                tile.SetCount(0);
                byCard[card] = tile;
                added++;
            }

            // 순서대로 배치 + 카드 모양 갱신
            ui.tiles = new List<DeckBuilderTile>();
            for (int i = 0; i < all.Count; i++)
            {
                var tile = byCard[all[i]];
                int row = i / TileColumns, col = i % TileColumns;
                float x = (col - (TileColumns - 1) * 0.5f) * TileSpacingX;
                float y = row < TileRowsY.Length ? TileRowsY[row] : TileRowsY[TileRowsY.Length - 1] - (row - TileRowsY.Length + 1) * 2.6f;
                tile.transform.localPosition = new Vector3(x, y, 0f);
                tile.transform.localScale = Vector3.one * TileScale;
                var view = tile.GetComponent<CardView>();
                if (view != null) view.Setup(tile.card);
                EditorUtility.SetDirty(tile);
                ui.tiles.Add(tile);
            }
            EditorUtility.SetDirty(ui);

            // 배경판을 넓혀서 9칸이 다 들어가게
            var bg = ui.panelRoot != null ? ui.panelRoot.transform.Find("Background") : null;
            var sr = bg != null ? bg.GetComponent<SpriteRenderer>() : null;
            if (sr != null && sr.bounds.size.x < PanelMinWidth)
            {
                var s = bg.localScale;
                bg.localScale = new Vector3(s.x * PanelMinWidth / sr.bounds.size.x, s.y, s.z);
                EditorUtility.SetDirty(bg);
            }
            log.Add("덱 편집 타일 " + ui.tiles.Count + "개 (새로 " + added + "개)");
        }

        /// <summary>타일 순서: 조선 먼저, 같은 진영이면 유닛 → 진 → 장비 → 전술, 같은 종류면 비용 순.</summary>
        static int CompareForTiles(CardData a, CardData b)
        {
            int c = a.faction.CompareTo(b.faction);
            if (c != 0) return c;
            c = KindOrder(a.cardKind).CompareTo(KindOrder(b.cardKind));
            if (c != 0) return c;
            c = a.cost.CompareTo(b.cost);
            return c != 0 ? c : string.CompareOrdinal(a.cardNameEn, b.cardNameEn);
        }

        /// <summary>타일 정렬용 종류 순서.</summary>
        static int KindOrder(CardKind k)
        {
            switch (k)
            {
                case CardKind.Unit: return 0;
                case CardKind.Formation: return 1;
                case CardKind.Weapon: return 2;
                default: return 3;
            }
        }

        // ================= 4) 덱 더미 =================

        /// <summary>
        /// 배틀 화면: "덱 N장" 글자 대신 카드 뒷면 더미 2개(내 것/상대 것)를 놓는다.
        /// 처음 화면: 클릭하면 덱 편집 화면으로 가는 내 덱 더미를 놓는다.
        /// </summary>
        static void SetupDeckPiles(List<string> log)
        {
            var manager = Object.FindAnyObjectByType<CardManager>(FindObjectsInactive.Include);
            var screens = Object.FindAnyObjectByType<ScreenManager>(FindObjectsInactive.Include);
            if (manager == null || screens == null) { log.Add("덱 더미: CardManager/ScreenManager를 찾지 못함"); return; }

            // 배틀 화면 더미 2개
            var playerPile = EnsurePile("PlayerDeckPile", null, PlayerPilePos, DeckPile.PileMode.Battle, Side.Player, manager, screens);
            var enemyPile = EnsurePile("EnemyDeckPile", null, EnemyPilePos, DeckPile.PileMode.Battle, Side.Enemy, manager, screens);

            // 기존 "덱 N장" 글자는 숨기고, 배틀 화면 목록에서 더미로 바꾼다
            var battle = new List<GameObject>(screens.battleObjects ?? new GameObject[0]);
            foreach (var text in new[] { manager.playerDeckCountText, manager.enemyDeckCountText })
            {
                if (text == null) continue;
                battle.Remove(text.gameObject);
                text.gameObject.SetActive(false);
            }
            foreach (var pile in new[] { playerPile, enemyPile })
                if (!battle.Contains(pile.gameObject)) battle.Add(pile.gameObject);
            screens.battleObjects = battle.ToArray();

            // 처음 화면: 클릭하면 덱 편집으로 가는 덱 더미
            EnsurePile("LobbyDeckPile", LobbyRoot(screens), LobbyPilePos, DeckPile.PileMode.Lobby, Side.Player, manager, screens);
            EditorUtility.SetDirty(screens);
            log.Add("덱 더미 배치: 배틀 2개(내 덱/상대 덱), 처음 화면 1개(클릭 → 덱 편집)");
        }

        // ================= 5) 다시 시작 / 게임 종료 버튼 =================

        /// <summary>배틀 화면 오른쪽 위에 "다시 시작", "게임 종료" 버튼을 턴 종료 버튼을 복제해서 만든다(이미 있으면 위치만 맞춤).</summary>
        static void SetupBattleButtons(List<string> log)
        {
            var manager = Object.FindAnyObjectByType<CardManager>(FindObjectsInactive.Include);
            var screens = Object.FindAnyObjectByType<ScreenManager>(FindObjectsInactive.Include);
            var endTurn = Object.FindAnyObjectByType<EndTurnButton>(FindObjectsInactive.Include);
            if (manager == null || screens == null || endTurn == null) { log.Add("다시 시작/게임 종료 버튼: 턴 종료 버튼을 찾지 못함"); return; }

            var restart = EnsureButton<RestartButton>("RestartButton", "다시 시작", RestartButtonPos, endTurn.gameObject, screens);
            restart.manager = manager;
            var quit = EnsureButton<QuitGameButton>("QuitGameButton", "게임 종료", QuitButtonPos, endTurn.gameObject, screens);
            quit.screens = screens;
            EditorUtility.SetDirty(restart);
            EditorUtility.SetDirty(quit);
            EditorUtility.SetDirty(screens);
            log.Add("배틀 버튼: 다시 시작, 게임 종료");
        }

        /// <summary>이름으로 버튼을 찾고, 없으면 template(턴 종료 버튼)을 복제해 T 버튼으로 바꾼다. 배틀 화면 목록에도 넣는다.</summary>
        static T EnsureButton<T>(string name, string label, Vector3 position, GameObject template, ScreenManager screens) where T : ClickableButton
        {
            T button = null;
            foreach (var b in Object.FindObjectsByType<T>(FindObjectsInactive.Include)) if (b.name == name) { button = b; break; }
            if (button == null)
            {
                var go = Object.Instantiate(template, template.transform.parent);
                go.name = name;
                foreach (var old in go.GetComponents<ClickableButton>()) Object.DestroyImmediate(old); // 턴 종료 기능은 빼고
                button = go.AddComponent<T>();
                var bg = go.transform.Find("Background");
                button.background = bg != null ? bg.GetComponent<SpriteRenderer>() : go.GetComponentInChildren<SpriteRenderer>();
            }
            button.transform.position = position;
            var labelTf = button.transform.Find("Label");
            var text = labelTf != null ? labelTf.GetComponent<TextMesh>() : button.GetComponentInChildren<TextMesh>();
            if (text != null) { text.text = label; EditorUtility.SetDirty(text); }
            if (button.background != null) button.background.color = GamePalette.NeutralButton.normal;

            var battle = new List<GameObject>(screens.battleObjects ?? new GameObject[0]);
            if (!battle.Contains(button.gameObject)) battle.Add(button.gameObject);
            screens.battleObjects = battle.ToArray();
            return button;
        }

        // ================= 6) 튜토리얼 / 2인 대전 버튼 =================

        /// <summary>
        /// 처음 화면에 "튜토리얼", "2인 대전" 버튼을 "배틀 시작" 버튼을 복제해서 만들고(이미 있으면 위치·글자만 맞춤),
        /// CardManager에 튜토리얼 덱을 연결한다.
        /// </summary>
        static void SetupModeButtons(DeckData tutorialJoseon, DeckData tutorialQing, List<string> log)
        {
            var manager = Object.FindAnyObjectByType<CardManager>(FindObjectsInactive.Include);
            ScreenButton battleStart = null;
            foreach (var b in Object.FindObjectsByType<ScreenButton>(FindObjectsInactive.Include))
                if (b.name == "BattleStartButton") battleStart = b;
            if (manager == null || battleStart == null) { log.Add("튜토리얼/2인 대전 버튼: 배틀 시작 버튼을 찾지 못함"); return; }

            manager.tutorialPlayerDeck = tutorialJoseon;
            manager.tutorialEnemyDeck = tutorialQing;
            EditorUtility.SetDirty(manager);

            battleStart.mode = GameMode.VsAI;
            EditorUtility.SetDirty(battleStart);
            EnsureModeButton("TutorialButton", "튜토리얼", TutorialButtonPos, GameMode.Tutorial, battleStart);
            EnsureModeButton("TwoPlayerButton", "2인 대전", TwoPlayerButtonPos, GameMode.TwoPlayer, battleStart);
            EnsureModeButton("OnlineButton", "온라인 대전", OnlineButtonPos, GameMode.Online, battleStart);
            SetupOnlineMatch(manager, log);
            log.Add("처음 화면 버튼: 튜토리얼, 2인 대전, 온라인 대전 (+ 튜토리얼 덱 연결)");
        }

        /// <summary>
        /// 온라인 대전 오브젝트(OnlineMatch)를 씬에 하나 두고(없으면 만든다) CardManager와 연결하고,
        /// 상대 덱을 이름으로 찾을 수 있게 모든 카드 에셋을 목록에 넣는다. 서버 주소(serverUrl)는 이미 있으면 건드리지 않는다.
        /// </summary>
        static void SetupOnlineMatch(CardManager manager, List<string> log)
        {
            var online = Object.FindAnyObjectByType<OnlineMatch>(FindObjectsInactive.Include);
            if (online == null) online = new GameObject("OnlineMatch").AddComponent<OnlineMatch>();
            online.manager = manager;
            manager.online = online;

            online.cardLibrary = new List<CardData>();
            foreach (var guid in AssetDatabase.FindAssets("t:CardData"))
            {
                var card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                if (card != null) online.cardLibrary.Add(card);
            }
            EditorUtility.SetDirty(online);
            EditorUtility.SetDirty(manager);
            log.Add("온라인 대전: 카드 " + online.cardLibrary.Count + "장 등록, 서버 " + online.serverUrl);
        }

        /// <summary>이름으로 버튼을 찾고, 없으면 template(배틀 시작 버튼)을 같은 부모(Lobby) 아래에 복제한다.</summary>
        static void EnsureModeButton(string name, string label, Vector3 position, GameMode mode, ScreenButton template)
        {
            ScreenButton button = null;
            foreach (var b in Object.FindObjectsByType<ScreenButton>(FindObjectsInactive.Include))
                if (b.name == name) button = b;
            if (button == null)
            {
                button = Object.Instantiate(template.gameObject, template.transform.parent).GetComponent<ScreenButton>();
                button.name = name;
            }
            button.transform.position = position;
            button.target = GameScreen.Battle;
            button.mode = mode;
            var text = button.GetComponentInChildren<TextMesh>(true);
            if (text != null) { text.text = label; EditorUtility.SetDirty(text); }
            EditorUtility.SetDirty(button);
        }

        /// <summary>처음 화면 오브젝트 묶음(Lobby). 처음 화면 목록의 첫 번째 오브젝트를 쓴다.</summary>
        static Transform LobbyRoot(ScreenManager screens)
        {
            return screens.lobbyObjects != null && screens.lobbyObjects.Length > 0 && screens.lobbyObjects[0] != null
                ? screens.lobbyObjects[0].transform : null;
        }

        /// <summary>이름으로 더미를 찾고, 없으면 만든다. 위치/설정은 매번 맞춘다.</summary>
        static DeckPile EnsurePile(string name, Transform parent, Vector3 position, DeckPile.PileMode mode, Side side,
                                   CardManager manager, ScreenManager screens)
        {
            DeckPile pile = null;
            foreach (var p in Object.FindObjectsByType<DeckPile>(FindObjectsInactive.Include))
                if (p.name == name) { pile = p; break; }
            if (pile == null)
            {
                var go = new GameObject(name);
                go.AddComponent<BoxCollider2D>();
                pile = go.AddComponent<DeckPile>();
            }
            if (parent != null) pile.transform.SetParent(parent, true);
            pile.transform.position = position;
            pile.transform.localScale = Vector3.one * (mode == DeckPile.PileMode.Lobby ? LobbyPileScale : PileScale);
            pile.mode = mode;
            pile.side = side;
            pile.manager = manager;
            pile.screens = screens;
            EditorUtility.SetDirty(pile);
            return pile;
        }

    }
}
