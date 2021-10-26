
using System.Runtime.InteropServices;

using UnityEngine;


namespace Turniphead.Trebek {

    public class JSBridge : MonoBehaviour {

        [DllImport("__Internal")]
        public static extern void RequestPlayerDetails();

        ConnectionManager connectionManager;

        private void Start() {

        }

        private void Update() {

        }

        public void UpdateGameKey(
            string gameKey
        ) {
            connectionManager.SetGameKey(gameKey);
        }

        public void UpdatePlayerId(
            int playerId
        ) {
            connectionManager.SetPlayerId(playerId);
        }

        public void UpdatePlayerName(
            string playerName
        ) {
            connectionManager.SetPlayerName(playerName);
        }
    }
}
