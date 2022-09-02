using Unity.Netcode;
using UnityEngine;

public class GameSessionManager : NetworkBehaviour {
    public static GameSessionManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
    }

    NetworkVariable<ulong> WhitePlayerID = new NetworkVariable<ulong>();
    NetworkVariable<ulong> BlackPlayerID = new NetworkVariable<ulong>();

    public NetworkVariable<bool> WhitePlayersTurn = new NetworkVariable<bool>(true);
    public bool MyTurn = false;
    public bool GameStarted = false;
    public Player localPlayer = null;

    [SerializeField] private GameObject ClassicPlayerObject;

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
        if (IsClient) {
            SpawnPlayerObject(NetworkManager.Singleton.LocalClientId);
            Debug.Log("Game initialized as host therefore spawned a player object");
        }
    }

    [ClientRpc]
    public void InitializeGameClientRPC() {
        localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>();
        localPlayer.Setup(WhitePlayerID.Value, BlackPlayerID.Value);
        WhitePlayersTurn.OnValueChanged += InvokeTurnChangeLogic;
        GameStarted = true;
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

    //Rewrite paused

    private void Singleton_OnClientConnectedCallback(ulong clientID) {
        Debug.Log("on client connected callback uid: " + clientID);
        if (IsServer && NetworkManager.Singleton.name != "TestingPurposeNetworkManager") SpawnPlayerObject(clientID);
    }

    private void SpawnPlayerObject(ulong clientID) {
        var player = Instantiate(ClassicPlayerObject);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
        Debug.Log("Spawned classic mode player object for clientID: " + clientID);
    }

    /*public void AdvanceTurn() {
        WhitesTurn.Value = !WhitesTurn.Value;
    }*/

    public void CheckForChecks() {
        if (MoveManager.Instance.IsKingInCheck()) Debug.Log("My king is in check!");
    }

    public void OnMyTurn() {
        var gameBoard = BoardManager.Instance.board;
        int legalMoves = 0;
        foreach (var space in gameBoard) {
            if (space.transform.childCount > 0) {
                var piece = space.transform.GetChild(0).GetComponent<Piece>();
                if (piece.ID * (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().PlayerColor ? 1 : -1) > 0)
                    legalMoves += piece.PossibleMoves.Count;
            }
        }
        Debug.Log("No of legal moves: " + legalMoves);
        if (MoveManager.Instance.IsKingInCheck() && legalMoves == 0) {
            Debug.Log("Game end - opponent wins!");
            Debug.Log("You lost ;)");
            if (IsServer) EndGameClientRPC(true, BlackPlayerID.Value); else EndGameServerRPC(true, WhitePlayerID.Value);
            //Display canvas
            return;
        }
        if (legalMoves == 0) {
            Debug.Log("Game end - stalemate");
            if (IsServer) EndGameClientRPC(false, default); else EndGameServerRPC(false, default);
            //Display canvas
        }

        NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().OnMyTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndGameServerRPC(bool winnerExists, ulong winnerID) {
        if (winnerExists) {
            if (winnerID == WhitePlayerID.Value) {
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

    [ClientRpc]
    public void EndGameClientRPC(bool winnerExists, ulong winnerID) {
        if (IsServer) return;
        if (winnerExists) {
            if (winnerID == BlackPlayerID.Value) {
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

    [ServerRpc(RequireOwnership = false)]
    public void AdvanceTurnServerRPC() {
        //AdvanceTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void MovePieceServerRPC(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
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
