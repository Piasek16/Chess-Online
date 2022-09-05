using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameSessionManager : NetworkBehaviour {
    public static GameSessionManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
    }

    public NetworkVariable<ulong> WhitePlayerID = new NetworkVariable<ulong>();
    public NetworkVariable<ulong> BlackPlayerID = new NetworkVariable<ulong>();
    public NetworkVariable<bool> WhitePlayersTurn = new NetworkVariable<bool>(true);
    public NetworkVariable<FixedString128Bytes> FENBoardState = new NetworkVariable<FixedString128Bytes>();

    public bool MyTurn = false;
    public bool GameStarted = false;
    public string EnPessantSquare = "-";
    public int HalfmoveClock = 0;
    public int FullmoveNumber = 1;
    public Player localPlayer = null;

    [SerializeField] private GameObject ClassicPlayerObject;
    [SerializeField] private string fenStartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public override void OnNetworkSpawn() {
        if (!IsServer) return;
        InitializeGameServer();
        InitializeGameClientRPC();
    }

    private void InitializeGameServer() {
        var connectedPlayers = NetworkManager.Singleton.ConnectedClientsIds;
        WhitePlayerID.Value = (ulong)Random.Range(0, connectedPlayers.Count);
        foreach (ulong id in connectedPlayers) {
            if (id != WhitePlayerID.Value) { BlackPlayerID.Value = id; break; }
        }
        Debug.Log("Server initialized game with following parameters:");
        Debug.Log("White player id: " + WhitePlayerID.Value);
        Debug.Log("Black player id: " + BlackPlayerID.Value);
        foreach (ulong id in connectedPlayers) {
            SpawnPlayerObject(id);
        }
    }

    [ClientRpc]
    private void InitializeGameClientRPC() {
        localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>();
        WhitePlayersTurn.OnValueChanged += InvokeTurnChangeLogic;
        GameStarted = true;
    }

    private void SpawnPlayerObject(ulong clientID) {
        var player = Instantiate(ClassicPlayerObject);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
        Debug.Log("Spawned classic mode player object for clientID: " + clientID);
    }

    private void InvokeTurnChangeLogic(bool oldTurn, bool newTurn) {
        SetMyTurn();
        if (MyTurn) {
            OnMyTurn();
            CheckForChecks();
        }
        if (IsServer) {
            DespawnEnPessantIfPossible();
        }
    }

    private void SetMyTurn() {
        if (localPlayer.PlayerColor == true) MyTurn = WhitePlayersTurn.Value; else MyTurn = !WhitePlayersTurn.Value;
        if (MyTurn) Debug.Log("My Turn!"); else Debug.Log("Opponent's turn!");
    }

    private void CheckForChecks() {
        if (MoveManager.Instance.IsKingInCheck()) {
            Debug.Log("My king is in check!");
            //Play check sound from sound manager
        }
    }

    private bool IsPieceMyColor(Piece piece) {
        return piece.ID * (localPlayer.PlayerColor ? 1 : -1) > 0;
    }

    private void OnMyTurn() {
        var gameBoard = BoardManager.Instance.board;
        int legalMoves = 0;
        foreach (var space in gameBoard) {
            if (space.transform.childCount > 0) {
                var piece = space.transform.GetChild(0).GetComponent<Piece>();
                if (IsPieceMyColor(piece))
                    legalMoves += piece.PossibleMoves.Count;
            }
        }
        Debug.Log("No of legal moves: " + legalMoves);
        if (MoveManager.Instance.IsKingInCheck() && legalMoves == 0) {
            EndGameRequestServerRPC(true); //checkmate
        } else if (legalMoves == 0) {
            EndGameRequestServerRPC(false); //stalemate
        } else {
            localPlayer.OnMyTurn();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndGameRequestServerRPC(bool kingCheckmated, ServerRpcParams serverRpcParams = default) {
        if (kingCheckmated) {
            EndGameClientRPC(true, serverRpcParams.Receive.SenderClientId == WhitePlayerID.Value ? BlackPlayerID.Value : WhitePlayerID.Value);
            Debug.Log("Game end by checkmate!");
            Debug.Log("Winner is " + (serverRpcParams.Receive.SenderClientId == WhitePlayerID.Value ? "Black player" : "White player"));
        } else {
            Debug.Log("Game end by stalemate!");
            EndGameClientRPC(false, default);
        }
    }

    [ClientRpc]
    private void EndGameClientRPC(bool kingCheckmated, ulong winnerID) {
        if (kingCheckmated) {
            if (winnerID == NetworkManager.Singleton.LocalClientId) {
                //Display winner canvas
                Debug.Log("Game end - you win!");
            } else {
                //Display loser canvas
                Debug.Log("Game end - you lose!");
            }
        } else {
            //Display draw canvas
            Debug.Log("Game end - stalemate");
        }
    }

    private void UpdateFENBoardState() {
        FixedString128Bytes fenState = BoardManager.Instance.GetFENBoardState();
        fenState += " " + (WhitePlayersTurn.Value ? "w" : "b");
        fenState += " " + BoardManager.Instance.GetFENCastlingRights();
        fenState += " " + EnPessantSquare;
        fenState += " " + HalfmoveClock;
        fenState += " " + FullmoveNumber;
        FENBoardState.Value = fenState;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AdvanceTurnServerRPC() {
        WhitePlayersTurn.Value = !WhitePlayersTurn.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void MovePieceServerRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition, ServerRpcParams param = default) {
        MovePieceClientRPC(oldPiecePosition, newPiecePosition, GetOtherPlayerTarget(param));
        HalfmoveClock++;
        if (!WhitePlayersTurn.Value) FullmoveNumber++;
        UpdateFENBoardState();
        Debug.Log("[ServerRPC] " + "Moved " + BoardManager.Instance.GetPieceFromSpace(newPiecePosition).name + " from " + oldPiecePosition + " to " + newPiecePosition);
    }

    [ClientRpc]
    public void MovePieceClientRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition, ClientRpcParams param = default) {
        localPlayer.RevertPremoves();
        BoardManager.Instance.MovePiece(oldPiecePosition, newPiecePosition, true);
        BoardManager.Instance.GetPieceFromSpace(newPiecePosition)?.FirstMoveMade();
        BoardManager.Instance.HighlightMove(oldPiecePosition, newPiecePosition);
        Debug.Log("[ClientRPC] " + "Moved " + BoardManager.Instance.GetPieceFromSpace(oldPiecePosition).name + " from " + oldPiecePosition + " to " + newPiecePosition);
        localPlayer.RestorePremoves();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SummonGhostPawnServerRPC(Vector2Int parentPawnPosition, ServerRpcParams param = default) {
        bool senderColor = param.Receive.SenderClientId == WhitePlayerID.Value;
        Vector2Int ghostLocation = new Vector2Int(parentPawnPosition.x, parentPawnPosition.y + (senderColor ? -1 : 1));
        Debug.Log("[ServerRPC] Summoning a ghost on " + ghostLocation);
        SummonGhostPawnClientRPC(parentPawnPosition, ghostLocation);
        EnPessantSquare = BoardManager.Vector2IntToBoardLocation(ghostLocation);
    }

    [ClientRpc]
    public void SummonGhostPawnClientRPC(Vector2Int parentPawnPosition, Vector2Int ghostLocation) {
        BoardManager.Instance.SummonGhostPawn(parentPawnPosition, ghostLocation);
        Debug.Log("[ClientRPC] Client summoned a ghost on " + ghostLocation);
    }

    /// <summary>
    /// To avoid preemptive despawning register en pessant after advancing turn
    /// </summary>
    private void DespawnEnPessantIfPossible() {
        if (EnPessantSquare != "-") {
            DisposeOfGhostClientRPC(BoardManager.BoardLocationToVector2Int(EnPessantSquare));
            EnPessantSquare = "-";
        }
    }

    [ClientRpc]
    public void DisposeOfGhostClientRPC(Vector2Int location) {
        (BoardManager.Instance.GetPieceFromSpace(location) as Pawn)?.DisposeOfGhost();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PromotePawnToServerRPC(BoardManager.PieceType pieceType, Vector2Int location, ServerRpcParams param = default) {
        Debug.Log("[ServerRPC] Promoting pawn from " + location + " to " + pieceType);
        PromotePawnToClientRPC(pieceType, location, GetOtherPlayerTarget(param));
    }

    [ClientRpc]
    public void PromotePawnToClientRPC(BoardManager.PieceType pieceType, Vector2Int location, ClientRpcParams param = default) {
        BoardManager.Instance.DestroyPiece(location);
        BoardManager.Instance.SetSpace(location, pieceType);
        Debug.Log("[ClientRPC] Promoted pawn from " + location + " to " + pieceType);
    }

    private ClientRpcParams GetOtherPlayerTarget(ServerRpcParams senderParams) {
        ulong serverClientID = NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId;
        bool isSenderServer = senderParams.Receive.SenderClientId == serverClientID;
        ulong opponentID = localPlayer.PlayerColor ? BlackPlayerID.Value : WhitePlayerID.Value;
        return new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = new ulong[] {
                    isSenderServer ? opponentID : serverClientID,
                }
            }
        };
    }
}
