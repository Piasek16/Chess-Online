using Unity.Netcode;
using UnityEngine;

public class GameSessionManager : NetworkBehaviour {
	public static GameSessionManager Instance { get; private set; }
	void Awake() {
		if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
	}

	void Start() {
		boardManager = BoardManager.Instance;
		gameLogicManager = ClassicGameLogicManager.Instance;
	}

	public NetworkVariable<ulong> WhitePlayerID = new NetworkVariable<ulong>();
	public NetworkVariable<ulong> BlackPlayerID = new NetworkVariable<ulong>();

	public FENGameState OfficialFENGameState;
	public bool GameStarted = false;
	public Player LocalPlayer = null;
	public Player OpponentPlayer = null;
	public bool MyTurn => LocalPlayer.PlayerColor == OfficialFENGameState.IsWhiteTurn;

	[SerializeField] private GameObject ClassicPlayerObject;
	[SerializeField] private string fenStartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
	private BoardManager boardManager;
	private ClassicGameLogicManager gameLogicManager;

	public override void OnNetworkSpawn() {
		if (!IsServer) return;
		if (Application.isEditor) return;
		InitializeGameServer();
		InitializeGameClientRPC(fenStartingPosition, WhitePlayerID.Value, BlackPlayerID.Value);
	}

	public void InitializeTestGame() {
		InitializeGameServer();
		InitializeGameClientRPC(fenStartingPosition, WhitePlayerID.Value, BlackPlayerID.Value);
	}

	private void InitializeGameServer() {
		var connectedPlayers = NetworkManager.Singleton.ConnectedClientsIds;
		WhitePlayerID.Value = (ulong)Random.Range(0, connectedPlayers.Count);
		foreach (ulong id in connectedPlayers) {
			if (id != WhitePlayerID.Value) { BlackPlayerID.Value = id; break; }
		}
		Debug.Log("[Server] Game initialized with following parameters:");
		Debug.Log("White player id: " + WhitePlayerID.Value);
		Debug.Log("Black player id: " + BlackPlayerID.Value);
		foreach (ulong id in connectedPlayers) {
			SpawnPlayerObject(id);
		}
	}

	private void SpawnPlayerObject(ulong clientID) {
		var player = Instantiate(ClassicPlayerObject);
		player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
		Debug.Log("[Server] Spawned classic mode player object for clientID: " + clientID);
	}

	[ClientRpc]
	private void InitializeGameClientRPC(string fenGameState, ulong whitePlayerID, ulong blackPlayerID) {
		LocalPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>();
		foreach (Player p in FindObjectsOfType<Player>()) {
			if (p.OwnerClientId != LocalPlayer.OwnerClientId) OpponentPlayer = p;
		}
		LocalPlayer.SetupPlayerData(whitePlayerID, blackPlayerID);
		OpponentPlayer?.SetupPlayerData(whitePlayerID, blackPlayerID); // Added null propagation to enable single player move testing
		LoadStateFromFEN(fenGameState);
		GameStarted = true;
	}

	public void LoadStateFromFEN(string FENGameState) {
		OfficialFENGameState = new FENGameState(FENGameState);
		boardManager.LoadBoardStateFromFEN(OfficialFENGameState.BoardState);
		boardManager.LoadCastlingRightsFromFEN(OfficialFENGameState.CastlingAvailability);
		boardManager.LoadEnPassantTargetFromFEN(OfficialFENGameState.EnPassantTarget);
	}

	public void EndMyTurn() {
		EndTurnServerRPC();
	}

	[ServerRpc(RequireOwnership = false)]
	private void EndTurnServerRPC() {
		InvokeTurnChangeLogicClientRpc();
	}

	[ClientRpc]
	private void InvokeTurnChangeLogicClientRpc() {
		OfficialFENGameState.SetActiveColor(!OfficialFENGameState.IsWhiteTurn);
		OfficialFENGameState.HalfMoveClock++;
		if (OfficialFENGameState.IsWhiteTurn) OfficialFENGameState.FullMoveNumber++;
		if (MyTurn) {
			Debug.Log("[Turn Change] My Turn!");
			OnMyTurn();
			DisplayCheckInfo();
		} else {
			Debug.Log("[Turn Change] Opponent's turn!");
		}
	}

	private void OnMyTurn() {
		var numberOfLegalMoves = gameLogicManager.NumberOfLegalMoves;
		Debug.Log($"No of legal moves: {numberOfLegalMoves}");
		if (MoveManager.Instance.IsKingInCheck() && numberOfLegalMoves == 0) {
			EndGameRequestServerRPC(true); //checkmate
		} else if (numberOfLegalMoves == 0) {
			EndGameRequestServerRPC(false); //stalemate
		} else {
			LocalPlayer.OnMyTurn();
		}
	}

	private void DisplayCheckInfo() {
		if (MoveManager.Instance.IsKingInCheck()) {
			Debug.Log("My king is in check!");
			//Play check sound from sound manager
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

	[ServerRpc(RequireOwnership = false)]
	public void RelayMoveToOtherPlayerServerRPC(Move move, ServerRpcParams param = default) {
		MovePieceClientRPC(move, GetOtherPlayerTarget(param));
		Debug.Log($"[Server] Relayed: {move} to other player.");
	}

	[ClientRpc]
	public void MovePieceClientRPC(Move move, ClientRpcParams param = default) {
		boardManager.ExecuteMove(move);
		boardManager.HighlightMove(move);
		Debug.Log($"[Client] Executed: {move} received from server.");
	}

	[ServerRpc(RequireOwnership = false)]
	public void RelayPawnPromotionServerRPC(Vector2Int pawnLocation, BoardManager.PieceType promotionTarget, ServerRpcParams param = default) {
		PromotePawnClientRPC(pawnLocation, promotionTarget, GetOtherPlayerTarget(param));
		Debug.Log($"[Server] Relayed pawn promotion to {promotionTarget} on {pawnLocation}");
	}

	[ClientRpc]
	public void PromotePawnClientRPC(Vector2Int pawnLocation, BoardManager.PieceType promotionTarget, ClientRpcParams param = default) {
		gameLogicManager.PromotePawn(boardManager.GetPieceFromSpace(pawnLocation) as Pawn, promotionTarget);
		Debug.Log($"[Client] Executed pawn promotion to {promotionTarget} on {pawnLocation}");
	}

	private ClientRpcParams GetOtherPlayerTarget(ServerRpcParams senderParams) {
		ulong serverClientID = NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId;
		bool isSenderServer = senderParams.Receive.SenderClientId == serverClientID;
		ulong opponentID = OpponentPlayer.OwnerClientId; // verify change
		return new ClientRpcParams {
			Send = new ClientRpcSendParams {
				TargetClientIds = new ulong[] {
					isSenderServer ? opponentID : serverClientID,
				}
			}
		};
	}
}
