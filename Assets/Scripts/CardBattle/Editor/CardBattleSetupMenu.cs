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
    ///   게임 씬(SampleScene)을 열고 Play를 누른다. 이것만 누르면 게임이 시작된다.
    ///
    /// CardBattle/씬 자동 구성:
    ///   필드 슬롯을 점검·복구한다 (FieldSlot 컴포넌트, 콜라이더, 왼쪽→오른쪽 순서). 여러 번 눌러도 안전하다.
    ///   (체력 표시·승패 배너는 게임이 시작될 때 CardManager가 없으면 만든다)
    ///
    /// CardBattle/카드팩 확률 확인:
    ///   프로젝트 창에서 CardPackData 에셋을 선택하고 누르면, 희귀도별 확률과 1000팩 시뮬레이션 결과를 콘솔에 보여준다.
    /// </summary>
    public static class CardBattleSetupMenu
    {
        const string GameScenePath = "Assets/Scenes/SampleScene.unity";          // 게임이 들어있는 씬
        const string JoseonPackPath = "Assets/PackData/JoseonBasicPack.asset";   // 조선 기본팩 에셋 경로

        // ================= 메뉴: 게임 시작 =================

        /// <summary>게임 씬을 열고 Play 모드로 들어간다. 저장 안 한 씬이 열려 있으면 먼저 저장할지 물어본다.</summary>
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

            // 3) Play 시작 → 처음 화면이 뜨고, "배틀 시작"을 누르면 CardManager.ResetGame()으로 새 판이 시작된다
            EditorApplication.EnterPlaymode();
        }

        // ================= 메뉴: 씬 자동 구성 =================

        /// <summary>필드 슬롯을 점검·복구하고 씬을 저장한다.</summary>
        [MenuItem("CardBattle/씬 자동 구성", false, 20)]
        public static void SetupScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardBattle", "플레이 모드를 끈 뒤에 실행해 주세요.", "확인");
                return;
            }

            var manager = Object.FindAnyObjectByType<CardManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                EditorUtility.DisplayDialog("CardBattle", "씬에서 CardManager를 찾지 못했습니다. SampleScene을 연 뒤 다시 실행해 주세요.", "확인");
                return;
            }

            var log = new List<string>(); // 이번에 바꾼 내용 기록 (마지막에 콘솔에 출력)
            RepairFieldSlots(manager.playerField, log);
            RepairFieldSlots(manager.enemyField, log);

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
    }
}
