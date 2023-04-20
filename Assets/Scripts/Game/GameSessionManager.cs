using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSessionManager : NetworkBehaviour {
	public static GameSessionManager Instance { get; private set; }
	void Awake() {
		if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
	}

	public override void OnDestroy() {
		Instance = null;
		base.OnDestroy();
	}

	public NetworkVariable<ulong> WhitePlayerID = new();
	public NetworkVariable<ulong> BlackPlayerID = new();

	public FENGameState OfficialFENGameState;
	public bool GameRunning = false;
	public Player LocalPlayer = null;
	public Player OpponentPlayer = null;
	public bool MyTurn => LocalPlayer.PlayerColor == OfficialFENGameState.IsWhiteTurn;

	[SerializeField] private GameObject ClassicPlayerObject;
	[SerializeField] private string fenStartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
	private BoardManager boardManager;
	private ClassicGameLogicManager gameLogicManager;

	public override void OnNetworkSpawn() {
		NetworkManager.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
	}

	public override void OnNetworkDespawn() {
		NetworkManager.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
	}

	private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut) {
		SetCachedInstanceVariables();
		if (IsServer)
			InitializeGame();
	}

	private void SetCachedInstanceVariables() {
		boardManager = BoardManager.Instance;
		gameLogicManager = ClassicGameLogicManager.Instance;
		boardManager.CacheInstanceVariables();
		gameLogicManager.CacheInstanceVarables();
	}

	public void InitializeGame() {
		InitializeGameServer();
		InitializeGameClientRPC(fenStartingPosition, WhitePlayerID.Value, BlackPlayerID.Value);
	}

	private void InitializeGameServer() {
		var connectedPlayers = NetworkManager.Singleton.ConnectedClientsIds;
		WhitePlayerID.Value = connectedPlayers[Random.Range(0, connectedPlayers.Count)];
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
		GameRunning = true;
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
		OfficialFENGameState.HalfMoveClock = ClassicGameMoveLogger.Instance.MovesSinceLastCaptureOrPawnMove;
		if (OfficialFENGameState.IsWhiteTurn) OfficialFENGameState.FullMoveNumber++;
		if (MyTurn) {
			Debug.Log("[Turn Change] My Turn!");
			OnMyTurn();
		} else {
			Debug.Log("[Turn Change] Opponent's turn!");
		}
	}

	private void OnMyTurn() {
		var numberOfLegalMoves = gameLogicManager.MyNumberOfLegalMoves;
		Debug.Log($"My number of legal moves: {numberOfLegalMoves}");
		LocalPlayer.OnMyTurn();
	}

	public void OnCheck(bool checkedKingColor) {
		if (checkedKingColor == LocalPlayer.PlayerColor) {
			Debug.Log("[Notification] My king is in check!");
		} else {
			Debug.Log("[Notification] Opponent's king is in check!");
		}
	}

	public void OnCheckmate(bool checkmatedKingColor) {
		if (checkmatedKingColor == LocalPlayer.PlayerColor) {
			Debug.Log("[Notification] My king is checkmated!");
			EndGameRequestServerRPC(true);
		} else {
			Debug.Log("[Notification] Opponent's king is checkmated!");
		}
	}

	public void OnStalemate() {
		Debug.Log("[Notification] Stalemate!");
		EndGameRequestServerRPC(false);
	}

	[ServerRpc(RequireOwnership = false)]
	private void EndGameRequestServerRPC(bool kingCheckmated, ServerRpcParams serverRpcParams = default) {
		if (kingCheckmated) {
			EndGameClientRPC(true, serverRpcParams.Receive.SenderClientId == WhitePlayerID.Value ? BlackPlayerID.Value : WhitePlayerID.Value);
			Debug.Log("[Game Status] Game end by checkmate!");
			Debug.Log("Winner: " + (serverRpcParams.Receive.SenderClientId == WhitePlayerID.Value ? "Black player" : "White player"));
		} else {
			Debug.Log("[Game Status] Game end by stalemate!");
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
		GameRunning = false;
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
	public void RelayPawnPromotionServerRPC(Vector2Int pawnLocation, PieceType promotionTarget, ServerRpcParams param = default) {
		PromotePawnClientRPC(pawnLocation, promotionTarget, GetOtherPlayerTarget(param));
		Debug.Log($"[Server] Relayed pawn promotion to {promotionTarget} on {pawnLocation}");
	}

	[ClientRpc]
	public void PromotePawnClientRPC(Vector2Int pawnLocation, PieceType promotionTarget, ClientRpcParams param = default) {
		gameLogicManager.PromotePawn(boardManager.GetPieceFromSpace(pawnLocation) as Pawn, promotionTarget);
		Debug.Log($"[Client] Executed pawn promotion to {promotionTarget} on {pawnLocation}");
	}

	private ClientRpcParams GetOtherPlayerTarget(ServerRpcParams senderParams) {
		ulong serverClientID = NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId;
		bool isSenderServer = senderParams.Receive.SenderClientId == serverClientID;
		ulong opponentID = OpponentPlayer.OwnerClientId;
		return new ClientRpcParams {
			Send = new ClientRpcSendParams {
				TargetClientIds = new ulong[] {
					isSenderServer ? opponentID : serverClientID,
				}
			}
		};
	}
}
