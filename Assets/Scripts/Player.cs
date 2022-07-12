using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<bool> PlayerColor = new NetworkVariable<bool>();
    private GameObject playerObject;

    Vector2 whitePosition = new Vector2(-2f, 1f);
    Vector2 blackPosition = new Vector2(-2f, 5f);

    void Start() {
        playerObject = transform.gameObject;
        SetLocalPlayerName(FindObjectOfType<Canvas>().transform.GetChild(0).GetComponent<TMP_InputField>().text);
    }

    void Update() {
        
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) SetPlayerColorServerRPC();
        base.OnNetworkSpawn();
    }

    [ServerRpc]
    void SetPlayerColorServerRPC() {
        PlayerColor.Value = IsHost;
        if (PlayerColor.Value) {
            transform.position = whitePosition;
        } else {
            transform.position = blackPosition;
        }
    }

    public void SetLocalPlayerName(string name) {
        if (IsLocalPlayer) {
            //PlayerName.Value = name;
            Debug.Log("I am");
            Debug.Log(name);
        }
    }

   
}
