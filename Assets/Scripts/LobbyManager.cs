using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour {

    private Canvas lobbyCanvas;
    private TMP_Text lobbyTextField;
    private Dictionary<ulong, bool> readyStatus;
    private Dictionary<ulong, string> playerNames;
    private enum GameType { Classic, Upgraded, Rapids };
    private GameType selectedGameType;
    private string localPlayerName;
    private Button readyButton;
    private bool playerReady = false;

    public override void OnNetworkSpawn() {
        if (IsServer) {
            playerNames = new Dictionary<ulong, string>();
            readyStatus = new Dictionary<ulong, bool> {
                { NetworkManager.LocalClientId, false }
            };
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
        }
        lobbyCanvas = transform.GetChild(0).GetComponent<Canvas>();
        lobbyTextField = lobbyCanvas.transform.GetChild(0).GetComponent<TMP_Text>();
        localPlayerName = FindObjectOfType<LoginSessionManager>().Username;
        readyButton = lobbyCanvas.transform.GetChild(4).GetComponent<Button>();
        readyButton.onClick.AddListener(ReadyButtonClicked);
        RegisterPlayerInLobbyServerRPC(localPlayerName);
        if (IsServer) GenerateAndUpdatePlayersLobbyText();
    }

    private void Singleton_OnClientConnectedCallback(ulong obj) {
        if (!IsServer) return; //Technically impossible
        readyStatus.Add(obj, false);
        GenerateAndUpdatePlayersLobbyText();
        Debug.Log("Player " + obj + " connected!");
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj) {
        if (!IsServer) return; //Technically impossible
        readyStatus.Remove(obj);
        playerNames.Remove(obj);
        GenerateAndUpdatePlayersLobbyText();
        Debug.Log("Player " + obj + " disconnected.");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerInLobbyServerRPC(string username, ServerRpcParams serverRpcParams = default) {
        Debug.Log("Registering player id " + serverRpcParams.Receive.SenderClientId + " in lobby.");
        playerNames.Add(serverRpcParams.Receive.SenderClientId, username);
    }

    private void GenerateAndUpdatePlayersLobbyText() {
        string lobbyText;
        lobbyText = "Selected game mode: <color=orange>" + selectedGameType.ToString() + "</color>\n";
        lobbyText += "Players in lobby: \n";
        foreach (var playerClient in readyStatus.Keys) {
            lobbyText += readyStatus[playerClient] ? "<color=green>" : "<color=red>";
            lobbyText += playerNames[playerClient] + "\n";
        }
        UpdatePlayersLobbyTextClientRPC(lobbyText);
    }

    [ClientRpc]
    void UpdatePlayersLobbyTextClientRPC(string text) {
        lobbyTextField.text = text;
    }

    void Update() {
        if (!IsOwner) return;
        foreach (var status in readyStatus) {
            if (status.Value == false) {
                HideStartGameButton();
                return;
            }
        }
        ShowStartGameButton();
    }

    private void ShowStartGameButton() {
        lobbyCanvas.transform.GetChild(5).gameObject.SetActive(true);
    }

    private void HideStartGameButton() {
        lobbyCanvas.transform.GetChild(5).gameObject.SetActive(false);
    }

    private void ReadyButtonClicked() {
        playerReady = !playerReady;
        var colors = readyButton.colors;
        colors.normalColor = playerReady ? Color.green : Color.white;
        colors.highlightedColor = colors.normalColor;
        colors.selectedColor = colors.normalColor;
        readyButton.colors = colors;
        SetReadyStatusServerRPC(playerReady);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyStatusServerRPC(bool status, ServerRpcParams param = default) {
        readyStatus[param.Receive.SenderClientId] = status;
        Debug.Log("Ready status changed for client id " + param.Receive.SenderClientId + " to " + (status ? "ready" : "not ready"));
        GenerateAndUpdatePlayersLobbyText();
    }

    public void SetGameTypeClassic() {
        if (!IsOwner) return;
        selectedGameType = GameType.Classic;
        GenerateAndUpdatePlayersLobbyText();
    }

    public void SetGameTypeUpgraded() {
        if (!IsOwner) return;
        selectedGameType = GameType.Upgraded;
        GenerateAndUpdatePlayersLobbyText();
    }

    public void SetGameTypeRapids() {
        if (!IsOwner) return;
        selectedGameType = GameType.Rapids;
        GenerateAndUpdatePlayersLobbyText();
    }

    public void StartGame() {
        switch (selectedGameType) {
            case GameType.Classic: {
                    Debug.Log("Starting Classic mode!");
                    //NetworkManager.Singleton.SceneManager.LoadScene("ClassicMode", LoadSceneMode.Single);
                    break;
                }
            case GameType.Upgraded: {
                    Debug.Log("Starting Upgraded mode!");
                    //NetworkManager.Singleton.SceneManager.LoadScene("UpgradedMode", LoadSceneMode.Single);
                    break;
                }
            case GameType.Rapids: {
                    Debug.Log("Starting Rapids mode!");
                    //NetworkManager.Singleton.SceneManager.LoadScene("RapidsMode", LoadSceneMode.Single);
                    break;
                }
        }
    }
}
