using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private GameObject playerObject;

    void Start() {
        playerObject = transform.gameObject;
        SetLocalPlayerName(FindObjectOfType<Canvas>().transform.GetChild(0).GetComponent<TMP_InputField>().text);
    }

    void Update() {
        //Debug.Log(PlayerName.Value);

        transform.position = Position.Value;
        Debug.Log("Alive with name: " + PlayerName.Value);
    }

    public void SetLocalPlayerName(string name) {
        if (IsLocalPlayer) {
            PlayerName.Value = name;
            Debug.Log("I am");
            Debug.Log(PlayerName.Value);
        }
    }

    public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            Move();
        }
    }

    public void Move() {
        if (NetworkManager.Singleton.IsServer) {
            var randomPos = GetRandomPosition();
            transform.position = randomPos;
            Position.Value = randomPos;
        } else {
            SubmitPositionRequestServerRPC();
        }
    }

    [ServerRpc]
    void SubmitPositionRequestServerRPC(ServerRpcParams rpcParams = default) {
        Position.Value = GetRandomPosition();
    }

    static Vector3 GetRandomPosition() {
        return new Vector3(Random.Range(-10f, 10f), 1f, Random.Range(-10f, 10f));
    }
}
