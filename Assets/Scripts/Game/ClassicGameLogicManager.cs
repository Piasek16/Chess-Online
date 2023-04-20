using System.Collections.Generic;
using UnityEngine;

public class ClassicGameLogicManager : MonoBehaviour {
	public static ClassicGameLogicManager Instance { get; private set; }
	private BoardManager boardManager;
	private GameSessionManager gameSessionManager;
	
	[SerializeField] private PromotionPicker promotionPicker;

	void Awake() {
		if (Instance != null && Instance != this)
			Destroy(gameObject);
		else
			Instance = this;
		OnMoveFinished += ClassicGameLogicManager_OnMoveFinished;
	}

	void OnDestroy() {
		Instance = null;
		OnMoveFinished -= ClassicGameLogicManager_OnMoveFinished;
	}

	public void CacheInstanceVarables() {
		boardManager = BoardManager.Instance;
		gameSessionManager = GameSessionManager.Instance;
	}

	private Piece movingPiece;
	private Piece targetPiece;
	private ClassicGameMove currentMoveData;
	public delegate void MoveFinishedHandler(ClassicGameMove moveData);
	public event MoveFinishedHandler OnMoveFinished;
	public void BeforeMove(Move move) {
		movingPiece = boardManager.GetPieceFromSpace(move.PositionOrigin);
		targetPiece = boardManager.GetPieceFromSpace(move.PositionDestination);
		currentMoveData = new ClassicGameMove(gameSessionManager.OfficialFENGameState.IsWhiteTurn, move, movingPiece);
		if (movingPiece is Pawn && targetPiece is GhostPawn ghost) { // En pessant capture check
			ghost.ExecuteGhost();
			currentMoveData.Action |= ClassicGameMove.SpecialAction.EnPassant;
		}
		if (targetPiece != null) {
			boardManager.DestroyPiece(targetPiece);
			currentMoveData.Action |= ClassicGameMove.SpecialAction.Capture;
		}
	}

	public void AfterMove(Move move) {
		if (movingPiece is IFirstMovable firstMovable) {
			firstMovable.FirstMove = false;
		}
		var enPassantTarget = gameSessionManager.OfficialFENGameState.EnPassantTarget;
		if (enPassantTarget != "-") { // En pessant ghost removal check
			if (boardManager.GetPieceFromSpace(BoardManager.BoardLocationToVector2Int(enPassantTarget)) is GhostPawn ghost) {
				ghost.DisposeOfGhost();
			}
		}
		if (movingPiece is Pawn) {
			bool isPieceWhite = movingPiece.ID > 0;
			if (Vector2Int.Distance(move.PositionOrigin, move.PositionDestination) == 2) { // En pessant generation check
				Vector2Int ghostLocation = new(movingPiece.Position.x, movingPiece.Position.y + (isPieceWhite ? -1 : 1));
				boardManager.SummonGhostPawn(movingPiece.Position, ghostLocation);
				gameSessionManager.OfficialFENGameState.EnPassantTarget = BoardManager.Vector2IntToBoardLocation(ghostLocation);
			}
			if (movingPiece.Position.y == (isPieceWhite ? 7 : 0)) {
				currentMoveData.Action |= ClassicGameMove.SpecialAction.Promotion;
				if (BoardManager.Instance.IsPieceMyColor(movingPiece)) {
					Instantiate(promotionPicker, (Vector3Int)movingPiece.Position, gameSessionManager.LocalPlayer.PlayerColor ? Quaternion.identity : Quaternion.Euler(0, 0, 180));
					gameSessionManager.LocalPlayer.EnterPromotion(currentMoveData);
				}
			}
		}
		if (movingPiece is King && Vector2Int.Distance(move.PositionOrigin, move.PositionDestination) == 2) { // Castling check
			bool castleKingSide = move.PositionDestination.x > move.PositionOrigin.x;
			if (castleKingSide) {
				Piece rook = boardManager.GetPieceFromSpace(new Vector2Int(7, movingPiece.Position.y));
				boardManager.ExecuteMove(new Move(rook.Position, new Vector2Int(5, movingPiece.Position.y)), false);
				(rook as Rook).FirstMove = false;
				currentMoveData.Action |= ClassicGameMove.SpecialAction.CastlingK;
			} else {
				Piece rook = boardManager.GetPieceFromSpace(new Vector2Int(0, movingPiece.Position.y));
				boardManager.ExecuteMove(new Move(rook.Position, new Vector2Int(3, movingPiece.Position.y)), false);
				(rook as Rook).FirstMove = false;
				currentMoveData.Action |= ClassicGameMove.SpecialAction.CastlingQ;
			}
		}
		if (!currentMoveData.Action.HasFlag(ClassicGameMove.SpecialAction.Promotion))
			OnMoveFinished?.Invoke(currentMoveData);
	}

	private void ClassicGameLogicManager_OnMoveFinished(ClassicGameMove moveData) {
		ClassicGameMoveLogger.Instance.RecordMove(moveData);
	}

	public void PromotePawn(Pawn pawn, PieceType pieceType) {
		if (pawn == null) {
			Debug.LogError("Attempted to promote a piece that is not a pawn!");
			return;
		}
		var promotionSpace = pawn.Position;
		boardManager.DestroyPiece(pawn);
		boardManager.SetSpace(promotionSpace, pieceType);
		currentMoveData.PromotedTo = pieceType;
		OnMoveFinished?.Invoke(currentMoveData);
	}

	public int NumberOfLegalMoves {
		get {
			int numberOfLegalMoves = 0;
			var gameBoard = boardManager.board;
			foreach (var space in gameBoard) {
				var piece = boardManager.GetPieceFromSpace(space);
				if (piece != null && boardManager.IsPieceMyColor(piece)) {
					numberOfLegalMoves += piece.PossibleMoves.Count;
				}
			}
			return numberOfLegalMoves;
		}
	}

	public bool IsKingInCheck() {
		var king = BoardManager.Instance.LocalPlayerKing;
		var positionsToCheck = new List<Vector2Int>();
		positionsToCheck.AddRange(MoveGenerator.Instance.GetDiagonalMoves(king.Position));
		positionsToCheck.AddRange(MoveGenerator.Instance.GetVerticalMoves(king.Position));
		positionsToCheck.AddRange(MoveGenerator.Instance.GetKnightMoves(king.Position));
		positionsToCheck.RemoveAll(move => BoardManager.Instance.board[move.x, move.y].transform.childCount <= 0
			|| BoardManager.Instance.board[move.x, move.y].GetComponentInChildren<Piece>().ID * king.ID > 0);
		if (positionsToCheck.Count == 0)
			return false;
		foreach (Vector2Int position in positionsToCheck) {
			var possiblyThreateningPiece = BoardManager.Instance.GetPieceFromSpace(position);
			var possiblePieceMoves = possiblyThreateningPiece.PossibleMoves;
			foreach (var move in possiblePieceMoves) {
				var piece = BoardManager.Instance.GetPieceFromSpace(move);
				if (piece != null && piece is King)
					return true;
			}
		}
		return false;
	}

	public bool IsMoveLegal(Vector2Int oldPosition, Vector2Int newPosition) {
		var oldPiece = BoardManager.Instance.GetPieceFromSpace(newPosition);
		if (oldPiece != null) oldPiece.transform.parent = null;
		Move move = new(oldPosition, newPosition);
		BoardManager.Instance.ExecuteMove(move, false);
		var check = IsKingInCheck();
		BoardManager.Instance.ExecuteMove(move.Reverse, false);
		if (oldPiece != null) oldPiece.transform.parent = BoardManager.Instance.board[newPosition.x, newPosition.y].transform;
		return !check;
	}
}
