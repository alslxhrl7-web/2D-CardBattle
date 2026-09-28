# 산성의 패 대전 서버

온라인 대전·2인 대전(방)용 서버입니다. 자동 매칭(먼저 기다리던 사람과 짝) 또는 방 번호(4자리)로 두 사람을 짝지어 주고,
둘 사이의 메시지를 그대로 전달합니다. 진영은 각자 게임에서 고릅니다. 게임 규칙은 양쪽 게임이 각자 계산하기 때문에 서버에는 게임 코드가 없습니다.

## 1. 내 PC에서 테스트

1. Node.js(LTS)를 설치합니다: https://nodejs.org
2. 이 폴더의 `서버켜기.bat`을 더블클릭합니다. `대전 서버 시작: 포트 8080`이 나오면 켜진 것입니다. (창을 닫으면 서버도 꺼집니다)
3. 게임 두 개를 켜서 둘 다 처음 화면의 **온라인 대전**을 누릅니다(또는 **2인 대전** → 한쪽은 방 만들기, 다른 쪽은 방 참가). 예를 들면:
   - Unity 에디터에서 Play 하나 + WebGL 빌드(Builds/WebGL)를 브라우저로 하나
   - 또는 PC 빌드(File → Build Profiles → Windows) 두 개
   씬의 `OnlineMatch` 오브젝트의 Server Url이 `ws://localhost:8080`이어야 합니다(기본값).
4. 같은 와이파이의 다른 컴퓨터에서 붙으려면 Server Url을 `ws://내PC의IP:8080`으로 바꿉니다
   (명령 프롬프트에서 `ipconfig` → IPv4 주소). 처음 켤 때 Windows 방화벽이 물으면 허용합니다.

## 2. 인터넷에 올리기 (Render 무료)

itch.io 웹 버전은 https 페이지라서 서버도 보안 주소(wss://)여야 합니다. Render는 무료로 wss 주소를 줍니다.

1. 이 프로젝트를 GitHub에 올립니다(`Server` 폴더와 루트의 `render.yaml` 포함).
2. https://render.com 에 GitHub 계정으로 가입 → **New → Blueprint** → 이 저장소를 고르면 `render.yaml`대로 서버가 만들어집니다.
   (Blueprint 대신 **New → Web Service**로 해도 됩니다: Root Directory `Server`, Build `npm install`, Start `node server.js`, Instance Type `Free`)
3. 배포가 끝나면 `https://sanseong-battle-server-xxxx.onrender.com` 같은 주소가 나옵니다. 브라우저로 열어서
   "산성의 패 대전 서버 동작 중"이 보이면 성공입니다.
4. Unity 씬의 `OnlineMatch` → Server Url을 `wss://sanseong-battle-server-xxxx.onrender.com`으로 바꾸고
   (https → wss), 메뉴 **CardBattle → itch.io용 WebGL 빌드**로 다시 빌드해서 올립니다.

무료 서버는 15분 동안 아무도 안 쓰면 잠들고, 다음 접속 때 깨어나는 데 1분쯤 걸립니다
(게임에는 "서버에 연결하는 중…"이 뜹니다).

## 메시지 (참고)

- 게임 → 서버 (접속하자마자 한 번): `find` (자동 매칭), `create` (방 만들기), `join|방번호` (방 참가)
- 서버 → 게임: `wait` (상대를 기다리는 중), `room|방번호`, `noroom` (그런 방 없음), `match|0` 또는 `match|1` (내 자리), `left` (상대가 나감)
- 게임 ↔ 게임 (서버는 전달만): `deck|진영번호|카드,카드,…`, `place|손패번호|레인`, `equip|손패번호|레인`, `spell|손패번호`, `power`, `end`
- 서버 코드를 바꾸면 Render 서버도 다시 올려야 합니다(GitHub에 푸시하면 Render가 자동으로 다시 배포. 자동 배포가 꺼져 있으면 Render 화면에서 Manual Deploy). 게임과 서버가 서로 다른 버전이면 대전이 안 됩니다.
