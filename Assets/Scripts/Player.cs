using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class Player : NetworkBehaviour {
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    bool playerColor = false;
    Vector2 whitePosition = new Vector2(-2f, 1f);
    Vector2 blackPosition = new Vector2(-2f, 6f);

    public override void OnNetworkSpawn() {

        PlayerName.OnValueChanged += UpdatePlayerObjectName;
        if (IsOwner) {
            var _playerName = FindObjectOfType<Canvas>().GetComponent<LoginManager>().username;
            PlayerName.Value = _playerName;
            UpdatePlayerObjectName(default, default);
        }

        playerColor = IsOwnedByServer;
        if (playerColor) {
            transform.position = whitePosition;
        } else {
            transform.position = blackPosition;
        }
    }

    void UpdatePlayerObjectName(FixedString128Bytes previous, FixedString128Bytes newValue) {
        gameObject.name = "Player (" + PlayerName.Value + ")";
    }
}
