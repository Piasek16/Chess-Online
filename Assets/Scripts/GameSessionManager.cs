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
    public int HalfmoveClock = 0;
    public int FullmoveNumber = 1;
    public Player localPlayer = null;
    //public Player opponentPlayer = null;

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
        FixedString128Bytes fenState = BoardManager.Instance.ExportFenState();
        fenState += " " + (WhitePlayersTurn.Value ? "w" : "b");
        //Castling rights
        //En pessant target
        fenState += " " + HalfmoveClock;
        fenState += " " + FullmoveNumber;
        FENBoardState.Value = fenState;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AdvanceTurnServerRPC() {
        WhitePlayersTurn.Value = !WhitePlayersTurn.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void MovePieceServerRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
        localPlayer.RestoreOfficialBoard();
        BoardManager.Instance.MovePiece(oldPiecePosition, newPiecePosition);


        Debug.Log("[ServerRPC] " + "Moved " + BoardManager.Instance.GetPieceFromSpace(oldPiecePosition).name + " from " + oldPiecePosition + " to " + newPiecePosition);
        NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().RestoreOfficialBoard();
        BoardManager.Instance.MovePiece(oldPiecePosition, newPiecePosition);
        BoardManager.Instance.GetPieceFromSpace(newPiecePosition)?.FirstMoveMade();
        BoardManager.Instance.HighlightMove(oldPiecePosition, newPiecePosition);
    }

    [ClientRpc]
    public void MovePieceClientRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
        if (IsServer) return;
        Debug.Log("[ClientRPC] " + "Moved " + BoardManager.Instance.GetPieceFromSpace(oldPiecePosition).name + " from " + oldPiecePosition + " to " + newPiecePosition);
        NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().RestoreOfficialBoard();
        BoardManager.Instance.MovePiece(oldPiecePosition, newPiecePosition);
        BoardManager.Instance.GetPieceFromSpace(newPiecePosition)?.FirstMoveMade();
        BoardManager.Instance.HighlightMove(oldPiecePosition, newPiecePosition);
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

    [ServerRpc(RequireOwnership = false)]
    public void PromotePawnToServerRPC(BoardManager.PieceType pieceType, Vector2Int location) {
        Debug.Log("[ServerRPC] Promoted pawn from " + location + " to " + pieceType);
        BoardManager.Instance.DestroyPiece(location);
        BoardManager.Instance.SetSpace(location, pieceType);
    }

    [ClientRpc]
    public void PromotePawnToClientRPC(BoardManager.PieceType pieceType, Vector2Int location) {
        if (IsServer) return;
        Debug.Log("[ClientRPC] Promoted pawn from " + location + " to " + pieceType);
        BoardManager.Instance.DestroyPiece(location);
        BoardManager.Instance.SetSpace(location, pieceType);
    }
}
