using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// <para>Singleton class for game board management.</para>
/// <para>This includes: board generation, piece position management, applying board colors as well as numeric to chess location conversions.</para>
/// </summary>
public class BoardManager : MonoBehaviour {
	[SerializeField] private Piece[] piecesPrefabs;
	private Shader defaultShader;
	private Dictionary<int, Piece> pieces;
	private King[] kings;
	private static readonly string files = "abcdefgh"; //file is abcdefgh rank is 1-8
	private GameSessionManager gameSessionManager;
	private ClassicGameLogicManager gameLogicManager;
	private PiecePool piecePool;
	[SerializeField] AudioClip movePieceClip;
	[SerializeField] AudioClip capturePieceClip;
	public static Dictionary<char, PieceType> SymbolToPieceType;
	public static Dictionary<PieceType, char> PieceTypeToSymbol;

	public BoardTheme BoardTheme;
	public GameObject[,] board = new GameObject[8, 8];
	/// <summary>
	/// An array containinng the two kings of the game. White King is at index 0, Black King is at index 1.
	/// </summary>
	public King[] Kings => kings;

	/// <summary>
	/// The instance of the BoardManager singleton.
	/// </summary>
	public static BoardManager Instance { get; private set; }
	void Awake() {
		if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
		defaultShader = Shader.Find("Unlit/Color");
		pieces = new Dictionary<int, Piece>();
		SymbolToPieceType = new Dictionary<char, PieceType>();
		foreach (Piece piece in piecesPrefabs) {
			pieces.Add(piece.ID, piece);
			SymbolToPieceType.Add(piece.Symbol, piece.Type);
		}
		PieceTypeToSymbol = SymbolToPieceType.ToDictionary(x => x.Value, x => x.Key);
		piecePool = new PiecePool();
		GenerateBoard();
		ClassicGameLogicManager.Instance.OnMoveFinished += Instance_OnMoveFinished;
	}

	private void Instance_OnMoveFinished(ClassicGameMove moveData) {
		if (moveData.Action.HasFlag(ClassicGameMove.SpecialAction.Capture)) {
			AudioSource.PlayClipAtPoint(capturePieceClip, Camera.main.transform.position);
		} else {
			AudioSource.PlayClipAtPoint(movePieceClip, Camera.main.transform.position);
		}
	}

	void OnDestroy() {
		Instance = null;
	}

	public void CacheInstanceVariables() {
		gameSessionManager = GameSessionManager.Instance;
		gameLogicManager = ClassicGameLogicManager.Instance;
	}

	void GenerateBoard() {
		for (int i = 0; i < 8; i++) {
			if (i % 2 == 0) {
				for (int j = 0; j < 8; j++) {
					board[i, j] = CreateSpace(i, j, j % 2 == 1);
				}
			} else {
				for (int j = 0; j < 8; j++) {
					board[i, j] = CreateSpace(i, j, j % 2 == 0);
				}
			}
		}
	}

	/// <summary>
	/// Cleans the board by setting all spaces to "None", which detaches the piece from the board and destroys it.
	/// </summary>
	public void CleanBoard() {
		foreach (GameObject space in board) {
			SetSpace(space, PieceType.None);
		}
	}

	GameObject CreateSpace(int file, int rank, bool color) {
		var newSpace = GameObject.CreatePrimitive(PrimitiveType.Quad);
		newSpace.transform.position = new Vector3Int(file, rank, 0);
		newSpace.transform.parent = transform;
		newSpace.name = files[file] + (rank + 1).ToString();
		newSpace.GetComponent<MeshRenderer>().material = new Material(defaultShader) {
			color = color ? BoardTheme.WhiteColor : BoardTheme.BlackColor
		};
		return newSpace;
	}

	public void SetSpace(GameObject space, PieceType p) {
		SetSpace((int)space.transform.position.x, (int)space.transform.position.y, p);
	}

	public void SetSpace(string location, PieceType p) {
		if (location.Length > 2) { Debug.LogError("Wrong Piece Format"); return; }
		SetSpace(files.IndexOf(location[0]), location[1] - '0' - 1, p);
	}

	public void SetSpace(char file, int rank, PieceType p) {
		SetSpace(files.IndexOf(file), rank - 1, p);
	}

	public void SetSpace(Vector2Int location, PieceType p) {
		SetSpace(location.x, location.y, p);
	}

	/// <summary>
	/// Sets the space at the given position to the given piece type. If the piece type is "None", the current piece is detached from the board and destroyed, otherwise a piece is only added.
	/// </summary>
	/// <param name="positionX">Horizontal space position</param>
	/// <param name="positionY">Vertical space position</param>
	/// <param name="p">Enum describing the piece type to set</param>
	public void SetSpace(int positionX, int positionY, PieceType p) {
		if (!MoveGenerator.IsPositionValid(new Vector2Int(positionX, positionY))) {
			Debug.LogWarning($"Tried to set an invalid space! Supplied location: X:{positionX}, Y:{positionY}");
			return;
		}
		if (p == PieceType.None) {
			var piece = GetPieceFromSpace(positionX, positionY);
			DestroyPiece(piece);
			return;
		}
		var newPiece = piecePool.GetPiece(p);
		newPiece.transform.parent = board[positionX, positionY].transform;
		newPiece.transform.rotation = gameSessionManager.LocalPlayer.PlayerColor ? Quaternion.identity : Quaternion.Euler(0, 0, 180);
		newPiece.transform.localPosition = Vector3.zero;
	}

	/// <summary>
	/// Returns the piece at the given position on the board.
	/// </summary>
	/// <param name="positionX">Horizontal space position</param>
	/// <param name="positionY">Vertical space position</param>
	/// <returns>A reference to a piece at the chosen position</returns>
	public Piece GetPieceFromSpace(int positionX, int positionY) {
		if (!MoveGenerator.IsPositionValid(new Vector2Int(positionX, positionY))) {
			Debug.LogWarning("Player tried to get a piece from a non existent position " + new Vector2Int(positionX, positionY));
			return null;
		}
		GameObject space = board[positionX, positionY];
		if (space.transform.childCount >= 1) return space.transform.GetComponentInChildren<Piece>();
		return null;
	}

	public Piece GetPieceFromSpace(char file, int rank) {
		return GetPieceFromSpace(files.IndexOf(file), rank - 1);
	}

	public Piece GetPieceFromSpace(Vector2Int position) {
		return GetPieceFromSpace(position.x, position.y);
	}

	public Piece GetPieceFromSpace(string position) {
		if (position.Length > 2) { Debug.LogError("Wrong Piece Format"); return null; }
		return GetPieceFromSpace(files.IndexOf(position[0]), position[1] - '0' - 1);
	}

	public Piece GetPieceFromSpace(GameObject space) {
		return GetPieceFromSpace((int)space.transform.position.x, (int)space.transform.position.y);
	}

	/// <summary>
	/// Returns a list of all pieces on the board of the given type.
	/// </summary>
	/// <typeparam name="T">Type of piece to find</typeparam>
	/// <returns>List of all pieces found on the board</returns>
	public List<T> FindPiecesOfType<T>() where T : Piece {
		List<T> pieces = new List<T>();
		foreach (var tile in board) {
			var piece = GetPieceFromSpace(tile);
			if (piece is T target)
				pieces.Add(target);
		}
		return pieces;
	}

	/// <summary>
	/// Executes a <see cref="Move"/> on the board. That is to say, it moves the piece from the origin to the destination,
	/// while calling the game logic manager to handle the resulting changes.
	/// </summary>
	/// <param name="move">Move to execute</param>
	public void ExecuteMove(Move move, bool executeLogic = true) {
		if (executeLogic) gameLogicManager.BeforeMove(move);
		var movingPiece = GetPieceFromSpace(move.PositionOrigin);
		movingPiece.transform.parent = board[move.PositionDestination.x, move.PositionDestination.y].transform;
		movingPiece.transform.localPosition = Vector3.zero;
		if (executeLogic) gameLogicManager.AfterMove(move);
	}

	public void SummonGhostPawn(Vector2Int parentPawnPosition, Vector2Int ghostLocation) {
		GhostPawn ghost = piecePool.GetGhost();
		ghost.transform.parent = board[ghostLocation.x, ghostLocation.y].transform;
		ghost.transform.localPosition = Vector3.zero;
		ghost.InitGhost(parentPawnPosition);
	}

	public void DestroyPieceAt(Vector2Int position) {
		var piece = GetPieceFromSpace(position);
		DestroyPiece(piece);
	}

	/// <summary>
	/// Detaches the given piece from the game board and destroys it.
	/// </summary>
	/// <param name="piece">Piece to destroy</param>
	public void DestroyPiece(Piece piece) {
		if (piece != null) {
			piece.transform.parent = null;
			piecePool.ReturnPiece(piece);
		}
	}

	Move currentlyHighlightedMove;
	/// <summary>
	/// Highlights the given <see cref="Move"/> on the board.
	/// </summary>
	/// <param name="move">Move spaces to highlight</param>
	public void HighlightMove(Move move) {
		RestoreTileColor(currentlyHighlightedMove.PositionOrigin);
		RestoreTileColor(currentlyHighlightedMove.PositionDestination);
		currentlyHighlightedMove = move;
		HighlightTile(currentlyHighlightedMove.PositionOrigin);
		HighlightTile(currentlyHighlightedMove.PositionDestination);
	}

	/// <summary>
	/// Sets the tile color to the default color, with respect to the checkerboard pattern.
	/// </summary>
	/// <param name="tileLocation">Position of the tile</param>
	public void RestoreTileColor(Vector2Int tileLocation) {
		SetTileColor(tileLocation, BoardTheme.WhiteColor, BoardTheme.BlackColor);
	}

	public void RestoreTileColor(GameObject tile) {
		SetTileColor(new Vector2Int((int)tile.transform.position.x, (int)tile.transform.position.y), BoardTheme.WhiteColor, BoardTheme.BlackColor);
	}

	public void HighlightTile(GameObject tile) {
		SetTileColor(new Vector2Int((int)tile.transform.position.x, (int)tile.transform.position.y), BoardTheme.WhiteHighlightColor, BoardTheme.BlackHighlightColor);
	}

	public void HighlightTile(Vector2Int tile) {
		SetTileColor(tile, BoardTheme.WhiteHighlightColor, BoardTheme.BlackHighlightColor);
	}

	/// <summary>
	/// Sets the color of the tile at the given location. Requires the colors for the white and black tiles.
	/// </summary>
	/// <param name="tileLocation">Position of the tile</param>
	/// <param name="colorWhite">Color for white tiles</param>
	/// <param name="colorBlack">Color for black tiles</param>
	public void SetTileColor(Vector2Int tileLocation, Color colorWhite, Color colorBlack) {
		if (tileLocation.x % 2 == 0) {
			if (tileLocation.y % 2 == 0) {
				board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorBlack;
			} else {
				board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorWhite;
			}
		} else {
			if (tileLocation.y % 2 == 0) {
				board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorWhite;
			} else {
				board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorBlack;
			}
		}
	}

	#region FENState
	/// <summary>
	/// Loads the board state(location of all pieces) from a supplied portion of a fen game state string.
	/// Automatically updates local king's for the players and sets first move privileges for pawns if they are at their starting lines.
	/// </summary>
	/// <param name="fenBoardState">A board part(first part) of a string containing a FEN chess game state</param>
	public void LoadBoardStateFromFEN(string fenBoardState) {
		CleanBoard();
		Debug.Log($"Loading boardstate: {fenBoardState}");
		int file = 0, rank = 7;
		foreach (char c in fenBoardState) {
			if (c == '/') {
				file = 0;
				rank--;
			} else {
				if (char.IsDigit(c)) {
					file += (int)char.GetNumericValue(c);
				} else {
					SetSpace(file, rank, SymbolToPieceType[c]);
					file++;
				}
			}
		}
		FindAndUpdateKings();
		SetPawnFirstMovePrivileges();
		Debug.Log("Boardstate loaded, kings updated and pawn privileges set!");
	}

	private void FindAndUpdateKings() {
		List<King> foundKings = FindPiecesOfType<King>();
		kings = foundKings.OrderByDescending(x => x.ID).ToArray();
		Debug.Log("Found " + kings.Length + " kings");
		Debug.Log("White king on " + kings[0].Position);
		Debug.Log("Black king on " + kings[1].Position);
	}

	private void SetPawnFirstMovePrivileges() {
		for (int i = 0; i < 8; i++) {
			var pieceFirstRow = GetPieceFromSpace(i, 1);
			if (pieceFirstRow is Pawn pawnFirstRow)
				pawnFirstRow.FirstMove = true;
			var pieceSecondRow = GetPieceFromSpace(i, 6);
			if (pieceSecondRow is Pawn pawnSecondRow)
				pawnSecondRow.FirstMove = true;
		}
	}

	/// <summary>
	/// Applies castling rights from a supplied portion of a fen game state string.
	/// </summary>
	/// <param name="fenCastlingRights">A castling rights part of a string containing a FEN chess game state</param>
	public void LoadCastlingRightsFromFEN(string fenCastlingRights) {
		if (fenCastlingRights == null || fenCastlingRights == "-") return;
		Debug.Log("Setting castling rights: " + fenCastlingRights);
		Debug.Log("White king pos: " + kings[0].Position);
		Debug.Log("Black king pos: " + kings[1].Position);
		foreach (char c in fenCastlingRights) {
			switch (c) {
				case 'K': {
					var rook = GetPieceFromSpace(7, 0) as Rook;
					rook.FirstMove = true;
					kings[0].FirstMove = true;
					break;
				}
				case 'k': {
					var rook = GetPieceFromSpace(7, 7) as Rook;
					rook.FirstMove = true;
					kings[1].FirstMove = true;
					break;
				}
				case 'Q': {
					var rook = GetPieceFromSpace(0, 0) as Rook;
					rook.FirstMove = true;
					kings[0].FirstMove = true;
					break;
				}
				case 'q': {
					var rook = GetPieceFromSpace(0, 7) as Rook;
					rook.FirstMove = true;
					kings[1].FirstMove = true;
					break;
				}
			}
		}
	}

	public void LoadEnPassantTargetFromFEN(string fenEnPassantTarget) {
		if (fenEnPassantTarget == null || fenEnPassantTarget == "-") return;
		Debug.Log("Loading en passant target: " + fenEnPassantTarget);
		var enPassantTarget = BoardLocationToVector2Int(fenEnPassantTarget);
		var possibleParentLocation = new Vector2Int(enPassantTarget.x, enPassantTarget.y + 1);
		if (GetPieceFromSpace(possibleParentLocation) == null)
			possibleParentLocation.y -= 2;
		SummonGhostPawn(possibleParentLocation, enPassantTarget);
	}

	/// <summary>
	/// Saves the board state to a fen format
	/// </summary>
	/// <returns>
	/// <para>A string containing the board state of the game</para>
	/// <para>The string is the first part of a whole fen game state string</para>
	/// </returns>
	public string GetFENBoardState() {
		string fenBoardState = string.Empty;
		for (int rank = 7; rank >= 0; rank--) {
			int emptySpaces = 0;
			for (int file = 0; file < 8; file++) {
				var piece = GetPieceFromSpace(file, rank);
				if (piece == null || piece is GhostPawn) {
					emptySpaces++;
				} else {
					if (emptySpaces > 0) fenBoardState += emptySpaces.ToString();
					emptySpaces = 0;
					fenBoardState += piece.Symbol;
				}
			}
			if (emptySpaces > 0) fenBoardState += emptySpaces.ToString();
			if (rank != 0) fenBoardState += "/";
		}
		return fenBoardState;
	}

	/// <summary>
	/// Saves the castling rights from the board to a fen format
	/// </summary>
	/// <returns>
	/// <para>A string containing castling rights data</para>
	/// <para>The string is only a select part of a whole fen game state string</para>
	/// </returns>
	public string GetFENCastlingRights() {
		string fenCastlingRights = string.Empty;
		if (kings[0].FirstMove) {
			if (GetPieceFromSpace(7, 0) is Rook and { FirstMove: true })
				fenCastlingRights += "K";
			if (GetPieceFromSpace(0, 0) is Rook and { FirstMove: true })
				fenCastlingRights += "Q";
		}
		if (kings[1].FirstMove) {
			if (GetPieceFromSpace(7, 7) is Rook and { FirstMove: true })
				fenCastlingRights += "k";
			if (GetPieceFromSpace(0, 7) is Rook and { FirstMove: true })
				fenCastlingRights += "q";
		}
		if (string.IsNullOrEmpty(fenCastlingRights))
			fenCastlingRights = "-";
		return fenCastlingRights;
	}

	/// <summary>
	/// Saves the en passant target from the board to a fen format.
	/// </summary>
	/// <returns>Square target of EnPassant</returns>
	public string GetFENEnPassantTarget() {
		var ghostPawns = FindPiecesOfType<GhostPawn>();
		if (ghostPawns.Count > 0)
			return Vector2IntToBoardLocation(ghostPawns[0].Position);
		return "-";
	}
	#endregion FENState

	/// <summary>
	/// Converts a Vector2Int to a board location string in the format of "a1" or "h8".
	/// </summary>
	/// <param name="vector2Int">Location on the board</param>
	/// <returns>A 2 character string with the location in chess format</returns>
	/// <exception cref="Exception">Gets thrown if a given location is out of board bounds</exception>
	public static string Vector2IntToBoardLocation(Vector2Int vector2Int) {
		if (vector2Int.x < 0 || vector2Int.x > 7 || vector2Int.y < 0 || vector2Int.y > 7)
			throw new Exception("Invalid board location was passed to location converter! (Position parameter out of bounds)");
		string location = string.Empty;
		location += files[vector2Int.x];
		location += vector2Int.y + 1;
		return location;
	}

	/// <summary>
	/// Converts a board location string in the format of "a1" or "h8" to a Vector2Int.
	/// </summary>
	/// <param name="boardLocation">A 2 character string with the location in chess format</param>
	/// <returns>Location on the board as a <see cref="Vector2Int"/></returns>
	/// <exception cref="Exception">Gets thrown if invalid board location was passed (length was above 2 characters) or a given location is out of board bounds</exception>
	public static Vector2Int BoardLocationToVector2Int(string boardLocation) {
		if (boardLocation.Length > 2)
			throw new Exception("Invalid board location was passed to location converter!");
		Vector2Int vector2Int = new Vector2Int();
		vector2Int.x = files.IndexOf(boardLocation[0]);
		vector2Int.y = boardLocation[1] - '0' - 1;
		if (vector2Int.x < 0 || vector2Int.x > 7 || vector2Int.y < 0 || vector2Int.y > 7)
			throw new Exception("Invalid board location was passed to location converter! (Position parameter out of bounds)");
		return vector2Int;
	}

	/// <summary>
	/// Checks if a given piece is the same color as the local player.
	/// </summary>
	/// <param name="piece">Piece to check</param>
	/// <returns>True if piece is of the same color, false otherwise</returns>
	public bool IsPieceMyColor(Piece piece) {
		return piece.ID * (gameSessionManager.LocalPlayer.PlayerColor ? 1 : -1) > 0;
	}

	public void ReturnGhost(GhostPawn ghost) {
		piecePool.ReturnGhost(ghost);
	}

	/// <summary>
	/// Nestes class for managing the piece pool.
	/// </summary>
	private class PiecePool {
		private readonly Dictionary<PieceType, Queue<Piece>> pool = new();
		private readonly GameObject poolObject;
		private readonly Vector3Int poolPosition = new(-1, -1, -1);
		private GhostPawn ghostPawn;

		public PiecePool() {
			poolObject = new GameObject("PiecePool");
			poolObject.transform.parent = Instance.transform;
			poolObject.transform.position = poolPosition;
			foreach (var piece in Instance.piecesPrefabs) {
				pool.Add((PieceType)piece.ID, new Queue<Piece>());
			}
			ghostPawn = CreateGhostPawn();
		}

		private GhostPawn CreateGhostPawn() {
			GameObject ghost = new("GhostPawn");
			ghost.transform.position = poolPosition;
			ghost.transform.parent = poolObject.transform;
			ghost.SetActive(false);
			return ghost.AddComponent<GhostPawn>();
		}

		/// <summary>
		/// Gets a piece from the pool or creates a new one if the pool is empty.
		/// </summary>
		/// <param name="pieceType">Type of piece to receive</param>
		/// <returns>A reference to the requested piece</returns>
		public Piece GetPiece(PieceType pieceType) {
			if (pool[pieceType].Count > 0) {
				Piece piece = pool[pieceType].Dequeue();
				piece.transform.parent = null;
				piece.gameObject.SetActive(true);
				return piece;
			} else {
				Piece newPiece = Instantiate(Instance.pieces[(int)pieceType], poolPosition, Quaternion.identity);
				return newPiece;
			}
		}

		/// <summary>
		/// Returns a piece to the pool.
		/// </summary>
		/// <param name="piece">Piece to return</param>
		public void ReturnPiece(Piece piece) {
			if (piece is IFirstMovable firstMovable) {
				firstMovable.ReinitializeValues();
			}
			if (piece is GhostPawn ghost) {
				ReturnGhost(ghost);
				return;
			}
			piece.gameObject.SetActive(false);
			piece.transform.parent = poolObject.transform;
			piece.transform.position = poolPosition;
			pool[(PieceType)piece.ID].Enqueue(piece);
		}

		public GhostPawn GetGhost() {
			if (ghostPawn == null)
				throw new Exception("Ghost is already in use! - Return the currently used ghost before requesting!");
			var ghost = ghostPawn;
			ghost.gameObject.SetActive(true);
			ghost.transform.parent = null;
			ghostPawn = null;
			return ghost;
		}

		public void ReturnGhost(GhostPawn ghost) {
			ghost.gameObject.SetActive(false);
			ghost.transform.parent = poolObject.transform;
			ghost.transform.position = poolPosition;
			ghostPawn = ghost;
		}
	}
}
