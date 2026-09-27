// 웹(WebGL) 빌드에서 OnlineSocket.cs가 쓰는 브라우저 WebSocket.
// 받은 메시지는 queue에 쌓아 두고, C#이 매 프레임 OnlineSocketPoll로 하나씩 꺼내 간다.
mergeInto(LibraryManager.library, {

  // 접속 시작 (이전 연결이 있으면 먼저 닫는다)
  OnlineSocketConnect: function (url) {
    if (window.cardBattleSocket) window.cardBattleSocket.ws.close();
    var socket = { ws: new WebSocket(UTF8ToString(url)), queue: [], closed: false };
    socket.ws.onmessage = function (e) { socket.queue.push(e.data); };
    socket.ws.onclose = function () { socket.closed = true; };
    socket.ws.onerror = function () { socket.closed = true; };
    window.cardBattleSocket = socket;
  },

  // 메시지 보내기 (열려 있을 때만)
  OnlineSocketSend: function (message) {
    var socket = window.cardBattleSocket;
    if (socket && socket.ws.readyState === 1) socket.ws.send(UTF8ToString(message));
  },

  // 연결 끊기
  OnlineSocketClose: function () {
    var socket = window.cardBattleSocket;
    if (socket) { socket.closed = true; socket.ws.close(); }
    window.cardBattleSocket = null;
  },

  // 0 = 접속 중, 1 = 연결됨, 2 = 끊김
  OnlineSocketState: function () {
    var socket = window.cardBattleSocket;
    if (!socket || socket.closed) return 2;
    return socket.ws.readyState === 1 ? 1 : (socket.ws.readyState === 0 ? 0 : 2);
  },

  // 받은 메시지 하나를 꺼내 C# 문자열로 돌려준다 (없으면 null). 메모리는 Unity가 알아서 해제한다
  OnlineSocketPoll: function () {
    var socket = window.cardBattleSocket;
    if (!socket || socket.queue.length === 0) return null;
    var text = socket.queue.shift();
    var size = lengthBytesUTF8(text) + 1;
    var buffer = _malloc(size);
    stringToUTF8(text, buffer, size);
    return buffer;
  }
});
