using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour {

    private Canvas lobbyCanvas;
    private TMP_Text lobbyTextField;
    private Dictionary<ulong, bool> readyStatus;
    private Dictionary<ulong, string> playerNames;
    private enum GameType { Classic, Upgraded, Rapids };
    private GameType selectedGameType;
    private string localPlayerName;

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
        RegisterPlayerInLobbyServerRPC(localPlayerName);
        GenerateAndUpdatePlayersLobbyText();
    }

    private void Singleton_OnClientConnectedCallback(ulong obj) {
        if (!IsServer) return; //Technically impossible
        readyStatus.Add(obj, false);
        GenerateAndUpdatePlayersLobbyText();
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj) {
        if (!IsServer) return; //Technically impossible
        readyStatus.Remove(obj);
        GenerateAndUpdatePlayersLobbyText();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerInLobbyServerRPC(string username, ServerRpcParams serverRpcParams = default) {
        Debug.Log("Registering player id " + serverRpcParams.Receive.SenderClientId + "in lobby.");
        playerNames.Add(serverRpcParams.Receive.SenderClientId, username);
    }

    private void GenerateAndUpdatePlayersLobbyText() {
        string lobbyText;
        lobbyText = "Selected game mode: " + selectedGameType.ToString() + "\n";
        lobbyText += "Players in lobby: \n";
        foreach (var playerClient in NetworkManager.Singleton.ConnectedClientsList) {
            lobbyText += readyStatus[playerClient.ClientId] ? "<color=green>" : "<color=red>";
            lobbyText += playerNames[playerClient.ClientId] + "\n";
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
                //HideStartGameButton();
                return;
            }
        }
        //ShowStartGameButton();
    }

    private void ShowStartGameButton() {
        transform.GetChild(0).gameObject.SetActive(true);
    }

    private void HideStartGameButton() {
        transform.GetChild(0).gameObject.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyStatusServerRPC(bool status, ServerRpcParams param = default) {
        readyStatus[param.Receive.SenderClientId] = status;
        Debug.Log("Ready status changed for " + param.Receive.SenderClientId + " to " + status);
    }

    public void SetGameTypeClassic() {
        selectedGameType = GameType.Classic;
    }

    public void SetGameTypeUpgraded() {
        selectedGameType = GameType.Upgraded;
    }

    public void SetGameTypeRapids() {
        selectedGameType = GameType.Rapids;
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
