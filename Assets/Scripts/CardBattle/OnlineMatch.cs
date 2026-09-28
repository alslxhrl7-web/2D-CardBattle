using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 온라인 대전 담당. 서버에 접속 → 자동 매칭 → 덱 교환 → 서로의 행동(카드 내기, 턴 종료)을 주고받는다.
    /// 게임 규칙은 두 컴퓨터가 똑같이 계산하고, 서버(Server/server.js)는 메시지를 전달만 한다.
    ///
    /// 화면: 각자 아래쪽이 나(Player), 위쪽이 상대(Enemy).
    /// 자리: 먼저 기다리던 사람 = 자리 0 = 조선 덱(playerDeckData), 나중에 온 사람 = 자리 1 = 청 덱(enemyDeckData).
    ///
    /// 메시지는 글자 한 줄이고 | 로 나눈다.
    ///   서버 → 나   : wait (기다리는 중) / match|자리 / left (상대가 나감)
    ///   나 ↔ 상대  : deck|카드,카드,…(뽑는 순서) / place|손패번호|레인 / equip|손패번호|레인 / spell|손패번호 / power(영웅 능력) / end
    /// 상대가 보낸 행동은 상대 쪽(위쪽) 손패·필드에 똑같이 적용한다.
    /// </summary>
    public class OnlineMatch : MonoBehaviour
    {
        /// <summary>Render에 올린 대전 서버 주소. 웹 빌드는 이 주소로 접속한다 (localhost는 웹에서 쓸 수 없음).</summary>
        public const string DeployedServerUrl = "wss://twod-cardbattle-2026-09-23-16-33-40.onrender.com";

        [Tooltip("대전 서버 주소. 내 PC에서 테스트: ws://localhost:8080 / 배포한 서버: " + DeployedServerUrl)]
        public string serverUrl = DeployedServerUrl;
        public CardManager manager; // 게임을 진행하는 매니저
        [Tooltip("상대 덱을 카드 이름으로 받아서 찾을 때 쓰는 전체 카드 목록 (메뉴 '새 카드·덱·덱 더미 적용'이 채운다)")]
        public List<CardData> cardLibrary = new List<CardData>();

        OnlineSocket socket;                                            // 서버 연결 (없으면 null)
        readonly Queue<string[]> opponentMoves = new Queue<string[]>(); // 받은 상대 행동 (전투 연출이 끝나면 차례로 적용)

        /// <summary>내 자리 (0 = 조선, 1 = 청).</summary>
        public int LocalSeat { get; private set; }
        /// <summary>내 드로우 더미 (내가 섞어서 상대에게도 보낸 순서).</summary>
        public List<CardData> MyPile { get; private set; }
        /// <summary>상대 드로우 더미 (상대가 보내 준 순서).</summary>
        public List<CardData> OpponentPile { get; private set; }
        /// <summary>덱까지 주고받아서 게임이 진행 중인지.</summary>
        public bool InGame { get; private set; }
        /// <summary>안내판에 띄울 접속 상태 (접속 중·기다리는 중·실패). 게임 중이면 null.</summary>
        public string Status { get; private set; }

        /// <summary>서버에 접속해서 상대를 찾는다. 이전 연결이 있으면 끊고 새로 시작.</summary>
        public void FindMatch()
        {
            Leave();
            socket = new OnlineSocket();
            socket.Connect(serverUrl);
            SetStatus(GameTexts.OnlineConnecting);
        }

        /// <summary>연결을 끊고 처음 상태로.</summary>
        public void Leave()
        {
            if (socket != null) socket.Close();
            socket = null;
            InGame = false;
            MyPile = null;
            OpponentPile = null;
            opponentMoves.Clear();
            Status = null;
        }

        /// <summary>게임이 꺼질 때 연결도 끊는다.</summary>
        void OnDestroy()
        {
            Leave();
        }

        /// <summary>매 프레임: 받은 메시지를 처리하고, 연결이 끊겼는지 보고, 쌓인 상대 행동을 적용한다.</summary>
        void Update()
        {
            string message;
            while (socket != null && (message = socket.Poll()) != null) Receive(message);
            if (socket != null && socket.CurrentState == OnlineSocket.State.Closed) ConnectionLost();
            ApplyOpponentMoves();
        }

        // ================= 보내기 (CardManager가 내 행동을 알린다) =================

        public void SendPlace(int handIndex, int lane) { socket.Send("place|" + handIndex + "|" + lane); }
        public void SendEquip(int handIndex, int lane) { socket.Send("equip|" + handIndex + "|" + lane); }
        public void SendSpell(int handIndex) { socket.Send("spell|" + handIndex); }
        public void SendEndTurn() { socket.Send("end"); }
        public void SendHeroPower() { socket.Send("power"); }

        // ================= 받기 =================

        /// <summary>메시지 하나를 처리한다.</summary>
        void Receive(string message)
        {
            string[] parts = message.Split('|');
            switch (parts[0])
            {
                case "wait":
                    SetStatus(GameTexts.OnlineWaiting);
                    break;
                case "match":
                    LocalSeat = int.Parse(parts[1]);
                    MyPile = CardManager.BuildDeck(LocalSeat == 0 ? manager.playerDeckData : manager.enemyDeckData, true);
                    socket.Send("deck|" + string.Join(",", MyPile.ConvertAll(card => card.name).ToArray()));
                    TryStart();
                    break;
                case "deck":
                    OpponentPile = ParsePile(parts[1]);
                    TryStart();
                    break;
                case "left":
                    Leave();
                    manager.EndByDisconnect(GameTexts.OnlineOpponentLeft);
                    break;
                default:
                    opponentMoves.Enqueue(parts); // place / equip / spell / power / end
                    break;
            }
        }

        /// <summary>내 덱과 상대 덱이 둘 다 준비되면 게임 시작.</summary>
        void TryStart()
        {
            if (InGame || MyPile == null || OpponentPile == null) return;
            InGame = true;
            Status = null;
            manager.BeginOnlineGame();
        }

        /// <summary>연결이 끊겼을 때: 게임 중이었으면 게임을 끝내고, 아니면 "연결 실패"를 띄운다.</summary>
        void ConnectionLost()
        {
            bool wasInGame = InGame;
            Leave();
            if (wasInGame) manager.EndByDisconnect(GameTexts.OnlineConnectionLost);
            else SetStatus(GameTexts.OnlineFailed);
        }

        /// <summary>
        /// 쌓인 상대 행동을 차례로 적용한다. 전투 연출 중이면 끝날 때까지 기다린다
        /// (내 화면의 연출이 상대보다 늦게 끝날 수 있어서).
        /// 규칙상 안 되는 행동이 오면 두 게임이 이미 어긋난 것이라, 계속하지 않고 판을 끝낸다.
        /// </summary>
        void ApplyOpponentMoves()
        {
            while (InGame && opponentMoves.Count > 0 && !manager.IsBusy && !manager.IsGameOver)
            {
                string[] move = opponentMoves.Dequeue();
                if (ApplyOpponentMove(move)) continue;
                Debug.LogWarning("[온라인] 상대 행동을 적용하지 못했습니다 (두 게임이 어긋남): " + string.Join("|", move));
                Leave(); // 연결을 끊으면 상대 화면에는 "상대가 나갔습니다"가 뜬다
                manager.EndByDisconnect(GameTexts.OnlineDesync);
                return;
            }
        }

        /// <summary>상대 행동 하나를 위쪽(상대) 손패·필드에 적용한다. 규칙상 안 되는 행동이면 false.</summary>
        bool ApplyOpponentMove(string[] move)
        {
            if (move[0] == "end") return manager.EndOpponentTurn();
            if (move[0] == "power") return manager.UseHeroPower(Side.Enemy);

            var hand = manager.enemyHand;
            int handIndex = int.Parse(move[1]);
            if (handIndex < 0 || handIndex >= hand.cards.Count) return false;
            var card = hand.cards[handIndex].GetComponent<CardView>();

            switch (move[0])
            {
                case "place": return manager.TryPlaceCard(card, hand, manager.enemyField.SlotAt(true, int.Parse(move[2])));
                case "equip": return manager.TryEquip(card, hand, manager.enemyField.SlotAt(false, int.Parse(move[2])));
                case "spell": return manager.TryCastSpell(card, hand);
                default: return false;
            }
        }

        /// <summary>"이름,이름,…"을 카드 목록으로. 전체 카드 목록과 두 시작 덱에서 이름으로 찾는다.</summary>
        List<CardData> ParsePile(string names)
        {
            var byName = new Dictionary<string, CardData>();
            foreach (var card in cardLibrary) if (card != null) byName[card.name] = card;
            foreach (var deck in new[] { manager.playerDeckData, manager.enemyDeckData })
                if (deck != null)
                    foreach (var card in deck.BuildCardList()) byName[card.name] = card;

            var pile = new List<CardData>();
            foreach (var name in names.Split(','))
            {
                CardData card;
                if (byName.TryGetValue(name, out card)) pile.Add(card);
                else Debug.LogWarning("[온라인] 모르는 카드: " + name + " (cardLibrary에 없음)");
            }
            return pile;
        }

        /// <summary>접속 상태 문구를 바꾸고 안내판을 갱신한다.</summary>
        void SetStatus(string status)
        {
            Status = status;
            manager.RefreshInfoPanel();
        }
    }
}
