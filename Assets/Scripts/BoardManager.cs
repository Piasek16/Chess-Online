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

	public BoardTheme BoardTheme;
	public GameObject[,] board = new GameObject[8, 8];
	public King LocalPlayerKing;
	public enum PieceType : int {
		None = 0,
		WKing = 1,
		WQueen = 2,
		WBishop = 3,
		WKnight = 4,
		WRook = 5,
		WPawn = 6,
		BKing = -1,
		BQueen = -2,
		BBishop = -3,
		BKnight = -4,
		BRook = -5,
		BPawn = -6
	}

	/// <summary>
	/// The instance of the BoardManager singleton.
	/// </summary>
	public static BoardManager Instance { get; private set; }
	void Awake() {
		if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
		defaultShader = Shader.Find("Unlit/Color");
		pieces = new Dictionary<int, Piece>();
		foreach (Piece piece in piecesPrefabs) {
			pieces.Add(piece.ID, piece);
		}
		piecePool = new PiecePool();
		GenerateBoard();
	}

	void Start() {
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
		if (!MoveManager.IsPositionValid(new Vector2Int(positionX, positionY))) {
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
		if (!MoveManager.IsPositionValid(new Vector2Int(positionX, positionY))) {
			Debug.LogWarning("Player tried to get a piece from a non existent position " + new Vector2Int(positionX, positionY));
			return null;
		}
		GameObject _space = board[positionX, positionY];
		if (_space.transform.childCount >= 1) return _space.transform.GetComponentInChildren<Piece>();
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
			if (piece is T target) pieces.Add(target);
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

	#region OldMovePiece
	[Obsolete("Use struct format instead")]
	public void MovePiece(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
		var _oldPiece = GetPieceFromSpace(newPiecePosition);
		if (_oldPiece != null) {
			if ((_oldPiece as Pawn)?.IsGhost == true && GetPieceFromSpace(oldPiecePosition).GetType() == typeof(Pawn)) {
				(_oldPiece as Pawn)?.ExecuteGhost();
			}
			_oldPiece.transform.parent = null; //Detach from gameboard to make the piece not show up in search for pieces (Destroy gets executerd later in the frame)
			Destroy(_oldPiece.gameObject);
		}
		var movedPiece = GetPieceFromSpace(oldPiecePosition);
		movedPiece.transform.parent = board[newPiecePosition.x, newPiecePosition.y].transform;
		movedPiece.transform.localPosition = Vector3.zero;
		//Debug.Log("Moved " + movedPiece.name + " from " + oldPiecePosition + " to " + newPiecePosition);
	}
	#endregion OldMovePiece

	public void SummonGhostPawn(Vector2Int parentPawnPosition, Vector2Int ghostLocation) {
		Pawn ghost = piecePool.GetGhost();
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

	#region OldBoardState
	/*public void LogBoardState() {
		Debug.Log("Current Boardstate: " + ExportBoardState());
	}*/

	[Obsolete("Method is obsolete - use fen state instead")]
	public string ExportBoardState() {
		string boardState = string.Empty;
		foreach (GameObject space in board) {
			var piece = GetPieceFromSpace(space);
			boardState += piece?.ID;
			if (piece?.FirstMove == true) boardState += "*";
			boardState += "/";
		}
		return boardState;
	}

	[Obsolete("Method is obsolete - use fen state instead")]
	public void LoadBoardState(string boardStateData) {
		var boardEnumerator = board.GetEnumerator();
		var test = boardStateData.Split('/');
		foreach (string s in boardStateData.Split('/')) {
			if (!boardEnumerator.MoveNext()) break;
			if (string.IsNullOrEmpty(s)) {
				SetSpace((GameObject)boardEnumerator.Current, PieceType.None);
			} else {
				string pieceData = s;
				bool firstMoveStatus = pieceData[^1] == '*';
				if (firstMoveStatus) pieceData = pieceData.TrimEnd('*');
				SetSpace((GameObject)boardEnumerator.Current, (PieceType)int.Parse(pieceData));
				if (!firstMoveStatus) GetPieceFromSpace((GameObject)boardEnumerator.Current).FirstMove = false; //this should be removed (first move is set in game logic manager)
			}
		}
	}
	#endregion OldBoardState

	#region FENState
	private readonly Dictionary<char, PieceType> fenPieces = new Dictionary<char, PieceType>() {
		{ 'K', PieceType.WKing },
		{ 'Q', PieceType.WQueen },
		{ 'B', PieceType.WBishop },
		{ 'N', PieceType.WKnight },
		{ 'R', PieceType.WRook },
		{ 'P', PieceType.WPawn },
		{ 'k', PieceType.BKing },
		{ 'q', PieceType.BQueen },
		{ 'b', PieceType.BBishop },
		{ 'n', PieceType.BKnight },
		{ 'r', PieceType.BRook },
		{ 'p', PieceType.BPawn },
	};

	/// <summary>
	/// Loads the board state(location of all pieces) from a supplied portion of a fen game state string.
	/// Automatically updates local king's for the players and sets first move privileges for pawns if they are at their starting lines.
	/// </summary>
	/// <param name="fenBoardState">A board part(first part) of a string containing a FEN chess game state</param>
	public void LoadBoardStateFromFEN(string fenBoardState) {
		CleanBoard();
		int file = 0, rank = 7;
		foreach (char c in fenBoardState) {
			if (c == '/') {
				file = 0;
				rank--;
			} else {
				if (char.IsDigit(c)) {
					file += (int)char.GetNumericValue(c);
				} else {
					SetSpace(file, rank, fenPieces[c]);
					file++;
				}
			}
		}
		FindAndUpdateKings();
		SetPawnFirstMovePrivileges();
	}

	private void FindAndUpdateKings() {
		List<King> foundKings = FindPiecesOfType<King>();
		kings = foundKings.OrderByDescending(x => x.ID).ToArray();
		LocalPlayerKing = gameSessionManager.LocalPlayer.PlayerColor ? kings[0] : kings[1];
		Debug.Log("Found " + kings.Length + " kings");
		Debug.Log("White king on " + kings[0].Position);
		Debug.Log("Black king on " + kings[1].Position);
	}

	private void SetPawnFirstMovePrivileges() {
		for (int i = 0; i < 8; i++) {
			var pieceFirstRow = GetPieceFromSpace(i, 1);
			if (pieceFirstRow != null) pieceFirstRow.FirstMove = true;
			var pieceSecondRow = GetPieceFromSpace(i, 6);
			if (pieceSecondRow != null) pieceSecondRow.FirstMove = true;
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
					var rook = GetPieceFromSpace(7, 0);
					rook.FirstMove = true;
					kings[0].FirstMove = true;
					break;
				}
				case 'k': {
					var rook = GetPieceFromSpace(7, 7);
					rook.FirstMove = true;
					kings[1].FirstMove = true;
					break;
				}
				case 'Q': {
					var rook = GetPieceFromSpace(0, 0);
					rook.FirstMove = true;
					kings[0].FirstMove = true;
					break;
				}
				case 'q': {
					var rook = GetPieceFromSpace(0, 7);
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
		if (GetPieceFromSpace(possibleParentLocation) == null) possibleParentLocation.y -= 2;
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
		var reversedFenPieces = fenPieces.ToDictionary(piece => piece.Value, piece => piece.Key);
		for (int rank = 7; rank >= 0; rank--) {
			int emptySpaces = 0;
			for (int file = 0; file < 8; file++) {
				var piece = GetPieceFromSpace(file, rank);
				if (piece == null || (piece as Pawn)?.IsGhost == true) {
					emptySpaces++;
				} else {
					if (emptySpaces > 0) fenBoardState += emptySpaces.ToString();
					emptySpaces = 0;
					fenBoardState += reversedFenPieces[(PieceType)piece.ID].ToString();
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
			if ((GetPieceFromSpace(7, 0) as Rook)?.FirstMove == true) {
				fenCastlingRights += "K";
			}
			if ((GetPieceFromSpace(0, 0) as Rook)?.FirstMove == true) {
				fenCastlingRights += "Q";
			}
		}
		if (kings[1].FirstMove) {
			if ((GetPieceFromSpace(7, 7) as Rook)?.FirstMove == true) {
				fenCastlingRights += "k";
			}
			if ((GetPieceFromSpace(0, 7) as Rook)?.FirstMove == true) {
				fenCastlingRights += "q";
			}
		}
		if (string.IsNullOrEmpty(fenCastlingRights)) fenCastlingRights = "-";
		return fenCastlingRights;
	}

	/// <summary>
	/// Saves the en passant target from the board to a fen format.
	/// </summary>
	/// <returns>Square target of EnPassant</returns>
	public string GetFENEnPassantTarget() {
		foreach(var pawn in FindPiecesOfType<Pawn>()) {
			if (pawn.IsGhost) return Vector2IntToBoardLocation(pawn.Position);
		}
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

	public void ReturnGhost(Pawn ghost) {
		piecePool.ReturnGhost(ghost);
	}

	/// <summary>
	/// Nestes class for managing the piece pool.
	/// </summary>
	private class PiecePool {
		private readonly Dictionary<PieceType, Queue<Piece>> pool = new();
		private readonly GameObject poolObject;
		private readonly Vector3Int poolPosition = new(-1, -1, -1);
		private Pawn ghostPawn;

		public PiecePool() {
			poolObject = new GameObject("PiecePool");
			poolObject.transform.parent = Instance.transform;
			poolObject.transform.position = poolPosition;
			foreach (var piece in Instance.piecesPrefabs) {
				pool.Add((PieceType)piece.ID, new Queue<Piece>());
			}
			// Create ghost pawn
			Pawn ghost = Instantiate(Instance.pieces[(int)PieceType.WPawn], poolPosition, Quaternion.identity) as Pawn;
			Destroy(ghost.GetComponent<SpriteRenderer>());
			ghost.transform.parent = poolObject.transform;
			ghost.gameObject.SetActive(false);
			ghostPawn = ghost;
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
			if (piece is Pawn pawn && pawn.IsGhost) {
				ReturnGhost(pawn);
				return;
			}
			piece.gameObject.SetActive(false);
			piece.transform.parent = poolObject.transform;
			piece.transform.position = poolPosition;
			pool[(PieceType)piece.ID].Enqueue(piece);
		}

		public Pawn GetGhost() {
			if (ghostPawn == null)
				throw new Exception("Ghost is already in use! - Return the used ghost before requesting again!");
			var ghost = ghostPawn;
			ghost.gameObject.SetActive(true);
			ghost.transform.parent = null;
			ghostPawn = null;
			return ghost;
		}

		public void ReturnGhost(Pawn ghost) {
			ghost.gameObject.SetActive(false);
			ghost.transform.parent = poolObject.transform;
			ghost.transform.position = poolPosition;
			ghostPawn = ghost;
		}
	}
}
