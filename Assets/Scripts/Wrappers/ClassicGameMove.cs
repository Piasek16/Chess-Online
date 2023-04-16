using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

public class ClassicGameMove {
	public int MoveNumber;
	public bool ByWhitePlayer;
	public Move Move;
	public Piece MovingPiece;
	public SpecialAction Action;
	public PieceType PromotedTo;
	private readonly AmbiguityLevel _ambiguity;

	[Flags]
	public enum SpecialAction {
		None = 0,
		Capture = 1,
		EnPassant = 2,
		CastlingK = 4,
		CastlingQ = 8,
		Promotion = 16,
		Check = 32,
		Checkmate = 64
	}

	private enum AmbiguityLevel {
		None,
		File,
		Rank,
		Both
	}

	public ClassicGameMove(bool byWhitePlayer, Move move, Piece movingPiece) {
		ByWhitePlayer = byWhitePlayer;
		Move = move;
		MovingPiece = movingPiece;
		_ambiguity = DetermineAmbiguity();
	}

	/// <summary>
	/// Determines the ambiguity level of the move.<br/>
	/// This method should be called while the move is still being made, before the move is actually made in order to ensure correct piece placement during the check.
	/// </summary>
	private AmbiguityLevel DetermineAmbiguity() {
		var identicalPieces = BoardManager.Instance.FindPiecesOfType<Piece>().Where(piece => piece.ID == MovingPiece.ID).ToList();
		var piecesOnDestination = new List<Piece>();
		foreach (var piece in identicalPieces) {
			if (piece == MovingPiece)
				continue;
			if (piece.PossibleMoves.Contains(Move.PositionDestination))
				piecesOnDestination.Add(piece);
		}
		if (piecesOnDestination.Count == 0)
			return AmbiguityLevel.None;
		var piecesOnFile = new List<Piece>();
		var piecesOnRank = new List<Piece>();
		foreach (var piece in piecesOnDestination) {
			if (piece.Position.x == MovingPiece.Position.x)
				piecesOnFile.Add(piece);
			if (piece.Position.y == MovingPiece.Position.y)
				piecesOnRank.Add(piece);
		}
		if (piecesOnFile.Count == 0 && piecesOnRank.Count == 0)
			return AmbiguityLevel.None;
		if (piecesOnFile.Count > 0 && piecesOnRank.Count > 0)
			return AmbiguityLevel.Both;
		if (piecesOnFile.Count > 0)
			return AmbiguityLevel.File;
		return AmbiguityLevel.Rank;
	}

	public override string ToString() {
		if (Action.HasFlag(SpecialAction.CastlingK) || Action.HasFlag(SpecialAction.CastlingQ)) {
			return Action.HasFlag(SpecialAction.CastlingK) ? "O-O" : "O-O-O";
		}
		StringBuilder stringBuilder = new();
		if (MovingPiece is Pawn && Action.HasFlag(SpecialAction.Capture))
			stringBuilder.Append(BoardManager.Vector2IntToBoardLocation(Move.PositionOrigin)[0]);
		if (MovingPiece is not Pawn) { // Pawn moves, except captures handled above, are not ambiguous
			stringBuilder.Append(char.ToUpper(MovingPiece.Symbol));
			switch (_ambiguity) {
				case AmbiguityLevel.None:
					break;
				case AmbiguityLevel.File:
					stringBuilder.Append(BoardManager.Vector2IntToBoardLocation(Move.PositionOrigin)[1]);
					break;
				case AmbiguityLevel.Rank:
					stringBuilder.Append(BoardManager.Vector2IntToBoardLocation(Move.PositionOrigin)[0]);
					break;
				case AmbiguityLevel.Both:
					stringBuilder.Append(BoardManager.Vector2IntToBoardLocation(Move.PositionOrigin));
					break;
			}
		}
		if (Action.HasFlag(SpecialAction.Capture))
			stringBuilder.Append('x');
		stringBuilder.Append(BoardManager.Vector2IntToBoardLocation(Move.PositionDestination));
		if (Action.HasFlag(SpecialAction.EnPassant))
			stringBuilder.Append("e.p.");
		if (Action.HasFlag(SpecialAction.Promotion))
			stringBuilder.Append('=').Append(char.ToUpper(BoardManager.PieceTypeToSymbol[PromotedTo]));
		if (Action.HasFlag(SpecialAction.Checkmate))
			stringBuilder.Append('#');
		else if (Action.HasFlag(SpecialAction.Check))
			stringBuilder.Append('+');
		return stringBuilder.ToString();
	}
}