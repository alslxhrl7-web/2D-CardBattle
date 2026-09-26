using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardBattle.EditorTools
{
    /// <summary>
    /// 새 카드(유닛·장비·전술·진) 14장, 시작 덱 2개, 덱 편집 타일, 덱 더미를 한 번에 만들어 넣는 도구.
    /// 메뉴 "CardBattle/새 카드·덱·덱 더미 적용"으로 실행하며, CardBattleAutoApply가 컴파일 뒤 자동으로 한 번 불러준다.
    /// 이미 있는 카드 에셋은 새로 만들지 않고 값만 아래 표대로 맞춘다(여러 번 실행해도 안전).
    ///
    /// 카드 수치를 바꾸고 싶으면: 에셋(Assets/CardData/...)을 인스펙터에서 직접 고치면 된다.
    ///   (이 표는 "처음 만들 때 넣는 값"이다. 이 메뉴를 다시 실행하면 표의 값으로 돌아가니 주의)
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

        // ================= 규칙이 바뀌어서 설명을 고쳐야 하는 기존 카드 (파일 이름, 새 키워드 문구) =================
        // 공격은 전열에서만 하고, Ranged는 "상대 Wall 무시"로 바뀌었다.
        static readonly KeyValuePair<string, string>[] KeywordFixes =
        {
            new KeyValuePair<string, string>("Cannoneer", "Ranged: 상대 Wall 무시"),
            new KeyValuePair<string, string>("Mounted_Archer", "Ranged, Hit and Run: 상대 Wall 무시"),
            new KeyValuePair<string, string>("Wall_Guard", "Wall: 받는 피해 -1"),
        };

        const string CardFolder = "Assets/CardData";              // 카드 에셋 폴더 (진영별 하위 폴더)
        const string PortraitFolder = "Assets/Portraits";         // 초상화 폴더
        const string JoseonDeckPath = "Assets/DeckData/JoseonStarterDeck.asset"; // 플레이어 시작 덱
        const string QingDeckPath = "Assets/DeckData/QingStarterDeck.asset";     // 상대 시작 덱
        const string JoseonPackPath = "Assets/PackData/JoseonBasicPack.asset";   // 조선 기본팩

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
        static readonly Vector3 LobbyPackButtonPos = new Vector3(0f, -0.9f, 0f); // 덱 편집 버튼이 빠진 자리로 팩 열기 버튼을 올림
        const float PileScale = 0.8f;        // 배틀 화면 덱 더미 크기 배율
        const float LobbyPileScale = 1.1f;   // 처음 화면 덱 더미 크기 배율 (클릭하기 쉽게 조금 크게)

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
            FixOldKeywords(log);
            WriteDeck(JoseonDeckPath, JoseonDeck, log);
            WriteDeck(QingDeckPath, QingDeck, log);
            RefillPack(log);
            AssetDatabase.SaveAssets();
            LayoutDeckBuilderTiles(log);
            SetupDeckPiles(log);
        }

        // ================= 1) 카드 에셋 =================

        /// <summary>표의 카드를 에셋으로 만들거나(없을 때) 값을 맞춘다. 파일 이름 → 카드 사전을 돌려준다.</summary>
        static Dictionary<string, CardData> CreateCards(List<string> log)
        {
            var result = new Dictionary<string, CardData>();
            foreach (var s in NewCards)
            {
                string folder = CardFolder + "/" + s.faction;
                EnsureFolder(folder);
                string path = folder + "/" + s.file + ".asset";
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                bool created = card == null;
                if (created)
                {
                    card = ScriptableObject.CreateInstance<CardData>();
                    AssetDatabase.CreateAsset(card, path);
                }
                card.cardNameKo = s.ko; card.cardNameEn = s.en; card.faction = s.faction; card.cardKind = s.kind;
                card.rarity = s.rarity; card.cost = s.cost; card.attack = s.atk; card.health = s.hp;
                card.keywordText = s.keyword; card.flavorText = s.flavor; card.spellEffect = s.effect; card.effectValue = s.value;
                var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitFolder + "/" + s.portrait + ".png");
                if (portrait != null) card.portrait = portrait;
                else log.Add("그림 없음: " + s.portrait + ".png (카드는 그림 없이 만들어짐)");
                EditorUtility.SetDirty(card);
                result[s.file] = card;
                if (created) log.Add("카드 생성: " + path);
            }
            return result;
        }

        /// <summary>기존 카드의 키워드 설명을 바뀐 규칙에 맞게 고친다.</summary>
        static void FixOldKeywords(List<string> log)
        {
            foreach (var kv in KeywordFixes)
            {
                var card = FindCard(kv.Key);
                if (card == null || card.keywordText == kv.Value) continue;
                card.keywordText = kv.Value;
                EditorUtility.SetDirty(card);
                log.Add("설명 수정: " + card.cardNameKo + " → " + kv.Value);
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

        /// <summary>덱 에셋의 내용을 표대로 바꾼다.</summary>
        static void WriteDeck(string path, KeyValuePair<string, int>[] list, List<string> log)
        {
            var deck = AssetDatabase.LoadAssetAtPath<DeckData>(path);
            if (deck == null) { log.Add("덱 없음: " + path); return; }
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
        /// 처음 화면: "덱 편집" 버튼 대신 클릭할 수 있는 내 덱 더미를 놓는다.
        /// </summary>
        static void SetupDeckPiles(List<string> log)
        {
            var manager = Object.FindAnyObjectByType<CardManager>(FindObjectsInactive.Include);
            var screens = Object.FindAnyObjectByType<ScreenManager>(FindObjectsInactive.Include);
            var builder = Object.FindAnyObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);
            if (manager == null || screens == null) { log.Add("덱 더미: CardManager/ScreenManager를 찾지 못함"); return; }

            // 배틀 화면 더미 2개
            var playerPile = EnsurePile("PlayerDeckPile", null, PlayerPilePos, DeckPile.PileMode.Battle, Side.Player, manager, screens, builder);
            var enemyPile = EnsurePile("EnemyDeckPile", null, EnemyPilePos, DeckPile.PileMode.Battle, Side.Enemy, manager, screens, builder);

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

            // 처음 화면: 덱 편집 버튼 → 덱 더미
            var lobby = GameObject.Find("Lobby");
            var editButton = Object.FindAnyObjectByType<DeckEditButton>(FindObjectsInactive.Include);
            Transform lobbyParent = lobby != null ? lobby.transform : null;
            EnsurePile("LobbyDeckPile", lobbyParent, LobbyPilePos, DeckPile.PileMode.Lobby, Side.Player, manager, screens, builder);
            if (lobbyParent == null)
            {
                var lobbyList = new List<GameObject>(screens.lobbyObjects ?? new GameObject[0]);
                var pileGo = GameObject.Find("LobbyDeckPile");
                if (pileGo != null && !lobbyList.Contains(pileGo)) lobbyList.Add(pileGo);
                screens.lobbyObjects = lobbyList.ToArray();
            }
            if (editButton != null && editButton.gameObject.activeSelf)
            {
                editButton.gameObject.SetActive(false); // 버튼은 지우지 않고 꺼둔다 (필요하면 다시 켜면 됨)
                var pack = Object.FindAnyObjectByType<PackOpenButton>(FindObjectsInactive.Include);
                if (pack != null) pack.transform.position = LobbyPackButtonPos; // 빈자리로 팩 열기 버튼을 올림
            }
            EditorUtility.SetDirty(screens);
            log.Add("덱 더미 배치: 배틀 2개(내 덱/상대 덱), 처음 화면 1개(클릭 → 덱 편집)");
        }

        /// <summary>이름으로 더미를 찾고, 없으면 만든다. 위치/설정은 매번 맞춘다.</summary>
        static DeckPile EnsurePile(string name, Transform parent, Vector3 position, DeckPile.PileMode mode, Side side,
                                   CardManager manager, ScreenManager screens, DeckBuilderUI builder)
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
            pile.builder = builder;
            EditorUtility.SetDirty(pile);
            return pile;
        }
    }
}
