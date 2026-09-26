using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace CardBattle.EditorTools
{
    /// <summary>
    /// 에디터 상단 메뉴 "CardBattle"에 들어가는 도구 모음. (Editor 폴더 안이라 게임 빌드에는 포함되지 않는다)
    ///
    /// CardBattle/게임 시작:
    ///   게임 씬(SampleScene)을 열고, 씬 구성이 덜 됐으면 자동 구성까지 한 뒤 Play를 누른다. 이것만 누르면 게임이 시작된다.
    ///
    /// CardBattle/씬 자동 구성:
    ///   씬을 열어둔 상태에서 한 번 누르면, 코드에는 있지만 씬에 아직 없는 것들을 만들어 연결한다.
    ///   이미 있는 것은 건드리지 않으므로 여러 번 눌러도 안전하다.
    ///     - 필드 슬롯 점검/복구 (FieldSlot 컴포넌트, 콜라이더, 왼쪽→오른쪽 순서)
    ///     - 체력 표시 2개 + 승패 문구
    ///     - 조선 기본팩 에셋 (Assets/PackData/JoseonBasicPack.asset)
    ///     - "팩 열기" 버튼 + 팩 결과 패널
    ///     - 화면 나누기: 처음 화면(제목·배틀 시작·덱 편집·팩 열기) / 배틀 화면(카드·턴 종료) / 덱 편집 화면
    ///
    /// CardBattle/카드팩 확률 확인:
    ///   프로젝트 창에서 CardPackData 에셋을 선택하고 누르면, 희귀도별 확률과 1000팩 시뮬레이션 결과를 콘솔에 보여준다.
    /// </summary>
    public static class CardBattleSetupMenu
    {
        const string GameScenePath = "Assets/Scenes/SampleScene.unity";          // 게임이 들어있는 씬
        const string PackFolder = "Assets/PackData";                             // 카드팩 에셋을 두는 폴더
        const string JoseonPackPath = PackFolder + "/JoseonBasicPack.asset";     // 조선 기본팩 에셋 경로
        const string AbilityBoxSpritePath = "Assets/UI/CardFrame/AbilityBox.png"; // 패널 배경으로 쓸 둥근 사각형 그림

        // 화면 배치 (카메라 세로 ±6.5 기준). 왼쪽 열 = 상태 표시, 오른쪽 열 = 버튼
        static readonly Vector3 PlayerHealthPos = new Vector3(-7.2f, -3.7f, 0f); // 플레이어 체력 표시 위치
        static readonly Vector3 EnemyHealthPos = new Vector3(-7.2f, 3.7f, 0f);   // 상대 체력 표시 위치
        static readonly Vector3 PackButtonPos = new Vector3(7.0f, -3.7f, 0f);    // "팩 열기" 버튼 위치 (턴 종료 버튼 위)
        const int GameOverSortingOrder = 32700;   // 승패 문구의 그리기 순서 (모든 카드/패널보다 위, 최대값 32767 이하)

        // 처음 화면 배치 (화면 가운데 세로 줄)
        static readonly Vector3 LobbyTitlePos = new Vector3(0f, 3.0f, 0f);         // 제목
        static readonly Vector3 LobbyBattleButtonPos = new Vector3(0f, 0.6f, 0f);  // "배틀 시작"
        static readonly Vector3 LobbyDeckButtonPos = new Vector3(0f, -0.9f, 0f);   // "덱 편집"
        static readonly Vector3 LobbyPackButtonPos = new Vector3(0f, -2.4f, 0f);   // "팩 열기"
        const string LobbyTitle = "조선 vs 청 카드 배틀";                           // 처음 화면 제목

        // ================= 메뉴: 게임 시작 =================

        /// <summary>
        /// 게임 씬을 열고(필요하면 자동 구성까지) Play 모드로 들어간다.
        /// 저장 안 한 씬이 열려 있으면 먼저 저장할지 물어본다.
        /// </summary>
        [MenuItem("CardBattle/게임 시작", false, 0)]
        public static void StartGame()
        {
            if (EditorApplication.isPlaying) return; // 이미 게임 중

            // 1) 지금 열린 씬에 저장 안 한 변경이 있으면 저장할지 물어본다(취소하면 중단)
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 2) 게임 씬 열기 (이미 열려 있으면 그대로)
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
            {
                EditorUtility.DisplayDialog("CardBattle", GameScenePath + " 씬을 찾지 못했습니다.", "확인");
                return;
            }
            if (EditorSceneManager.GetActiveScene().path != GameScenePath)
                EditorSceneManager.OpenScene(GameScenePath);

            // 3) 체력 표시나 팩 UI가 아직 없으면 자동 구성을 먼저 한다
            var manager = Object.FindFirstObjectByType<CardManager>(FindObjectsInactive.Include);
            bool needsSetup = manager != null
                && (manager.playerHealthText == null
                    || Object.FindFirstObjectByType<PackOpenerUI>(FindObjectsInactive.Include) == null
                    || Object.FindFirstObjectByType<ScreenManager>(FindObjectsInactive.Include) == null);
            if (needsSetup) SetupScene();

            // 4) Play 시작 → 처음 화면이 뜨고, "배틀 시작"을 누르면 CardManager.ResetGame()으로 새 판이 시작된다
            EditorApplication.EnterPlaymode();
        }

        // ================= 메뉴: 씬 자동 구성 =================

        /// <summary>씬에 빠진 것(슬롯, 체력 표시, 팩 에셋, 팩 UI)을 만들어 연결하고 씬을 저장한다.</summary>
        [MenuItem("CardBattle/씬 자동 구성", false, 20)]
        public static void SetupScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardBattle", "플레이 모드를 끈 뒤에 실행해 주세요.", "확인");
                return;
            }

            var manager = Object.FindFirstObjectByType<CardManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                EditorUtility.DisplayDialog("CardBattle", "씬에서 CardManager를 찾지 못했습니다. SampleScene을 연 뒤 다시 실행해 주세요.", "확인");
                return;
            }

            var log = new List<string>(); // 이번에 바꾼 내용 기록 (마지막에 콘솔에 출력)
            RepairFieldSlots(manager.playerField, log);
            RepairFieldSlots(manager.enemyField, log);
            EnsureHealthTexts(manager, log);
            var pack = EnsureJoseonPack(log);
            EnsurePackUI(manager, pack, log);
            EnsureScreens(manager, log);

            // 씬을 변경됨으로 표시하고 저장한다
            EditorUtility.SetDirty(manager);
            var scene = manager.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (log.Count == 0) log.Add("이미 모두 구성되어 있어서 바꾼 것이 없습니다.");
            Debug.Log("[CardBattle] 씬 자동 구성 완료\n- " + string.Join("\n- ", log.ToArray()));
        }

        // ================= 메뉴: 카드팩 확률 확인 =================

        /// <summary>선택한 카드팩(없으면 조선 기본팩)의 설정 확률과 1000팩 시뮬레이션 결과를 콘솔에 출력한다.</summary>
        [MenuItem("CardBattle/카드팩 확률 확인", false, 40)]
        public static void ReportPackOdds()
        {
            var pack = Selection.activeObject as CardPackData; // 프로젝트 창에서 선택한 에셋
            if (pack == null) pack = AssetDatabase.LoadAssetAtPath<CardPackData>(JoseonPackPath);
            if (pack == null)
            {
                EditorUtility.DisplayDialog("CardBattle", "프로젝트 창에서 CardPackData 에셋을 선택한 뒤 다시 실행해 주세요.", "확인");
                return;
            }

            // 팩을 1000번 열어 희귀도별로 몇 장 나왔는지 센다
            const int packs = 1000;
            var counts = new Dictionary<Rarity, int>();
            int total = 0;
            for (int i = 0; i < packs; i++)
            {
                foreach (var card in CardPackOpener.Open(pack))
                {
                    int c;
                    counts.TryGetValue(card.rarity, out c);
                    counts[card.rarity] = c + 1;
                    total++;
                }
            }

            // 희귀도별로 한 줄씩: 설정 확률 | 시뮬레이션 결과
            var lines = new List<string>();
            foreach (Rarity r in System.Enum.GetValues(typeof(Rarity)))
            {
                int c;
                counts.TryGetValue(r, out c);
                lines.Add(string.Format("{0,-10} 설정 확률 {1,6:P1} | {2}팩 시뮬레이션 {3,6:P1} ({4}장)",
                    r, pack.ChanceOf(r), packs, total > 0 ? (float)c / total : 0f, c));
            }
            Debug.Log(string.Format("[CardBattle] {0} (카드 풀 {1}장, 팩당 {2}장, 보장 슬롯 {3})\n{4}",
                pack.packName, pack.cardPool.Count, pack.cardsPerPack,
                pack.guaranteeLastSlot ? pack.guaranteedMinRarity + " 이상" : "없음",
                string.Join("\n", lines.ToArray())));
        }

        // ================= 필드 슬롯 =================

        /// <summary>
        /// FrontSlot_* / BackSlot_* 자식에 FieldSlot·콜라이더가 있는지 확인하고, frontRow/backRow를
        /// 왼쪽→오른쪽(x좌표) 순서로 다시 맞춘다. 슬롯 스크립트가 빠져서 카드가 안 내지는 문제를 예방한다.
        /// </summary>
        static void RepairFieldSlots(FieldZone field, List<string> log)
        {
            if (field == null) return;
            var front = CollectSlots(field.transform, "FrontSlot_", log); // 전열 슬롯 모으기
            var back = CollectSlots(field.transform, "BackSlot_", log);   // 후열 슬롯 모으기

            // 배열이 실제 슬롯과 다를 때만 다시 연결한다
            if (front.Count > 0 && !SameSlots(field.frontRow, front))
            {
                field.frontRow = front.ToArray();
                log.Add(field.name + " 전열 슬롯 " + front.Count + "칸 다시 연결");
            }
            if (back.Count > 0 && !SameSlots(field.backRow, back))
            {
                field.backRow = back.ToArray();
                log.Add(field.name + " 후열 슬롯 " + back.Count + "칸 다시 연결");
            }
            EditorUtility.SetDirty(field);
        }

        /// <summary>이름이 prefix로 시작하는 자식을 슬롯으로 모은다. 빠진 컴포넌트는 추가하고, x좌표 순으로 정렬한다.</summary>
        static List<FieldSlot> CollectSlots(Transform parent, string prefix, List<string> log)
        {
            var slots = new List<FieldSlot>();
            foreach (Transform child in parent)
            {
                if (!child.name.StartsWith(prefix)) continue; // 슬롯이 아닌 자식(카드 등)은 건너뜀

                var slot = child.GetComponent<FieldSlot>();
                if (slot == null)
                {
                    slot = Undo.AddComponent<FieldSlot>(child.gameObject); // 스크립트가 빠져 있으면 추가
                    log.Add(child.name + "에 FieldSlot 추가");
                }
                var col = child.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = Undo.AddComponent<BoxCollider2D>(child.gameObject); // 드롭 판정용 콜라이더 추가
                    col.size = new Vector2(1.8f, 1.9f);
                    log.Add(child.name + "에 콜라이더 추가");
                }
                col.isTrigger = true; // 물리 충돌 없이 겹침 판정만
                slots.Add(slot);
            }
            slots.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x)); // 왼쪽 → 오른쪽
            return slots;
        }

        /// <summary>현재 배열과 새로 모은 슬롯 목록이 완전히 같은지.</summary>
        static bool SameSlots(FieldSlot[] current, List<FieldSlot> wanted)
        {
            if (current == null || current.Length != wanted.Count) return false;
            for (int i = 0; i < current.Length; i++) if (current[i] != wanted[i]) return false;
            return true;
        }

        // ================= 체력 / 승패 문구 =================

        /// <summary>체력 표시 2개와 승패 문구가 없으면 만들어 CardManager에 연결한다.</summary>
        static void EnsureHealthTexts(CardManager manager, List<string> log)
        {
            if (manager.playerHealthText == null)
            {
                manager.playerHealthText = CreateLabel(manager.manaText, "PlayerHealthDisplay", PlayerHealthPos);
                log.Add("플레이어 체력 표시 생성");
            }
            if (manager.enemyHealthText == null)
            {
                manager.enemyHealthText = CreateLabel(manager.manaText, "EnemyHealthDisplay", EnemyHealthPos);
                log.Add("상대 체력 표시 생성");
            }
            if (manager.gameOverText == null)
            {
                var text = CreateLabel(manager.manaText, "GameOverText", Vector3.zero); // 화면 가운데
                text.characterSize = 0.35f;                 // 크게
                text.anchor = TextAnchor.MiddleCenter;      // 가운데 기준
                text.alignment = TextAlignment.Center;
                text.GetComponent<MeshRenderer>().sortingOrder = GameOverSortingOrder; // 맨 위에 그리기
                text.gameObject.SetActive(false);           // 평소에는 숨김
                manager.gameOverText = text;
                log.Add("승패 문구 생성");
            }
        }

        /// <summary>기존 TextMesh(style)의 글꼴/재질/크기를 그대로 복제해 새 문구 오브젝트를 만든다.</summary>
        static TextMesh CreateLabel(TextMesh style, string name, Vector3 position)
        {
            GameObject go;
            if (style != null)
            {
                go = Object.Instantiate(style.gameObject); // 군력 표시와 같은 모양으로 복제
                go.transform.SetParent(null, false);
            }
            else
            {
                go = new GameObject(name, typeof(MeshRenderer), typeof(TextMesh)); // 원본이 없으면 새로 만듦
            }
            go.name = name;
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name); // Ctrl+Z로 되돌릴 수 있게
            var text = go.GetComponent<TextMesh>();
            text.text = name; // 실제 문구는 게임 시작 시 CardManager가 채운다
            return text;
        }

        // ================= 조선 기본팩 에셋 =================

        /// <summary>조선 기본팩 에셋이 없으면 만들고, 조선 카드 전체로 채운다.</summary>
        static CardPackData EnsureJoseonPack(List<string> log)
        {
            var pack = AssetDatabase.LoadAssetAtPath<CardPackData>(JoseonPackPath);
            if (pack != null) return pack; // 이미 있음

            if (!AssetDatabase.IsValidFolder(PackFolder))
                AssetDatabase.CreateFolder("Assets", "PackData"); // 폴더가 없으면 만든다

            pack = ScriptableObject.CreateInstance<CardPackData>();
            pack.packName = "조선 기본팩";
            pack.autoFillFaction = Faction.Joseon;
            AssetDatabase.CreateAsset(pack, JoseonPackPath);
            pack.AutoFillFromFaction(); // 조선 카드 전체로 카드 풀 채우기
            AssetDatabase.SaveAssets();
            log.Add("조선 기본팩 에셋 생성 (" + JoseonPackPath + ", 카드 " + pack.cardPool.Count + "장)");
            return pack;
        }

        // ================= 카드팩 버튼 / 결과 패널 =================

        /// <summary>
        /// 팩 결과 패널(PackOpener)과 "팩 열기" 버튼이 없으면 만든다.
        /// 모양은 기존 덱 빌더 패널/버튼을 복제해서 스타일을 통일한다.
        /// </summary>
        static void EnsurePackUI(CardManager manager, CardPackData pack, List<string> log)
        {
            if (Object.FindFirstObjectByType<PackOpenerUI>(FindObjectsInactive.Include) != null) return; // 이미 있음

            // 복제할 원본들 찾기
            var deckBuilder = Object.FindFirstObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);
            var deckEditButton = Object.FindFirstObjectByType<DeckEditButton>(FindObjectsInactive.Include);
            Transform deckPanel = deckBuilder != null && deckBuilder.panelRoot != null ? deckBuilder.panelRoot.transform : null;

            // 결과 패널을 관리하는 루트 오브젝트
            var root = new GameObject("PackOpener");
            Undo.RegisterCreatedObjectUndo(root, "Create PackOpener");
            var ui = root.AddComponent<PackOpenerUI>();
            ui.manager = manager;
            ui.pack = pack;

            // 켜고 끌 패널
            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            ui.panelRoot = panel;

            // 배경 (덱 빌더 배경 복제, 없으면 새로 만듦)
            var bgSource = deckPanel != null ? deckPanel.Find("Background") : null;
            if (bgSource != null) CloneUnder(bgSource.gameObject, panel.transform, "Background", Vector3.zero);
            else CreateBackground(panel.transform);

            // 제목 (덱 빌더 제목 복제)
            var titleSource = deckPanel != null ? deckPanel.Find("Title") : null;
            var title = titleSource != null
                ? CloneUnder(titleSource.gameObject, panel.transform, "Title", new Vector3(0f, 4.6f, 0f)).GetComponent<TextMesh>()
                : null;
            ui.titleText = title;

            // 뽑힌 카드가 펼쳐질 위치
            var reveal = new GameObject("Reveal");
            reveal.transform.SetParent(panel.transform, false);
            reveal.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            ui.revealParent = reveal.transform;

            // 닫기 버튼 (덱 빌더의 저장 버튼 모양 복제 → 기능만 닫기로 교체)
            var saveSource = deckPanel != null ? deckPanel.Find("SaveButton") : null;
            if (saveSource != null)
            {
                var close = CloneUnder(saveSource.gameObject, panel.transform, "CloseButton", new Vector3(0f, -3.6f, 0f));
                Object.DestroyImmediate(close.GetComponent<DeckSaveButton>()); // 저장 기능 제거
                var closeButton = close.AddComponent<PackCloseButton>();         // 닫기 기능 추가
                closeButton.background = close.GetComponent<SpriteRenderer>();
                closeButton.ui = ui;
                SetChildLabel(close, "닫기");
                if (closeButton.background != null) closeButton.background.color = GamePalette.NeutralButton.normal;
            }
            else
            {
                log.Add("경고: 덱 빌더의 저장 버튼을 찾지 못해 팩 패널의 닫기 버튼을 만들지 못했습니다.");
            }

            panel.SetActive(false); // 평소에는 숨김

            // 팩 열기 버튼 (덱 편집 버튼 모양 복제 → 기능만 팩 열기로 교체)
            if (deckEditButton != null)
            {
                var open = Object.Instantiate(deckEditButton.gameObject);
                open.name = "PackOpenButton";
                open.transform.position = PackButtonPos;
                Undo.RegisterCreatedObjectUndo(open, "Create PackOpenButton");
                Object.DestroyImmediate(open.GetComponent<DeckEditButton>()); // 덱 편집 기능 제거
                var openButton = open.AddComponent<PackOpenButton>();          // 팩 열기 기능 추가
                openButton.background = open.GetComponent<SpriteRenderer>();
                openButton.ui = ui;
                SetChildLabel(open, "팩 열기");
                if (openButton.background != null) openButton.background.color = GamePalette.GoldButton.normal;
            }
            else
            {
                log.Add("경고: 덱 편집 버튼을 찾지 못해 팩 열기 버튼을 만들지 못했습니다.");
            }

            log.Add("팩 열기 버튼 + 팩 결과 패널 생성");
        }

        // ================= 화면 나누기 (처음 화면 / 배틀 / 덱 편집) =================

        /// <summary>
        /// ScreenManager가 없으면 만들고, 처음 화면(제목 + 배틀 시작/덱 편집/팩 열기 버튼)과
        /// 배틀 화면 오브젝트 목록을 채워 연결한다. 배틀 화면에는 카드(손패·필드), 턴 종료 버튼, 군력/덱/체력 표시만 남긴다.
        /// </summary>
        static void EnsureScreens(CardManager manager, List<string> log)
        {
            if (Object.FindFirstObjectByType<ScreenManager>(FindObjectsInactive.Include) != null) return; // 이미 있음

            var deckBuilder = Object.FindFirstObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);
            var deckEditButton = Object.FindFirstObjectByType<DeckEditButton>(FindObjectsInactive.Include);
            var packOpenButton = Object.FindFirstObjectByType<PackOpenButton>(FindObjectsInactive.Include);
            var endTurnButton = Object.FindFirstObjectByType<EndTurnButton>(FindObjectsInactive.Include);
            var packOpener = Object.FindFirstObjectByType<PackOpenerUI>(FindObjectsInactive.Include);

            // 화면 전환 담당 오브젝트
            var smGo = new GameObject("ScreenManager");
            Undo.RegisterCreatedObjectUndo(smGo, "Create ScreenManager");
            var screens = smGo.AddComponent<ScreenManager>();
            screens.manager = manager;
            screens.deckBuilder = deckBuilder;
            screens.packOpener = packOpener;

            // ---- 처음 화면 ----
            var lobby = new GameObject("Lobby");
            Undo.RegisterCreatedObjectUndo(lobby, "Create Lobby");

            var title = CreateLabel(manager.manaText, "LobbyTitle", LobbyTitlePos); // 군력 표시 글꼴 복제
            title.text = LobbyTitle;
            title.characterSize = 0.3f;
            title.anchor = TextAnchor.MiddleCenter;
            title.alignment = TextAlignment.Center;
            title.transform.SetParent(lobby.transform, true);

            if (deckEditButton != null)
            {
                // "배틀 시작" 버튼: 덱 편집 버튼 모양을 복제해서 기능만 화면 이동으로 교체
                var battleButtonGo = Object.Instantiate(deckEditButton.gameObject);
                battleButtonGo.name = "BattleStartButton";
                Object.DestroyImmediate(battleButtonGo.GetComponent<DeckEditButton>());
                var battleButton = battleButtonGo.AddComponent<ScreenButton>();
                battleButton.background = battleButtonGo.GetComponent<SpriteRenderer>();
                battleButton.screens = screens;
                battleButton.target = GameScreen.Battle;
                SetChildLabel(battleButtonGo, "배틀 시작");
                if (battleButton.background != null) battleButton.background.color = GamePalette.GoldButton.normal;
                battleButtonGo.transform.SetParent(lobby.transform, true);
                battleButtonGo.transform.position = LobbyBattleButtonPos;

                // 기존 "덱 편집" 버튼은 처음 화면으로 옮기고, 누르면 덱 편집 화면으로 가게 연결
                Undo.SetTransformParent(deckEditButton.transform, lobby.transform, "Move DeckEditButton");
                deckEditButton.transform.position = LobbyDeckButtonPos;
                deckEditButton.screens = screens;
                EditorUtility.SetDirty(deckEditButton);
            }
            else
            {
                log.Add("경고: 덱 편집 버튼을 찾지 못해 처음 화면 버튼을 만들지 못했습니다.");
            }

            if (packOpenButton != null)
            {
                // "팩 열기" 버튼도 처음 화면으로 옮긴다 (배틀 중에는 안 보이게)
                Undo.SetTransformParent(packOpenButton.transform, lobby.transform, "Move PackOpenButton");
                packOpenButton.transform.position = LobbyPackButtonPos;
            }

            screens.lobbyObjects = new[] { lobby };

            // ---- 배틀 화면: 카드 + 턴 종료 + 상태 표시만 ----
            var battle = new List<GameObject>();
            AddIfExists(battle, manager.playerHand);
            AddIfExists(battle, manager.enemyHand);
            AddIfExists(battle, manager.playerField);
            AddIfExists(battle, manager.enemyField);
            AddIfExists(battle, endTurnButton);
            AddIfExists(battle, manager.manaText);
            AddIfExists(battle, manager.enemyManaText);
            AddIfExists(battle, manager.playerDeckCountText);
            AddIfExists(battle, manager.enemyDeckCountText);
            AddIfExists(battle, manager.playerHealthText);
            AddIfExists(battle, manager.enemyHealthText);
            AddIfExists(battle, manager.gameOverText);
            var battleLine = GameObject.Find("BattleLine"); // 필드 가운데 선 (있으면)
            if (battleLine != null) battle.Add(battleLine);
            screens.battleObjects = battle.ToArray();

            // 게임이 끝난 뒤 턴 종료 → 처음 화면, 덱 저장 → 처음 화면
            if (endTurnButton != null) { endTurnButton.screens = screens; EditorUtility.SetDirty(endTurnButton); }
            if (deckBuilder != null) { deckBuilder.screens = screens; EditorUtility.SetDirty(deckBuilder); }

            EditorUtility.SetDirty(screens);
            log.Add("화면 나누기: 처음 화면(배틀 시작/덱 편집/팩 열기) + 배틀 화면(오브젝트 " + battle.Count + "개) + 덱 편집 화면");
        }

        /// <summary>컴포넌트가 있으면 그 오브젝트를 목록에 넣는다(중복 제외).</summary>
        static void AddIfExists(List<GameObject> list, Component component)
        {
            if (component == null) return;
            if (!list.Contains(component.gameObject)) list.Add(component.gameObject);
        }

        /// <summary>source를 복제해 parent 아래에 이름/위치를 정해 둔다.</summary>
        static GameObject CloneUnder(GameObject source, Transform parent, string name, Vector3 localPos)
        {
            var clone = Object.Instantiate(source, parent, false);
            clone.name = name;
            clone.transform.localPosition = localPos;
            clone.SetActive(true);
            return clone;
        }

        /// <summary>버튼 자식의 글자(Label)를 바꾼다.</summary>
        static void SetChildLabel(GameObject button, string label)
        {
            var text = button.GetComponentInChildren<TextMesh>(true);
            if (text != null) text.text = label;
        }

        /// <summary>덱 빌더 배경을 찾지 못했을 때 쓰는 기본 반투명 배경을 만든다.</summary>
        static void CreateBackground(Transform parent)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AbilityBoxSpritePath);
            sr.color = new Color(0.03f, 0.03f, 0.05f, 0.92f); // 거의 검정, 약간 투명
            sr.sortingOrder = 30000;                           // 카드보다 위
            go.transform.localScale = new Vector3(11f, 19f, 1f);
        }
    }
}
