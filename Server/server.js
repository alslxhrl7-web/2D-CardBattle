// 산성의 패 온라인 대전 서버
// 하는 일은 두 가지뿐이다:
//   1) 매칭: 먼저 들어온 사람이 기다리고 있으면 새로 들어온 사람과 짝을 지어 준다 (먼저 온 사람 = 조선, 나중 = 청)
//   2) 중계: 짝이 된 두 사람 사이에서 메시지를 그대로 전달한다 (게임 규칙은 양쪽 게임이 각자 계산)
//
// 서버가 보내는 메시지:  wait (상대를 기다리는 중) / match|0 또는 match|1 (내 자리) / left (상대가 나감)
// 게임끼리 주고받는 메시지(서버는 내용을 보지 않음): deck|… place|… equip|… spell|… end
//
// 실행: npm install → node server.js   (포트는 환경변수 PORT, 없으면 8080)

const http = require('http');
const { WebSocketServer } = require('ws');

const port = process.env.PORT || 8080;

// Render 같은 호스팅은 주소로 접속해 보고 살아 있는지 확인하므로, 일반 접속에는 짧은 글자를 돌려준다
const server = http.createServer((req, res) => {
  res.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8' });
  res.end('산성의 패 대전 서버 동작 중\n');
});

const wss = new WebSocketServer({ server });
let waiting = null; // 짝을 기다리는 사람 (한 명만)

wss.on('connection', (ws) => {
  ws.partner = null;
  ws.alive = true;
  ws.on('pong', () => { ws.alive = true; });

  if (waiting && waiting.readyState === ws.OPEN) {
    pair(waiting, ws);
    waiting = null;
  } else {
    waiting = ws;
    ws.send('wait');
  }

  // 받은 메시지는 짝에게 그대로 넘긴다
  ws.on('message', (data) => {
    if (ws.partner && ws.partner.readyState === ws.OPEN) ws.partner.send(data.toString());
  });

  ws.on('close', () => {
    if (waiting === ws) waiting = null;
    if (ws.partner) {
      if (ws.partner.readyState === ws.OPEN) ws.partner.send('left');
      ws.partner.partner = null;
    }
  });
});

function pair(first, second) {
  first.partner = second;
  second.partner = first;
  first.send('match|0');  // 조선, 홀수 턴에 먼저 둔다
  second.send('match|1'); // 청
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
