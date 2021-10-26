
using System;
using System.Collections.Generic;

using NativeWebSocket;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;


namespace Turniphead.Trebek {


    public enum MessageType {
        Error = 0,
        Ping = 1,
        AdminConnected = 10,
        PlayerConnected = 12,
        GameReset = 21,
        ChangeRound = 22,
        PopQuestion = 30,
        ClearQuestion = 31,
        RequireWager = 32,
        RequireAnswer = 33,
        PlayerBuzzed = 40,
        PlayerEnteredWager = 43,
        PlayerEnteredAnswer = 44,
        UpdateScore = 50,
    }


    public class Message {

        public MessageType type;
        public Dictionary<string, object> data;

        public Message(
            MessageType type
        ) {
            this.type = type;
            data = new Dictionary<string, object>();
        }
    }


    public class ConnectionManager : MonoBehaviour {

        [SerializeField] private TextMeshProUGUI uiPlayerNameText;
        [SerializeField] private TextMeshProUGUI uiScoreText;
        [SerializeField] private TextMeshProUGUI uiQuestionText;
        [SerializeField] private GameObject uiBuzzerButton;
        [SerializeField] private GameObject uiSubmitWagerButton;
        [SerializeField] private GameObject uiSubmitAnswerButton;
        [SerializeField] private GameObject uiWagerInput;
        [SerializeField] private TMP_InputField uiWagerInputText;
        [SerializeField] private GameObject uiAnswerInput;
        [SerializeField] private TMP_InputField uiAnswerInputText;

        private string gameKey;
        private int playerId;
        private string playerName;

        private WebSocket socket;
        private bool connecting = false;

        private bool adminConnected = false;
        private int score;
        private int minWager = 0;
        private int maxWager;
        private string questionId;

        private void Start() {
            
        }

        private void Update() {
            #if !UNITY_WEBGL || UNITY_EDITOR
                if (socket != null) {
                    socket.DispatchMessageQueue();
                }
            #endif
            if (socket == null && !connecting && ReadyToConnect()) {
                connecting = true;
                Connect();
            }
        }

        private void OnOpen() {
            Message msg = new Message(MessageType.PlayerConnected);
            SendMessage(msg);
            CheckGameState();
        }

        private void OnClose(
            WebSocketCloseCode closeCode
        ) {
            CheckGameState();
        }

        private void OnMessage(
            byte[] bytes
        ) {
            string data = System.Text.Encoding.UTF8.GetString(bytes);
            Message message = JsonConvert.DeserializeObject<Message>(data);
            switch (message.type) {
                case MessageType.Ping:
                    // Send the same message right on back.
                    SendMessage(message);
                    break;
                case MessageType.AdminConnected:
                    adminConnected = true;
                    CheckGameState();
                    break;
                case MessageType.GameReset:
                case MessageType.ChangeRound:
                    ResetGame();
                    break;
                case MessageType.PopQuestion:
                    questionId = (string) message.data["question_id"];
                    string questionText = (string) message.data["question_text"];
                    ShowQuestion(questionText);
                    CheckGameState();
                    break;
                case MessageType.ClearQuestion:
                    questionId = null;
                    HideQuestion();
                    HideWagerEntry();
                    HideAnswerEntry();
                    CheckGameState();
                    break;
                case MessageType.RequireWager:
                    int newMaxWager = Convert.ToInt32(message.data["max_wager"]);
                    UpdateMaxWager(newMaxWager);
                    ShowWagerEntry();
                    break;
                case MessageType.RequireAnswer:
                    ShowAnswerEntry();
                    break;
                case MessageType.UpdateScore:
                    int newScore = Convert.ToInt32(message.data["score"]);
                    UpdateScore(newScore);
                    break;
                case MessageType.Error:
                    string errorMsg = (string) message.data["error"];
                    Debug.LogWarning($"Error: {errorMsg}");
                    break;
                default:
                    Debug.LogWarning($"Unhandled MessageType: {message.type}");
                    break;
            }
        }

        private void OnError(
            string errorMsg
        ) {

        }

        private async void OnApplicationQuit() {
            await socket.Close();
        }

        public bool ReadyToConnect() {
            return gameKey != null && playerId != 0 && playerName != null;
        }

        public async void Connect() {
            socket = new WebSocket("ws://localhost:8765");
            socket.OnOpen += OnOpen;
            socket.OnMessage += OnMessage;
            socket.OnError += OnError;
            socket.OnClose += OnClose;
            await socket.Connect();
        }

        public bool CheckConnection() {
            if (socket == null || socket.State != WebSocketState.Open) {
                if (socket != null && socket.State == WebSocketState.Connecting) {
                    // Just keep waiting for now.
                    return false;
                }
                Connect();
                return false;
            }
            return true;
        }

        public async void SendMessage(
            Message message
        ) {
            if (socket.State != WebSocketState.Open) {
                return;
            }
            // Changing the message data here instead of building a new message
            //  feels a little icky but :shrug:.
            message.data["game_key"] = gameKey;
            message.data["player_id"] = playerId;
            message.data["player_name"] = playerName;
            string json = JsonConvert.SerializeObject(message);
            await socket.SendText(json);
        }

        public void SetGameKey(
            string gameKey
        ) {
            this.gameKey = gameKey;
        }

        public void SetPlayerId(
            int playerId
        ) {
            this.playerId = playerId;
        }

        public void SetPlayerName(
            string playerName
        ) {
            this.playerName = playerName;
            uiPlayerNameText.text = playerName;
        }

        public void ResetGame() {
            adminConnected = false;
            score = 0;
            minWager = 0;
            maxWager = 0;
            questionId = null;
            uiScoreText.text = "?";
            uiQuestionText.text = "Connecting...";
            Connect();
        }

        public void CheckGameState() {
            if (!CheckConnection()) {
                uiQuestionText.text = "Connecting...";
            }
            else if (!adminConnected) {
                uiQuestionText.text = "Waiting for admin...";
            }
            else if (questionId == null) {
                uiQuestionText.text = "Waiting for question...";
            }
        }

        public void UpdateScore(
            int newScore
        ) {
            score = newScore;
            uiScoreText.text = score.ToString();
        }

        public void UpdateMinWager(
            int newMinWager
        ) {
            minWager = newMinWager;
        }

        public void UpdateMaxWager(
            int newMaxWager
        ) {
            maxWager = newMaxWager;
        }

        public void ShowQuestion(
            string questionText
        ) {
            uiQuestionText.text = questionText;
        }

        public void HideQuestion() {
            questionId = null;
            uiQuestionText.text = "";
        }

        public void SendBuzz() {
            Message msg = new Message(MessageType.PlayerBuzzed);
            msg.data["question_id"] = questionId;
            SendMessage(msg);
        }

        public void ShowWagerEntry() {
            uiBuzzerButton.SetActive(false);
            uiWagerInput.SetActive(true);
            uiSubmitWagerButton.SetActive(true);
        }

        public void HideWagerEntry() {
            uiWagerInput.SetActive(false);
            uiSubmitWagerButton.SetActive(false);
            uiBuzzerButton.SetActive(true);
        }

        public void SendWager(
            int amount
        ) {
            if (amount < minWager || amount > maxWager) {
                return;
            }
            Message msg = new Message(MessageType.PlayerEnteredWager);
            msg.data["amount"] = amount;
            SendMessage(msg);
            HideWagerEntry();
        }

        public void ShowAnswerEntry() {
            uiBuzzerButton.SetActive(false);
            uiAnswerInput.SetActive(true);
            uiSubmitAnswerButton.SetActive(true);
        }

        public void HideAnswerEntry() {
            uiAnswerInput.SetActive(false);
            uiSubmitAnswerButton.SetActive(false);
            uiBuzzerButton.SetActive(true);
        }

        public void SendAnswer(
            string answer
        ) {
            Message msg = new Message(MessageType.PlayerEnteredAnswer);
            msg.data["answer"] = answer;
            SendMessage(msg);
            HideAnswerEntry();
        }

        public void OnButtonBuzz() {
            SendBuzz();
        }

        public void OnButtonSubmitWager() {
            string wagerText = uiWagerInputText.text;
            int wager;
            if (!int.TryParse(wagerText, out wager)) {
                return;
            }
            if (wager < minWager || wager > maxWager) {
                return;
            }
            SendWager(wager);
        }

        public void OnButtonSubmitAnswer() {
            string answer = uiAnswerInputText.text;
            if (answer == "") {
                return;
            }
            SendAnswer(answer);
        }
    }
}
