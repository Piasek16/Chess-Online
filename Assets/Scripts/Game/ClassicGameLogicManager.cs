using UnityEngine;

public class ClassicGameLogicManager : MonoBehaviour {
	public static ClassicGameLogicManager Instance { get; private set; }
	private BoardManager boardManager;
	private GameSessionManager gameSessionManager;

	void Awake() {
		if (Instance != null && Instance != this)
			Destroy(gameObject);
		else
			Instance = this;
	}

	void OnDestroy() {
		Instance = null;
	}

	public void CacheInstanceVarables() {
		boardManager = BoardManager.Instance;
		gameSessionManager = GameSessionManager.Instance;
	}

	private Piece movingPiece;
	private Piece targetPiece;
	private ClassicGameMove currentMoveData;
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
				// TODO: Send local player prompt to choose promotion piece (potentially await or handle choosen piece in another method)
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
		ClassicGameMoveLogger.Instance.RecordMove(currentMoveData);
	}

	public void PromotePawn(Pawn pawn, PieceType pieceType) {
		if (pawn is null) {
			Debug.LogError("Attempted to promote a piece that is null!");
			return;
		}
		var promotionSpace = pawn.Position;
		boardManager.DestroyPiece(pawn);
		boardManager.SetSpace(promotionSpace, pieceType);
	}

	public int NumberOfLegalMoves {
		get {
			int numberOfLegalMoves = 0;
			var gameBoard = boardManager.board;
			foreach (var space in gameBoard) {
				var piece = boardManager.GetPieceFromSpace(space);
				if (piece is not null && boardManager.IsPieceMyColor(piece)) {
					numberOfLegalMoves += piece.PossibleMoves.Count;
				}
			}
			return numberOfLegalMoves;
		}
	}
}
