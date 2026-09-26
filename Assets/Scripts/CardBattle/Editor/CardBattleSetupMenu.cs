using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace CardBattle.EditorTools
{
    /// <summary>
    /// 에디터 상단 메뉴 "CardBattle"에 들어가는 도구 모음. (Editor 폴더 안이라 게임 빌드에는 포함되지 않는다)
    ///
    /// CardBattle/씬 자동 구성:
    ///   씬을 열어둔 상태에서 한 번 누르면, 코드에는 있지만 씬에 아직 없는 것들을 만들어 연결한다.
    ///   이미 있는 것은 건드리지 않으므로 여러 번 눌러도 안전하다.
    ///     - 필드 슬롯 점검/복구 (FieldSlot 컴포넌트, 콜라이더, 왼쪽→오른쪽 순서)
    ///     - 체력 표시 2개 + 승패 문구
    ///     - 조선 기본팩 에셋 (Assets/PackData/JoseonBasicPack.asset)
    ///     - "팩 열기" 버튼 + 팩 결과 패널
    ///
    /// CardBattle/카드팩 확률 확인:
    ///   프로젝트 창에서 CardPackData 에셋을 선택하고 누르면, 희귀도별 확률과 1000팩 시뮬레이션 결과를 콘솔에 보여준다.
    /// </summary>
    public static class CardBattleSetupMenu
    {
        const string PackFolder = "Assets/PackData";
        const string JoseonPackPath = PackFolder + "/JoseonBasicPack.asset";
        const string AbilityBoxSpritePath = "Assets/UI/CardFrame/AbilityBox.png";

        // 화면 배치 (카메라 세로 ±6.5 기준). 왼쪽 열 = 상태 표시, 오른쪽 열 = 버튼
        static readonly Vector3 PlayerHealthPos = new Vector3(-7.2f, -3.7f, 0f);
        static readonly Vector3 EnemyHealthPos = new Vector3(-7.2f, 3.7f, 0f);
        static readonly Vector3 PackButtonPos = new Vector3(7.0f, -3.7f, 0f);
        const int GameOverSortingOrder = 33000;   // 모든 카드/패널보다 위

        [MenuItem("CardBattle/씬 자동 구성")]
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

            var log = new List<string>();
            RepairFieldSlots(manager.playerField, log);
            RepairFieldSlots(manager.enemyField, log);
            EnsureHealthTexts(manager, log);
            var pack = EnsureJoseonPack(log);
            EnsurePackUI(manager, pack, log);

            EditorUtility.SetDirty(manager);
            var scene = manager.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (log.Count == 0) log.Add("이미 모두 구성되어 있어서 바꾼 것이 없습니다.");
            Debug.Log("[CardBattle] 씬 자동 구성 완료\n- " + string.Join("\n- ", log.ToArray()));
        }

        [MenuItem("CardBattle/카드팩 확률 확인")]
        public static void ReportPackOdds()
        {
            var pack = Selection.activeObject as CardPackData;
            if (pack == null) pack = AssetDatabase.LoadAssetAtPath<CardPackData>(JoseonPackPath);
            if (pack == null)
            {
                EditorUtility.DisplayDialog("CardBattle", "프로젝트 창에서 CardPackData 에셋을 선택한 뒤 다시 실행해 주세요.", "확인");
                return;
            }

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

        // ------------------------------------------------------------------
        // 필드 슬롯
        // ------------------------------------------------------------------

        /// <summary>
        /// FrontSlot_* / BackSlot_* 자식에 FieldSlot·콜라이더가 있는지 확인하고, frontRow/backRow를
        /// 왼쪽→오른쪽(x좌표) 순서로 다시 맞춘다. 슬롯 스크립트가 빠져서 카드가 안 내지는 문제를 예방한다.
        /// </summary>
        static void RepairFieldSlots(FieldZone field, List<string> log)
        {
            if (field == null) return;
            var front = CollectSlots(field.transform, "FrontSlot_", log);
            var back = CollectSlots(field.transform, "BackSlot_", log);

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

        static List<FieldSlot> CollectSlots(Transform parent, string prefix, List<string> log)
        {
            var slots = new List<FieldSlot>();
            foreach (Transform child in parent)
            {
                if (!child.name.StartsWith(prefix)) continue;

                var slot = child.GetComponent<FieldSlot>();
                if (slot == null)
                {
                    slot = Undo.AddComponent<FieldSlot>(child.gameObject);
                    log.Add(child.name + "에 FieldSlot 추가");
                }
                var col = child.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = Undo.AddComponent<BoxCollider2D>(child.gameObject);
                    col.size = new Vector2(1.8f, 1.9f);
                    log.Add(child.name + "에 콜라이더 추가");
                }
                col.isTrigger = true;
                slots.Add(slot);
            }
            slots.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
            return slots;
        }

        static bool SameSlots(FieldSlot[] current, List<FieldSlot> wanted)
        {
            if (current == null || current.Length != wanted.Count) return false;
            for (int i = 0; i < current.Length; i++) if (current[i] != wanted[i]) return false;
            return true;
        }

        // ------------------------------------------------------------------
        // 체력 / 승패 문구
        // ------------------------------------------------------------------

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
                var text = CreateLabel(manager.manaText, "GameOverText", Vector3.zero);
                text.characterSize = 0.35f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.GetComponent<MeshRenderer>().sortingOrder = GameOverSortingOrder;
                text.gameObject.SetActive(false);
                manager.gameOverText = text;
                log.Add("승패 문구 생성");
            }
        }

        /// <summary>기존 TextMesh(style)의 폰트/재질/크기를 그대로 복제해 새 문구 오브젝트를 만든다.</summary>
        static TextMesh CreateLabel(TextMesh style, string name, Vector3 position)
        {
            GameObject go;
            if (style != null)
            {
                go = Object.Instantiate(style.gameObject);
                go.transform.SetParent(null, false);
            }
            else
            {
                go = new GameObject(name, typeof(MeshRenderer), typeof(TextMesh));
            }
            go.name = name;
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            var text = go.GetComponent<TextMesh>();
            text.text = name;
            return text;
        }

        // ------------------------------------------------------------------
        // 조선 기본팩 에셋
        // ------------------------------------------------------------------

        static CardPackData EnsureJoseonPack(List<string> log)
        {
            var pack = AssetDatabase.LoadAssetAtPath<CardPackData>(JoseonPackPath);
            if (pack != null) return pack;

            if (!AssetDatabase.IsValidFolder(PackFolder))
                AssetDatabase.CreateFolder("Assets", "PackData");

            pack = ScriptableObject.CreateInstance<CardPackData>();
            pack.packName = "조선 기본팩";
            pack.autoFillFaction = Faction.Joseon;
            AssetDatabase.CreateAsset(pack, JoseonPackPath);
            pack.AutoFillFromFaction();
            AssetDatabase.SaveAssets();
            log.Add("조선 기본팩 에셋 생성 (" + JoseonPackPath + ", 카드 " + pack.cardPool.Count + "장)");
            return pack;
        }

        // ------------------------------------------------------------------
        // 카드팩 버튼 / 결과 패널
        // ------------------------------------------------------------------

        static void EnsurePackUI(CardManager manager, CardPackData pack, List<string> log)
        {
            if (Object.FindFirstObjectByType<PackOpenerUI>(FindObjectsInactive.Include) != null) return;

            // 기존 덱 빌더/덱 편집 버튼의 모양을 복제해서 스타일을 통일한다.
            var deckBuilder = Object.FindFirstObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);
            var deckEditButton = Object.FindFirstObjectByType<DeckEditButton>(FindObjectsInactive.Include);
            Transform deckPanel = deckBuilder != null && deckBuilder.panelRoot != null ? deckBuilder.panelRoot.transform : null;

            var root = new GameObject("PackOpener");
            Undo.RegisterCreatedObjectUndo(root, "Create PackOpener");
            var ui = root.AddComponent<PackOpenerUI>();
            ui.manager = manager;
            ui.pack = pack;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            ui.panelRoot = panel;

            // 배경
            var bgSource = deckPanel != null ? deckPanel.Find("Background") : null;
            if (bgSource != null) CloneUnder(bgSource.gameObject, panel.transform, "Background", Vector3.zero);
            else CreateBackground(panel.transform);

            // 제목
            var titleSource = deckPanel != null ? deckPanel.Find("Title") : null;
            var title = titleSource != null
                ? CloneUnder(titleSource.gameObject, panel.transform, "Title", new Vector3(0f, 4.6f, 0f)).GetComponent<TextMesh>()
                : null;
            ui.titleText = title;

            // 카드가 펼쳐질 위치
            var reveal = new GameObject("Reveal");
            reveal.transform.SetParent(panel.transform, false);
            reveal.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            ui.revealParent = reveal.transform;

            // 닫기 버튼 (덱 빌더의 저장 버튼 모양 복제)
            var saveSource = deckPanel != null ? deckPanel.Find("SaveButton") : null;
            if (saveSource != null)
            {
                var close = CloneUnder(saveSource.gameObject, panel.transform, "CloseButton", new Vector3(0f, -3.6f, 0f));
                Object.DestroyImmediate(close.GetComponent<DeckSaveButton>());
                var closeButton = close.AddComponent<PackCloseButton>();
                closeButton.background = close.GetComponent<SpriteRenderer>();
                closeButton.ui = ui;
                SetChildLabel(close, "닫기");
                if (closeButton.background != null) closeButton.background.color = GamePalette.NeutralButton.normal;
            }
            else
            {
                log.Add("경고: 덱 빌더의 저장 버튼을 찾지 못해 팩 패널의 닫기 버튼을 만들지 못했습니다.");
            }

            panel.SetActive(false);

            // 팩 열기 버튼 (덱 편집 버튼 모양 복제)
            if (deckEditButton != null)
            {
                var open = Object.Instantiate(deckEditButton.gameObject);
                open.name = "PackOpenButton";
                open.transform.position = PackButtonPos;
                Undo.RegisterCreatedObjectUndo(open, "Create PackOpenButton");
                Object.DestroyImmediate(open.GetComponent<DeckEditButton>());
                var openButton = open.AddComponent<PackOpenButton>();
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

        static GameObject CloneUnder(GameObject source, Transform parent, string name, Vector3 localPos)
        {
            var clone = Object.Instantiate(source, parent, false);
            clone.name = name;
            clone.transform.localPosition = localPos;
            clone.SetActive(true);
            return clone;
        }

        static void SetChildLabel(GameObject button, string label)
        {
            var text = button.GetComponentInChildren<TextMesh>(true);
            if (text != null) text.text = label;
        }

        static void CreateBackground(Transform parent)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AbilityBoxSpritePath);
            sr.color = new Color(0.03f, 0.03f, 0.05f, 0.92f);
            sr.sortingOrder = 30000;
            go.transform.localScale = new Vector3(11f, 19f, 1f);
        }
    }
}
