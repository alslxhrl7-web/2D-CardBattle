using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace CardBattle.EditorTools
{
    /// <summary>
    /// Unity MCP(외부 AI 연결) 설정 화면을 여는 도구.
    /// Claude 연결이 끝났다고 표시되기 전까지는 Unity를 켤 때마다(에디터 세션마다 한 번) 설정 화면을 자동으로 연다.
    /// 연결 상태 점검 결과는 Logs/CardBattleMcpStatus.log 에 기록한다(Claude가 이 파일로 상태를 확인함).
    ///
    /// 메뉴:
    ///   CardBattle/Unity MCP 설정 열기        — 지금 바로 설정 화면 열기
    ///   CardBattle/Unity MCP 연결 완료로 표시  — 더 이상 자동으로 열지 않기 (UserSettings/CardBattleMcpConnected.txt 생성)
    /// 설정 화면 위치: Edit → Project Settings → AI → Unity MCP Server
    /// </summary>
    [InitializeOnLoad]
    public static class CardBattleOpenMcpSettings
    {
        const string SettingsPath = "Project/AI/Unity MCP Server";              // Unity AI Assistant 패키지의 MCP 설정 경로
        const string ConnectedMarker = "UserSettings/CardBattleMcpConnected.txt"; // 이 파일이 있으면 "연결 완료"로 보고 자동으로 열지 않음
        const string StatusLog = "Logs/CardBattleMcpStatus.log";                  // 연결 점검 결과 기록 파일
        const string SessionKey = "CardBattle.McpSettingsOpenedThisSession";      // 이번 에디터 세션에서 이미 열었는지

        /// <summary>에디터가 켜지거나 다시 컴파일될 때 호출된다.</summary>
        static CardBattleOpenMcpSettings()
        {
            if (File.Exists(ConnectedMarker)) return;              // 연결 완료로 표시됨 → 아무것도 안 함
            if (SessionState.GetBool(SessionKey, false)) return;   // 이번 세션에 이미 열었음 (컴파일할 때마다 뜨지 않게)
            EditorApplication.delayCall += OpenWhenReady;
        }

        /// <summary>플레이/컴파일 중이 아닐 때 상태를 기록하고 설정 화면을 연다.</summary>
        static void OpenWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OpenWhenReady; // 준비될 때까지 기다림
                return;
            }
            SessionState.SetBool(SessionKey, true);
            WriteStatus();
            Open();
        }

        /// <summary>메뉴: Unity MCP 설정 화면 열기.</summary>
        [MenuItem("CardBattle/Unity MCP 설정 열기", false, 80)]
        public static void Open()
        {
            var window = SettingsService.OpenProjectSettings(SettingsPath);
            if (window != null) window.Focus(); // 창을 앞으로 가져옴
        }

        /// <summary>메뉴: 연결이 끝났으니 더 이상 자동으로 열지 않게 표시한다.</summary>
        [MenuItem("CardBattle/Unity MCP 연결 완료로 표시", false, 81)]
        public static void MarkConnected()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConnectedMarker));
            File.WriteAllText(ConnectedMarker, "연결 완료 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// Claude 데스크톱 설정 파일에 Unity MCP가 등록됐는지, 연결 프로그램(relay)이 있는지 점검해서 기록한다.
        /// (Claude 데스크톱은 설치 방식에 따라 설정 파일 위치가 두 군데 중 하나)
        /// </summary>
        static void WriteStatus()
        {
            var lines = new List<string>();
            lines.Add("시각: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 1) 연결 프로그램(relay)
            string relay = Path.Combine(home, ".unity", "relay", "relay_win.exe");
            lines.Add("relay 프로그램: " + (File.Exists(relay) ? "있음 (" + relay + ")" : "없음 (" + relay + ")"));

            // 2) Claude 데스크톱 설정 파일 후보들
            var candidates = new List<string> { Path.Combine(roaming, "Claude", "claude_desktop_config.json") };
            string packages = Path.Combine(local, "Packages");
            try
            {
                if (Directory.Exists(packages))
                    foreach (var dir in Directory.GetDirectories(packages, "*Claude*"))
                        candidates.Add(Path.Combine(dir, "LocalCache", "Roaming", "Claude", "claude_desktop_config.json"));
            }
            catch (Exception e) { lines.Add("Packages 폴더 확인 실패: " + e.Message); }

            bool registered = false;
            foreach (var path in candidates)
            {
                if (!File.Exists(path)) { lines.Add("Claude 설정 파일 없음: " + path); continue; }
                string text = "";
                try { text = File.ReadAllText(path); } catch (Exception e) { lines.Add("읽기 실패: " + path + " " + e.Message); }
                bool hasUnity = text.IndexOf("relay", StringComparison.OrdinalIgnoreCase) >= 0
                             || text.IndexOf("unity", StringComparison.OrdinalIgnoreCase) >= 0;
                registered |= hasUnity;
                lines.Add("Claude 설정 파일: " + path + " → Unity 등록 " + (hasUnity ? "됨" : "안 됨"));
            }
            lines.Add("요약: " + (registered
                ? "Claude 설정에 Unity가 등록됨 → Claude 앱 재시작 후 Unity 설정 화면의 Pending Connections에서 Accept 필요"
                : "Claude 설정에 Unity가 아직 없음 → 설정 화면 Integrations에서 Claude Desktop의 Configure를 누르세요"));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StatusLog));
                File.AppendAllText(StatusLog, string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine + "----" + Environment.NewLine);
            }
            catch (Exception) { /* 기록 실패는 무시 (설정 화면 여는 게 우선) */ }
        }
    }
}
