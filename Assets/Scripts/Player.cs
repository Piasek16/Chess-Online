using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{

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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Position.Value;
    }
}
