using Unity.Netcode;
using UnityEngine;

public class GameSessionManager : NetworkBehaviour {
    public static GameSessionManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(this); else Instance = this;
    }

    NetworkVariable<ulong> Player1 = new NetworkVariable<ulong>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<ulong> Player2 = new NetworkVariable<ulong>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> WhitesTurn = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool MyTurn = false;

    public void StartGame() {
        Player1.Value = NetworkManager.Singleton.LocalClientId;
        Player2.Value = NetworkManager.Singleton.ConnectedClientsIds[1];
        WhitesTurn.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRPC() {
        StartGame();
    }

    void Start() {
        Player1.OnValueChanged += PrintValues;
        Player2.OnValueChanged += PrintValues;
        WhitesTurn.OnValueChanged += SetMyTurn;
    }

    public void PrintValues(ulong old, ulong ne) {
        Debug.Log("Game Started by IDs updated");
        Debug.Log("Player1: " + Player1.Value);
        Debug.Log("Player2: " + Player2.Value);
    }

    private void SetMyTurn(bool old, bool nef) {
        if (IsServer) MyTurn = WhitesTurn.Value; else MyTurn = !WhitesTurn.Value;
        if (MyTurn) Debug.Log("My Turn!"); else Debug.Log("Opponent's turn!");
    }

    public void AdvanceTurn() {
        WhitesTurn.Value = !WhitesTurn.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void MovePieceServerRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
        var _oldPiece = BoardManager.Instance.GetPieceFromSpace(newPiecePosition);
        if (_oldPiece != null) Destroy(_oldPiece.gameObject);
        var movedPiece = BoardManager.Instance.GetPieceFromSpace(oldPiecePosition);
        movedPiece.transform.parent = BoardManager.Instance.board[newPiecePosition.x, newPiecePosition.y].transform;
        movedPiece.transform.localPosition = Vector3.zero;
        Debug.Log("Moved " + movedPiece.name + " from " + oldPiecePosition + " to " + newPiecePosition);
        AdvanceTurn();
    }

    [ClientRpc]
    public void MovePieceClientRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
        if (IsServer) return;
        var _oldPiece = BoardManager.Instance.GetPieceFromSpace(newPiecePosition);
        if (_oldPiece != null) Destroy(_oldPiece.gameObject);
        var movedPiece = BoardManager.Instance.GetPieceFromSpace(oldPiecePosition);
        movedPiece.transform.parent = BoardManager.Instance.board[newPiecePosition.x, newPiecePosition.y].transform;
        movedPiece.transform.localPosition = Vector3.zero;
        Debug.Log("Moved " + movedPiece.name + " from " + oldPiecePosition + " to " + newPiecePosition);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SummonGhostPawnBehindServerRPC(Vector2Int behind, Vector2Int parentPawnLocation) {
        Debug.Log("Server summoning a ghost on " + behind);
        BoardManager.Instance.SetSpace(behind, BoardManager.PieceType.WPawn);
        var ghost = BoardManager.Instance.GetPieceFromSpace(behind);
        ghost.GetComponent<Pawn>().InitGhost(parentPawnLocation);
    }

    [ClientRpc]
    public void SummonGhostPawnBehindClientRPC(Vector2Int behind, Vector2Int parentPawnLocation) {
        if (IsServer) return;
        Debug.Log("Client summoning a ghost on " + behind);
        BoardManager.Instance.SetSpace(behind, BoardManager.PieceType.WPawn);
        var ghost = BoardManager.Instance.GetPieceFromSpace(behind);
        ghost.GetComponent<Pawn>().InitGhost(parentPawnLocation);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DisposeOfGhostServerRPC(Vector2Int location) {
        (BoardManager.Instance.GetPieceFromSpace(location) as Pawn)?.DisposeOfGhost();
    }

    [ClientRpc]
    public void DisposeOfGhostClientRPC(Vector2Int location) {
        if (IsServer) return;
        (BoardManager.Instance.GetPieceFromSpace(location) as Pawn)?.DisposeOfGhost();
    }
}
