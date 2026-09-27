using System;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#else
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CardBattle
{
    /// <summary>
    /// 대전 서버와 글자 메시지를 주고받는 연결(웹소켓).
    ///   웹(WebGL) 빌드: 브라우저의 WebSocket을 씀 → Assets/Plugins/WebGL/OnlineSocket.jslib
    ///   에디터·PC 빌드: .NET의 ClientWebSocket을 씀
    /// 받은 메시지는 쌓아 두었다가 Poll()로 하나씩 꺼낸다. (OnlineMatch.Update가 매 프레임 꺼내서 처리)
    /// </summary>
    public class OnlineSocket
    {
        public enum State { Connecting, Open, Closed }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void OnlineSocketConnect(string url);
        [DllImport("__Internal")] static extern void OnlineSocketSend(string message);
        [DllImport("__Internal")] static extern void OnlineSocketClose();
        [DllImport("__Internal")] static extern int OnlineSocketState();
        [DllImport("__Internal")] static extern string OnlineSocketPoll();

        public void Connect(string url) { OnlineSocketConnect(url); }
        public void Send(string message) { OnlineSocketSend(message); }
        public void Close() { OnlineSocketClose(); }
        public string Poll() { return OnlineSocketPoll(); }

        public State CurrentState
        {
            get
            {
                int s = OnlineSocketState();
                return s == 0 ? State.Connecting : s == 1 ? State.Open : State.Closed;
            }
        }
#else
        readonly ClientWebSocket ws = new ClientWebSocket();
        readonly ConcurrentQueue<string> received = new ConcurrentQueue<string>(); // 받은 메시지 (받는 쪽은 다른 스레드일 수 있음)
        Task sending = Task.CompletedTask; // 보내기는 한 번에 하나씩만 할 수 있어서 줄을 세운다
        volatile State state = State.Connecting;

        public State CurrentState { get { return state; } }

        /// <summary>접속을 시작한다. 결과는 CurrentState로 확인.</summary>
        public void Connect(string url)
        {
            ReceiveLoop(url);
        }

        /// <summary>접속하고, 끊길 때까지 메시지를 받아서 쌓는다.</summary>
        async void ReceiveLoop(string url)
        {
            try
            {
                await ws.ConnectAsync(new Uri(url), CancellationToken.None);
                state = State.Open;
                var buffer = new byte[4096];
                var message = new MemoryStream(); // 한 메시지가 여러 조각으로 올 수 있어서 모은다
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    message.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage) continue;
                    received.Enqueue(Encoding.UTF8.GetString(message.ToArray()));
                    message.SetLength(0);
                }
            }
            catch (Exception)
            {
                // 접속 실패·끊김 → 아래에서 Closed로 알린다
            }
            state = State.Closed;
        }

        /// <summary>메시지를 보낸다 (접속돼 있지 않으면 무시).</summary>
        public void Send(string message)
        {
            if (state != State.Open) return;
            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            sending = sending.ContinueWith(_ => ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None)).Unwrap();
        }

        /// <summary>연결을 끊는다. (서버는 상대에게 "left"를 보낸다)</summary>
        public void Close()
        {
            state = State.Closed;
            ws.Abort();
        }

        /// <summary>받은 메시지를 하나 꺼낸다. 없으면 null.</summary>
        public string Poll()
        {
            string message;
            return received.TryDequeue(out message) ? message : null;
        }
#endif
    }
}
