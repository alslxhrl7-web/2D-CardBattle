using UnityEngine;
using UnityEngine.InputSystem;

namespace CardBattle
{
    /// <summary>
    /// 2인 대전 "방 참가"에서 방 번호(숫자 4자리)를 키보드로 받는다. 숫자를 다 넣으면 onDone(번호)을 부른다.
    /// 숫자키·숫자패드로 입력, Backspace로 지우기. 입력한 번호는 display 글자(고르기 화면 제목) 아래에 보인다.
    /// </summary>
    public class RoomCodeInput : MonoBehaviour
    {
        public const int Length = 4; // 방 번호 자릿수 (서버 Server/server.js와 같아야 함)
        static readonly Key[] Digits = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        static readonly Key[] Numpad = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        public TextMesh display;     // 안내 문구 + 입력한 번호를 보여줄 글자
        string prompt;               // 안내 문구 (입력을 시작할 때의 display 글자)
        string code = "";            // 지금까지 넣은 번호
        System.Action<string> onDone; // 다 넣었을 때 할 일

        /// <summary>입력을 시작한다 (빈 번호부터).</summary>
        public void Begin(System.Action<string> done)
        {
            onDone = done;
            prompt = display.text;
            code = "";
            enabled = true;
            Refresh();
        }

        /// <summary>매 프레임 눌린 숫자·지우기 키를 받는다.</summary>
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            for (int i = 0; i < 10; i++)
                if (keyboard[Digits[i]].wasPressedThisFrame || keyboard[Numpad[i]].wasPressedThisFrame) AddDigit(i);
            if (keyboard.backspaceKey.wasPressedThisFrame) Backspace();
        }

        /// <summary>숫자 하나를 넣는다. 자릿수를 다 채우면 입력을 끝내고 onDone.</summary>
        public void AddDigit(int digit)
        {
            if (!enabled) return;
            code += digit;
            Refresh();
            if (code.Length < Length) return;
            enabled = false;
            onDone(code);
        }

        /// <summary>마지막 숫자를 지운다.</summary>
        public void Backspace()
        {
            if (code.Length > 0) code = code.Substring(0, code.Length - 1);
            Refresh();
        }

        /// <summary>안내 문구 아래에 "12__"처럼 보여준다.</summary>
        void Refresh()
        {
            display.text = prompt + "\n\n" + code.PadRight(Length, '_');
        }
    }
}
