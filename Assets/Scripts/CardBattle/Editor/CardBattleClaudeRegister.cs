using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace CardBattle.EditorTools
{
    /// <summary>
    /// Claude 데스크톱 앱 설정 파일에 Unity MCP("unity-mcp")를 등록한다.
    ///
    /// 왜 필요한가: 스토어(패키지) 방식으로 설치된 Claude 앱은 설정 파일을
    ///   %LOCALAPPDATA%\Packages\Claude_xxx\LocalCache\Roaming\Claude\claude_desktop_config.json 에서 읽는데,
    ///   Unity MCP 설정 화면의 Configure 버튼은 %APPDATA%\Claude\... 에만 써서 Claude 앱이 못 본다.
    ///   그래서 이 도구가 Claude 앱이 실제로 읽는 파일에 직접 추가한다.
    ///
    /// 안전장치: 기존 설정은 그대로 두고 unity-mcp 항목만 추가, 바꾸기 전 원본을 .bak 파일로 백업,
    ///   결과가 올바른 JSON이 아니면 원본으로 되돌린다. 결과는 Logs/CardBattleMcpStatus.log 에 기록.
    /// 스크립트가 처음 컴파일될 때 한 번 자동 실행되고, 메뉴 "CardBattle/Claude 앱에 Unity MCP 등록"으로 다시 실행할 수 있다.
    /// </summary>
    [InitializeOnLoad]
    public static class CardBattleClaudeRegister
    {
        const string ServerName = "unity-mcp";                  // Claude 설정에 들어갈 서버 이름
        const string RunVersion = "claude-register-1";           // 이 값이 바뀌면 다음 컴파일 때 한 번 더 실행
        const string PrefKey = "CardBattle.ClaudeRegister";      // 이미 실행했는지 기록하는 키
        const string StatusLog = "Logs/CardBattleMcpStatus.log"; // 결과 기록 파일

        /// <summary>에디터가 켜지거나 다시 컴파일될 때: 아직 실행 안 했으면 한 번 실행.</summary>
        static CardBattleClaudeRegister()
        {
            if (EditorPrefs.GetString(PrefKey, "") == RunVersion) return;
            EditorApplication.delayCall += RunOnce;
        }

        /// <summary>플레이/컴파일 중이 아닐 때 한 번 실행.</summary>
        static void RunOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunOnce;
                return;
            }
            EditorPrefs.SetString(PrefKey, RunVersion);
            Register();
        }

        /// <summary>메뉴: Claude 앱 설정에 Unity MCP 등록.</summary>
        [MenuItem("CardBattle/Claude 앱에 Unity MCP 등록", false, 82)]
        public static void Register()
        {
            var log = new List<string>();
            log.Add("시각: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (Claude 앱에 Unity MCP 등록)");
            try
            {
                string relay = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".unity", "relay", "relay_win.exe");
                if (!File.Exists(relay))
                {
                    log.Add("실패: relay 프로그램이 없음 (" + relay + ") — Unity를 다시 켜면 설치됨");
                    return;
                }

                var targets = FindClaudeConfigPaths();
                if (targets.Count == 0)
                {
                    log.Add("실패: Claude 앱 설정 폴더를 찾지 못함");
                    return;
                }
                foreach (var path in targets) RegisterInto(path, relay, log);
            }
            catch (Exception e)
            {
                log.Add("오류: " + e.Message);
            }
            finally
            {
                WriteLog(log);
                UnityEngine.Debug.Log("[CardBattle] " + string.Join("\n", log.ToArray()));
            }
        }

        /// <summary>
        /// Claude 앱이 읽는 설정 파일 경로들. 스토어 설치(Packages\*Claude*) 폴더가 있으면 그쪽,
        /// 일반 설치 폴더(%APPDATA%\Claude)가 있으면 그쪽도 포함한다.
        /// </summary>
        static List<string> FindClaudeConfigPaths()
        {
            var list = new List<string>();
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string packages = Path.Combine(local, "Packages");
            if (Directory.Exists(packages))
            {
                foreach (var dir in Directory.GetDirectories(packages, "Claude_*"))
                {
                    string claudeDir = Path.Combine(dir, "LocalCache", "Roaming", "Claude");
                    if (Directory.Exists(claudeDir)) list.Add(Path.Combine(claudeDir, "claude_desktop_config.json"));
                }
            }
            string roamingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Claude");
            if (Directory.Exists(roamingDir)) list.Add(Path.Combine(roamingDir, "claude_desktop_config.json"));
            return list;
        }

        /// <summary>설정 파일 하나에 unity-mcp 항목을 추가한다(이미 있으면 그대로 둔다).</summary>
        static void RegisterInto(string path, string relay, List<string> log)
        {
            string original = File.Exists(path) ? File.ReadAllText(path) : "";
            if (original.Contains("\"" + ServerName + "\""))
            {
                log.Add("이미 등록됨: " + path);
                return;
            }

            // 넣을 항목: "unity-mcp": { "command": "C:\\...\\relay_win.exe", "args": ["--mcp"] }
            string entry = "\"" + ServerName + "\": {\n      \"command\": \"" + relay.Replace("\\", "\\\\") + "\",\n      \"args\": [\"--mcp\"]\n    }";

            string updated;
            if (original.Trim().Length == 0)
            {
                updated = "{\n  \"mcpServers\": {\n    " + entry + "\n  }\n}\n"; // 파일이 비었거나 없음 → 새로 작성
            }
            else
            {
                var servers = Regex.Match(original, "\"mcpServers\"\\s*:\\s*\\{");
                if (servers.Success)
                {
                    // 기존 mcpServers { 바로 뒤에 추가. 안에 다른 서버가 있으면 뒤에 쉼표.
                    int at = servers.Index + servers.Length;
                    bool empty = original.Substring(at).TrimStart().StartsWith("}");
                    updated = original.Substring(0, at) + "\n    " + entry + (empty ? "\n  " : ",") + original.Substring(at);
                }
                else
                {
                    // mcpServers가 없음 → 맨 바깥 { 바로 뒤에 mcpServers 통째로 추가
                    int at = original.IndexOf('{') + 1;
                    if (at <= 0) { log.Add("실패: 설정 파일 형식이 예상과 다름 (" + path + ")"); return; }
                    bool empty = original.Substring(at).TrimStart().StartsWith("}");
                    updated = original.Substring(0, at) + "\n  \"mcpServers\": {\n    " + entry + "\n  }" + (empty ? "\n" : ",") + original.Substring(at);
                }
            }

            if (!JsonChecker.IsValid(updated))
            {
                log.Add("실패: 수정 결과가 올바른 JSON이 아니라서 파일을 건드리지 않음 (" + path + ")");
                return;
            }

            if (File.Exists(path))
            {
                string backup = path + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(path, backup);
                log.Add("백업: " + backup);
            }
            File.WriteAllText(path, updated); // UTF-8(BOM 없음)
            log.Add("등록 완료: " + path);
            log.Add("다음 단계: Claude 앱을 트레이 아이콘에서 완전히 종료 후 다시 켜기 → Unity MCP 설정 화면 Pending Connections에서 Accept");
        }

        /// <summary>결과를 Logs/CardBattleMcpStatus.log 에 덧붙여 쓴다.</summary>
        static void WriteLog(List<string> lines)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StatusLog));
                File.AppendAllText(StatusLog, string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine + "----" + Environment.NewLine);
            }
            catch (Exception) { /* 기록 실패는 무시 */ }
        }

        /// <summary>
        /// 아주 작은 JSON 문법 검사기. 설정 파일을 망가뜨리지 않도록 저장 전에 한 번 확인하는 용도.
        /// (값을 읽지는 않고 문법이 맞는지만 본다)
        /// </summary>
        public static class JsonChecker
        {
            /// <summary>text 전체가 올바른 JSON 값 하나이면 true.</summary>
            public static bool IsValid(string text)
            {
                int i = 0;
                if (!Value(text, ref i)) return false;
                Skip(text, ref i);
                return i == text.Length;
            }

            // 공백 건너뛰기
            static void Skip(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

            // 값 하나: 객체/배열/문자열/숫자/true/false/null
            static bool Value(string s, ref int i)
            {
                Skip(s, ref i);
                if (i >= s.Length) return false;
                char c = s[i];
                if (c == '{') return Obj(s, ref i);
                if (c == '[') return Arr(s, ref i);
                if (c == '"') return Str(s, ref i);
                if (c == '-' || char.IsDigit(c)) return Num(s, ref i);
                return Word(s, ref i, "true") || Word(s, ref i, "false") || Word(s, ref i, "null");
            }

            // { "키": 값, ... }
            static bool Obj(string s, ref int i)
            {
                i++; Skip(s, ref i);
                if (i < s.Length && s[i] == '}') { i++; return true; }
                while (true)
                {
                    Skip(s, ref i);
                    if (i >= s.Length || s[i] != '"' || !Str(s, ref i)) return false;
                    Skip(s, ref i);
                    if (i >= s.Length || s[i] != ':') return false;
                    i++;
                    if (!Value(s, ref i)) return false;
                    Skip(s, ref i);
                    if (i >= s.Length) return false;
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return true; }
                    return false;
                }
            }

            // [ 값, ... ]
            static bool Arr(string s, ref int i)
            {
                i++; Skip(s, ref i);
                if (i < s.Length && s[i] == ']') { i++; return true; }
                while (true)
                {
                    if (!Value(s, ref i)) return false;
                    Skip(s, ref i);
                    if (i >= s.Length) return false;
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return true; }
                    return false;
                }
            }

            // "문자열" (역슬래시 이스케이프 포함)
            static bool Str(string s, ref int i)
            {
                i++;
                while (i < s.Length)
                {
                    char c = s[i];
                    if (c == '"') { i++; return true; }
                    if (c == '\\')
                    {
                        i++;
                        if (i >= s.Length) return false;
                        if ("\"\\/bfnrt".IndexOf(s[i]) >= 0) { i++; continue; }
                        if (s[i] == 'u' && i + 4 < s.Length) { i += 5; continue; }
                        return false;
                    }
                    if (c < ' ') return false; // 줄바꿈 등 제어문자는 문자열 안에 그대로 올 수 없음
                    i++;
                }
                return false;
            }

            // 숫자 (정수/소수/지수)
            static bool Num(string s, ref int i)
            {
                int start = i;
                if (s[i] == '-') i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+' || s[i] == '-')) i++;
                return i > start && char.IsDigit(s[i - 1]);
            }

            // true / false / null
            static bool Word(string s, ref int i, string w)
            {
                if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
                i += w.Length;
                return true;
            }
        }
    }
}
