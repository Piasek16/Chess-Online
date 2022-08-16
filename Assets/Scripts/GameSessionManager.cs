using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

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
        WhitesTurn.OnValueChanged += CheckForChecks;
    }

    public void PrintValues(ulong old, ulong ne) {
        Debug.Log("Game Started by IDs updated");
        Debug.Log("Player1: " + Player1.Value);
        Debug.Log("Player2: " + Player2.Value);
    }

    private void SetMyTurn(bool old, bool nef) {
        if (IsServer) MyTurn = WhitesTurn.Value; else MyTurn = !WhitesTurn.Value;
        if (MyTurn) Debug.Log("My Turn!"); else Debug.Log("Opponent's turn!");
        if (MyTurn) OnMyTurn();
    }

    public void AdvanceTurn() {
        WhitesTurn.Value = !WhitesTurn.Value;
    }

    public void CheckForChecks(bool old, bool ne) {
        if (MoveManager.Instance.IsKingInCheck()) Debug.Log("My king is in check!");
    }

    public void OnMyTurn() {
        var gameBoard = BoardManager.Instance.board;
        int legalMoves = 0;
        foreach (var space in gameBoard) {
            if (space.transform.childCount > 0) {
                var piece = space.transform.GetChild(0).GetComponent<Piece>();
                if (piece.ID * (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().playerColor ? 1 : -1) > 0)
                    legalMoves += piece.PossibleMoves.Count;
            }
        }
        Debug.Log("No of legal moves: " + legalMoves);
        if (MoveManager.Instance.IsKingInCheck() && legalMoves == 0) {
            Debug.Log("Game end - opponent wins!");
            if (IsServer) EndGameClientRPC(true, Player2.Value); else EndGameServerRPC(true, Player1.Value);
            //Display canvas
            return;
        }
        if (legalMoves == 0) {
            Debug.Log("Game end - stalemate");
            if (IsServer) EndGameClientRPC(false, default); else EndGameServerRPC(false, default);
            //Display canvas
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndGameServerRPC(bool winnerExists, ulong winnerID) {
        if (winnerExists) {
            if (winnerID == Player1.Value) {
                //Display winner canvas
            } else {
                //Display loser canvas
            }
        } else {
            //Display draw canvas
        }
    }

    [ClientRpc]
    public void EndGameClientRPC(bool winnerExists, ulong winnerID) {
        if (IsServer) return;
        if (winnerExists) {
            if (winnerID == Player2.Value) {
                //Display winner canvas
            } else {
                //Display loser canvas
            }
        } else {
            //Display draw canvas
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AdvanceTurnServerRPC() {
        AdvanceTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void MovePieceServerRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
        BoardManager.Instance.MovePiece(oldPiecePosition, newPiecePosition);
    }

    [ClientRpc]
    public void MovePieceClientRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
        if (IsServer) return;
        BoardManager.Instance.MovePiece(oldPiecePosition, newPiecePosition);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SummonGhostPawnBehindServerRPC(Vector2Int behind, Vector2Int parentPawnPosition) {
        Debug.Log("Server summoning a ghost on " + behind);
        BoardManager.Instance.SummonGhostPawn(behind, parentPawnPosition);
    }

    [ClientRpc]
    public void SummonGhostPawnBehindClientRPC(Vector2Int behind, Vector2Int parentPawnPosition) {
        if (IsServer) return;
        Debug.Log("Client summoning a ghost on " + behind);
        BoardManager.Instance.SummonGhostPawn(behind, parentPawnPosition);
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
