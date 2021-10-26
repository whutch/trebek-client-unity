
using System.Collections.Generic;

using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;


namespace Turniphead.Trebek {


    public enum MessageType {
        Error = 0,
        Ping = 1,
        PlayerConnected = 12,
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

        private string gameKey;
        private int playerId;
        private string playerName;

        private WebSocket socket;
        private bool connecting = false;

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
                case MessageType.Error:
                    string errorMsg = (string) message.data["error"];
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
        }

        public void ResetGame() {
            Connect();
        }

        public void CheckGameState() {
            CheckConnection();
        }
    }
}
