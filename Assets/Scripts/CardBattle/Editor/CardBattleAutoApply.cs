using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardBattle.EditorTools
{
    /// <summary>
    /// 스크립트가 새로 컴파일되면 자동으로 한 번 실행되어, 메뉴를 직접 누르지 않아도 최신 구성을 씬에 적용한다.
    ///   1) 배경 그림이 Sprite로 임포트됐는지 확인(아니면 Sprite로 바꿔 다시 임포트)
    ///   2) SampleScene을 열고 "씬 자동 구성"(CardBattleSetupMenu.SetupScene)과
    ///      "새 카드·덱·덱 더미 적용"(CardBattleContentSetup.Apply)을 실행 → 씬 저장
    ///   3) 결과를 프로젝트 폴더의 Logs/CardBattleAutoApply.log 에 기록 (Claude가 이 파일로 적용 여부를 확인함)
    /// 같은 ApplyVersion으로는 한 번만 실행된다. 다시 적용하고 싶으면 ApplyVersion 값을 바꾸거나
    /// 메뉴 "CardBattle/자동 적용 다시 실행"을 누른다.
    /// </summary>
    [InitializeOnLoad]
    public static class CardBattleAutoApply
    {
        const string ApplyVersion = "v8-rules-1";                                    // 적용 버전 (바뀌면 다시 한 번 실행됨)
        const string PrefKey = "CardBattle.AutoApplyVersion";                        // 마지막으로 적용한 버전을 저장하는 키
        const string ScenePath = "Assets/Scenes/SampleScene.unity";                  // 게임 씬
        const string BackgroundPath = "Assets/Resources/Backgrounds/BattleBackground.png"; // 배경 그림
        const int BackgroundMaxSize = 2048;                                          // 배경 그림 최대 해상도 (흐려지지 않게)
        const string LogPath = "Logs/CardBattleAutoApply.log";                       // 결과 기록 파일 (프로젝트 폴더 기준)

        /// <summary>에디터가 켜지거나 스크립트가 다시 컴파일될 때 호출된다. 에디터가 준비된 뒤에 실행하도록 미룬다.</summary>
        static CardBattleAutoApply()
        {
            if (EditorPrefs.GetString(PrefKey, "") == ApplyVersion) return; // 이미 적용함
            EditorApplication.delayCall += TryApply;
        }

        /// <summary>메뉴에서 강제로 다시 실행.</summary>
        [MenuItem("CardBattle/자동 적용 다시 실행", false, 60)]
        static void ApplyAgain()
        {
            EditorPrefs.DeleteKey(PrefKey);
            TryApply();
        }

        /// <summary>실행 가능한 상태가 될 때까지 기다렸다가 적용한다.</summary>
        static void TryApply()
        {
            // 플레이 중이거나 컴파일/임포트 중이면 잠시 뒤 다시 시도
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryApply;
                return;
            }

            var report = new List<string>();
            report.Add("시각: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.Add("적용 버전: " + ApplyVersion);
            report.Add("Unity: " + Application.unityVersion);
            report.Add("컴파일: 성공 (이 기록이 써졌다는 것 자체가 모든 스크립트가 컴파일됐다는 뜻)");

            // 적용하는 동안 콘솔에 나온 경고/오류도 같이 기록
            var consoleLines = new List<string>();
            Application.LogCallback capture = (msg, stack, type) =>
            {
                if (type != LogType.Log) consoleLines.Add("[" + type + "] " + msg);
            };
            Application.logMessageReceived += capture;

            bool done = false;
            try
            {
                FixBackgroundImport(report);
                done = ApplyScene(report);
            }
            catch (Exception e)
            {
                report.Add("오류: " + e);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }

            if (consoleLines.Count > 0)
            {
                report.Add("콘솔 경고/오류:");
                report.AddRange(consoleLines);
            }

            if (done) EditorPrefs.SetString(PrefKey, ApplyVersion); // 성공했을 때만 "적용함"으로 기록
            report.Add(done ? "결과: 적용 완료" : "결과: 적용 못 함 (다음 컴파일 때 다시 시도)");
            WriteLog(report);
            Debug.Log("[CardBattle] 자동 적용\n" + string.Join("\n", report.ToArray()));
        }

        /// <summary>배경 그림이 Sprite 형식이 아니면 Sprite로 바꿔 다시 임포트한다.</summary>
        static void FixBackgroundImport(List<string> report)
        {
            var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer == null)
            {
                report.Add("배경: " + BackgroundPath + " 을 찾지 못함");
                return;
            }
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                || importer.maxTextureSize < BackgroundMaxSize)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = BackgroundMaxSize; // 원본(1376px)이 줄어들지 않도록
                importer.SaveAndReimport();
                report.Add("배경: Sprite / 최대 " + BackgroundMaxSize + "px 로 다시 임포트함");
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            report.Add(sprite != null
                ? "배경: Sprite 확인 (" + sprite.rect.width + "x" + sprite.rect.height + ")"
                : "배경: Sprite를 불러오지 못함");
        }

        /// <summary>게임 씬을 열고 씬 자동 구성을 실행한다. 성공하면 true.</summary>
        static bool ApplyScene(List<string> report)
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                // 다른 씬에 저장 안 한 작업이 있으면 건드리지 않는다 (사용자 작업 보호)
                if (active.isDirty)
                {
                    report.Add("씬: 다른 씬(" + active.path + ")에 저장 안 한 변경이 있어서 건너뜀");
                    return false;
                }
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                {
                    report.Add("씬: " + ScenePath + " 을 찾지 못함");
                    return false;
                }
                EditorSceneManager.OpenScene(ScenePath);
                report.Add("씬: " + ScenePath + " 열기");
            }

            if (UnityEngine.Object.FindFirstObjectByType<CardManager>(FindObjectsInactive.Include) == null)
            {
                report.Add("씬: CardManager가 없음");
                return false;
            }

            CardBattleSetupMenu.SetupScene(); // 슬롯 복구, 체력/승패 문구, 조선팩, 팩 UI, 화면 나누기 + 씬 저장

            // 새 카드(장비·전술·진) 14장, 시작 덱, 덱 편집 타일, 덱 더미
            var contentLog = new List<string>();
            CardBattleContentSetup.Apply(contentLog);
            EditorSceneManager.SaveOpenScenes();
            foreach (var line in contentLog) report.Add("  " + line);
            report.Add("씬: 자동 구성 실행 후 저장");
            DescribeScene(report);
            return true;
        }

        /// <summary>적용 결과 확인용으로 주요 오브젝트가 씬에 있는지 기록한다.</summary>
        static void DescribeScene(List<string> report)
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<CardManager>(FindObjectsInactive.Include);
            report.Add("  CardManager: " + (manager != null ? manager.name : "없음"));
            if (manager != null)
            {
                report.Add("  내 체력 표시: " + (manager.playerHealthText != null ? manager.playerHealthText.name : "없음"));
                report.Add("  적 체력 표시: " + (manager.enemyHealthText != null ? manager.enemyHealthText.name : "없음"));
                report.Add("  승패 문구: " + (manager.gameOverText != null ? manager.gameOverText.name : "없음"));
            }
            var screens = UnityEngine.Object.FindFirstObjectByType<ScreenManager>(FindObjectsInactive.Include);
            report.Add("  ScreenManager: " + (screens != null ? screens.name : "없음"));
            var pack = UnityEngine.Object.FindFirstObjectByType<PackOpenerUI>(FindObjectsInactive.Include);
            report.Add("  PackOpenerUI: " + (pack != null ? pack.name : "없음"));
            report.Add("  카메라: " + (Camera.main != null ? Camera.main.name : "MainCamera 태그 카메라 없음"));
        }

        /// <summary>결과를 Logs/CardBattleAutoApply.log 에 덧붙여 쓴다.</summary>
        static void WriteLog(List<string> report)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.AppendAllText(LogPath, string.Join(Environment.NewLine, report.ToArray()) + Environment.NewLine + "----" + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CardBattle] 자동 적용 기록 실패: " + e.Message);
            }
        }
    }
}
