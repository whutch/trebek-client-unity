
gameKey = "TEST";
playerId = 0;
playerName = "Unity";

function handlePlayerDetailsRequest() {
    unityInstance.SendMessage("JS Bridge", "UpdateGameKey", gameKey);
    unityInstance.SendMessage("JS Bridge", "UpdatePlayerId", playerId);
    unityInstance.SendMessage("JS Bridge", "UpdatePlayerName", playerName);
}
