// 산성의 패 온라인 대전 서버
// 하는 일은 두 가지뿐이다:
//   1) 짝 짓기: 접속한 게임이 처음 보내는 말에 따라
//        find        → 자동 매칭 (먼저 기다리던 사람과 바로 짝)
//        create      → 방 만들기 (숫자 4자리 방 번호를 만들어 알려 주고 기다림)
//        join|번호   → 그 번호의 방에 들어가 방장과 짝
//      먼저 기다린 사람(방장)이 자리 0, 나중 사람이 자리 1 (자리 0이 홀수 턴에 먼저 둔다)
//   2) 중계: 짝이 된 두 사람 사이에서 메시지를 그대로 전달한다 (게임 규칙은 양쪽 게임이 각자 계산)
//
// 서버가 보내는 메시지:  wait / room|방번호 / noroom (그런 방 없음) / match|0 또는 match|1 (내 자리) / left (상대가 나감)
// 게임끼리 주고받는 메시지(서버는 내용을 보지 않음): deck|… place|… equip|… spell|… power end
//
// 실행: npm install → node server.js   (포트는 환경변수 PORT, 없으면 8080)

const http = require('http');
const { WebSocketServer } = require('ws');

const port = process.env.PORT || 8080;
const ROOM_DIGITS = 4; // 방 번호 자릿수 (게임의 RoomCodeInput.Length와 같아야 함)

// Render 같은 호스팅은 주소로 접속해 보고 살아 있는지 확인하므로, 일반 접속에는 짧은 글자를 돌려준다
const server = http.createServer((req, res) => {
  res.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8' });
  res.end('산성의 패 대전 서버 동작 중\n');
});

const wss = new WebSocketServer({ server });
let waiting = null;       // 자동 매칭으로 짝을 기다리는 사람 (한 명만)
const rooms = new Map();  // 방 번호 → 방장

wss.on('connection', (ws) => {
  ws.partner = null;
  ws.started = false; // 첫 말(find/create/join)을 받았는지
  ws.alive = true;
  ws.on('pong', () => { ws.alive = true; });

  ws.on('message', (data) => {
    const text = data.toString();
    if (!ws.started) { ws.started = true; start(ws, text); return; }
    if (ws.partner && ws.partner.readyState === ws.OPEN) ws.partner.send(text); // 짝에게 그대로 넘긴다
  });

  ws.on('close', () => {
    if (waiting === ws) waiting = null;
    if (ws.room && rooms.get(ws.room) === ws) rooms.delete(ws.room);
    if (ws.partner) {
      if (ws.partner.readyState === ws.OPEN) ws.partner.send('left');
      ws.partner.partner = null;
    }
  });
});

// 첫 말에 따라 짝을 짓는다
function start(ws, command) {
  if (command === 'create') {
    const code = newRoomCode();
    rooms.set(code, ws);
    ws.room = code;
    ws.send('room|' + code);
  } else if (command.startsWith('join|')) {
    const host = rooms.get(command.slice(5));
    if (!host || host.readyState !== ws.OPEN) { ws.send('noroom'); return; }
    rooms.delete(host.room);
    host.room = null;
    pair(host, ws);
  } else if (waiting && waiting.readyState === ws.OPEN) { // find
    pair(waiting, ws);
    waiting = null;
  } else {
    waiting = ws;
    ws.send('wait');
  }
}

// 지금 쓰고 있지 않은 방 번호 (0000 ~ 9999)
function newRoomCode() {
  let code;
  do { code = String(Math.floor(Math.random() * Math.pow(10, ROOM_DIGITS))).padStart(ROOM_DIGITS, '0'); } while (rooms.has(code));
  return code;
}

function pair(first, second) {
  first.partner = second;
  second.partner = first;
  first.send('match|0');  // 먼저 기다린 사람, 홀수 턴에 먼저 둔다
  second.send('match|1');
  console.log('매칭 성공, 지금 접속자 ' + wss.clients.size + '명');
}

// 30초마다 연결 확인: 응답 없는 연결은 끊는다 (창을 그냥 닫은 경우 등)
// 주고받는 신호가 있어서 호스팅 쪽에서 "놀고 있는 연결"로 보고 끊는 것도 막아 준다
setInterval(() => {
  for (const ws of wss.clients) {
    if (!ws.alive) { ws.terminate(); continue; }
    ws.alive = false;
    ws.ping();
  }
}, 30000);

server.listen(port, () => console.log('대전 서버 시작: 포트 ' + port));
