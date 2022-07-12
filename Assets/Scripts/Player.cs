using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class Player : NetworkBehaviour {
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<bool> PlayerColor = new NetworkVariable<bool>();
    private GameObject playerObject;

    Vector2 whitePosition = new Vector2(-2f, 1f);
    Vector2 blackPosition = new Vector2(-2f, 6f);

    void Awake() {
        playerObject = transform.gameObject;
    }

    void Update() {

    }

    public override void OnNetworkSpawn() {
        if (IsOwner) SetPlayerColorServerRPC();
        if (PlayerColor.Value) {
            transform.position = whitePosition;
        } else {
            transform.position = blackPosition;
        }
        PlayerName.OnValueChanged += UpdatePlayerObjectName;
        if (IsOwner) {
            var _playerName = FindObjectOfType<Canvas>().GetComponent<LoginManager>().username;
            SetLocalPlayerNameServerRpc(_playerName);
        } else {
            playerObject.name = "Player (" + PlayerName.Value + ")";
        }
        base.OnNetworkSpawn();
    }

    void UpdatePlayerObjectName(FixedString128Bytes previous, FixedString128Bytes newValue) {
        playerObject.name = "Player (" + PlayerName.Value + ")";
    }

    [ServerRpc]
    void SetPlayerColorServerRPC() {
        PlayerColor.Value = IsHost;
    }

    [ServerRpc]
    public void SetLocalPlayerNameServerRpc(string name) {
        PlayerName.Value = name;
        Debug.Log("Joined: " + name);
    }
}
