using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardBattle.EditorTools
{
    /// <summary>
    /// itch.io에 올릴 웹(WebGL) 빌드를 만드는 도구.
    ///
    /// 메뉴 "CardBattle/itch.io용 WebGL 빌드" 한 번으로:
    ///   1) 한글 글꼴 적용 — Unity 기본 글꼴은 웹에서 한글이 안 나오므로 Assets/Fonts/NanumGothic 으로 모든 글자를 바꾼다
    ///   2) 웹 설정 — 화면 1280×720, 압축 Gzip + 압축 해제 대체(itch.io에서 바로 돌아가게), 백그라운드 실행
    ///   3) 빌드 — Builds/WebGL 폴더에 빌드
    ///   4) 압축 — Builds/CardBattle_itch.zip 생성 (index.html이 zip 맨 위에 있어야 itch.io가 인식함)
    /// 진행 상황과 결과는 Logs/CardBattleWebBuild.log 에 기록된다.
    ///
    /// itch.io 업로드: 새 프로젝트 → Kind of project "HTML" → zip 업로드 → "This file will be played in the browser" 체크
    ///                → Embed options 크기 1280 × 720 (또는 "Click to launch in fullscreen")
    /// </summary>
    public static class CardBattleWebBuild
    {
        const string FontPath = "Assets/Fonts/NanumGothic-Regular.ttf"; // 한글이 들어있는 글꼴 (SIL OFL 라이선스)
        const string BuildFolder = "Builds/WebGL";                     // 빌드 결과 폴더 (프로젝트 폴더 기준)
        const string ZipPath = "Builds/CardBattle_itch.zip";            // itch.io에 올릴 zip
        const string LogPath = "Logs/CardBattleWebBuild.log";           // 결과 기록
        const string ProductName = "산성의 패: 병자호란";                // 브라우저 탭 제목 등에 쓰이는 게임 이름
        const int WebWidth = 1280;   // 웹 화면 가로 (itch.io Embed 크기와 맞추면 됨)
        const int WebHeight = 720;   // 웹 화면 세로

        // ================= 메뉴 =================

        /// <summary>메뉴: 글꼴 적용 → 웹 설정 → 빌드 → zip. (빌드는 몇 분 걸린다)</summary>
        [MenuItem("CardBattle/itch.io용 WebGL 빌드", false, 90)]
        public static void BuildForItch()
        {
            var log = new List<string>();
            log.Add("시작: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            try
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                {
                    log.Add("실패: WebGL 모듈이 설치돼 있지 않음 (Unity Hub → 설치된 에디터 → 모듈 추가 → WebGL Build Support)");
                    return;
                }
                ApplyKoreanFont(log);
                UseDeployedServer(log);
                ConfigureWebGL(log);
                if (!Build(log)) return;
                MakeZip(log);
            }
            catch (Exception e)
            {
                log.Add("오류: " + e);
            }
            finally
            {
                log.Add("끝: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                WriteLog(log);
                Debug.Log("[CardBattle] itch.io 빌드\n" + string.Join("\n", log.ToArray()));
            }
        }

        /// <summary>메뉴: 한글 글꼴만 적용 (빌드 없이).</summary>
        [MenuItem("CardBattle/한글 글꼴 적용 (웹 빌드용)", false, 91)]
        public static void ApplyKoreanFontFromMenu()
        {
            var log = new List<string>();
            ApplyKoreanFont(log);
            Debug.Log("[CardBattle] " + string.Join("\n", log.ToArray()));
        }

        // ================= 서버 주소 =================

        /// <summary>
        /// 씬의 온라인 대전 서버 주소가 내 PC(localhost)로 되어 있으면 배포한 서버 주소로 바꾸고 씬을 저장한다.
        /// 웹 빌드는 다른 사람 컴퓨터에서 돌아가므로 localhost로는 접속할 수 없다.
        /// </summary>
        static void UseDeployedServer(List<string> log)
        {
            var online = UnityEngine.Object.FindAnyObjectByType<OnlineMatch>(FindObjectsInactive.Include);
            if (online == null) { log.Add("서버 주소: OnlineMatch 없음 (온라인 대전 안 됨)"); return; }
            if (online.serverUrl.Contains("localhost") || online.serverUrl.Contains("127.0.0.1"))
            {
                online.serverUrl = OnlineMatch.DeployedServerUrl;
                EditorUtility.SetDirty(online);
                EditorSceneManager.SaveScene(online.gameObject.scene);
            }
            log.Add("서버 주소: " + online.serverUrl);
        }

        // ================= 1) 한글 글꼴 =================

        /// <summary>
        /// 씬과 프리팹의 모든 TextMesh 글꼴을 한글 글꼴로 바꾼다. (TextMesh는 글꼴을 바꾸면 재질도 그 글꼴 것으로 바꿔야 보인다)
        /// 실행 중에 만들어지는 글자(체력 표시, 피해 숫자 등)는 씬의 군력 글자를 복제하므로 자동으로 따라간다.
        /// </summary>
        public static void ApplyKoreanFont(List<string> log)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) { log.Add("글꼴 없음: " + FontPath); return; }

            // 씬
            int sceneCount = 0;
            foreach (var tm in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))
                if (SetFont(tm, font)) sceneCount++;
            if (sceneCount > 0)
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            // 프리팹 (카드 프리팹 등)
            int prefabCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var tm in root.GetComponentsInChildren<TextMesh>(true))
                    if (SetFont(tm, font)) { changed = true; prefabCount++; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            log.Add("한글 글꼴 적용: 씬 글자 " + sceneCount + "개, 프리팹 글자 " + prefabCount + "개");
        }

        /// <summary>글자 하나의 글꼴과 재질을 바꾼다. 이미 같은 글꼴이면 false.</summary>
        static bool SetFont(TextMesh tm, Font font)
        {
            if (tm == null || tm.font == font) return false;
            tm.font = font;
            var mr = tm.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = font.material; // 글꼴 텍스처가 들어있는 재질
            EditorUtility.SetDirty(tm);
            return true;
        }

        // ================= 2) 웹 설정 =================

        /// <summary>itch.io에서 바로 실행되도록 WebGL 설정을 맞춘다.</summary>
        static void ConfigureWebGL(List<string> log)
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultWebScreenWidth = WebWidth;
            PlayerSettings.defaultWebScreenHeight = WebHeight;
            PlayerSettings.runInBackground = true;
            // itch.io는 압축 파일에 맞는 서버 헤더를 붙여주지 않으므로 "압축 해제 대체"를 켜야 로딩이 된다
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            log.Add("웹 설정: " + WebWidth + "x" + WebHeight + ", Gzip + 압축 해제 대체, 이름 '" + ProductName + "'");
        }

        // ================= 3) 빌드 =================

        /// <summary>WebGL로 빌드한다. 성공하면 true.</summary>
        static bool Build(List<string> log)
        {
            var scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            if (scenes.Count == 0) scenes.Add("Assets/Scenes/SampleScene.unity");

            if (Directory.Exists(BuildFolder)) Directory.Delete(BuildFolder, true); // 예전 빌드 지우기
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = BuildFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            log.Add("빌드 결과: " + summary.result + " (" + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + "MB, " + summary.totalTime.TotalSeconds.ToString("0") + "초)");
            if (summary.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                    foreach (var msg in step.messages)
                        if (msg.type == LogType.Error || msg.type == LogType.Exception) log.Add("  빌드 오류: " + msg.content);
                return false;
            }
            return true;
        }

        // ================= 4) zip =================

        /// <summary>
        /// 빌드 폴더를 zip으로 묶는다(index.html이 zip 맨 위). 압축 기능은 참조 문제를 피하려고 실행 중에 불러서 쓴다.
        /// 실패하면 Builds/WebGL 폴더 안의 파일을 직접 zip으로 묶으면 된다.
        /// </summary>
        static void MakeZip(List<string> log)
        {
            if (File.Exists(ZipPath)) File.Delete(ZipPath);
            try
            {
                var asm = System.Reflection.Assembly.Load("System.IO.Compression.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                var zipFile = asm.GetType("System.IO.Compression.ZipFile");
                var method = zipFile.GetMethod("CreateFromDirectory", new[] { typeof(string), typeof(string) });
                method.Invoke(null, new object[] { Path.GetFullPath(BuildFolder), Path.GetFullPath(ZipPath) });
                log.Add("zip 생성: " + ZipPath + " (" + (new FileInfo(ZipPath).Length / (1024f * 1024f)).ToString("0.0") + "MB)");
            }
            catch (Exception e)
            {
                log.Add("zip 생성 실패(" + e.GetType().Name + ") → " + BuildFolder + " 폴더 안의 파일을 직접 zip으로 묶어 주세요");
            }
        }

        /// <summary>결과를 Logs/CardBattleWebBuild.log 에 덧붙여 쓴다.</summary>
        static void WriteLog(List<string> lines)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.AppendAllText(LogPath, string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine + "----" + Environment.NewLine);
            }
            catch (Exception) { /* 기록 실패는 무시 */ }
        }
    }
}
