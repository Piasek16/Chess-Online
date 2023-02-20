using Unity.Netcode;
using UnityEngine;

public readonly struct Move {
	public readonly Vector2Int PositionOrigin;
	public readonly Vector2Int PositionDestination;
	// In consideration
	// public readonly Piece MovingPiece;

	public Move(Vector2Int positionOrigin, Vector2Int positionDestination) {
		PositionOrigin = positionOrigin;
		PositionDestination = positionDestination;
		// MovingPiece = BoardManager.Instance.GetPieceFromSpace(positionOrigin);
	}
}

public static class MoveSerializationExtensions {
	public static void ReadValueSafe(this FastBufferReader reader, out Move move) {
		reader.ReadValueSafe(out Vector2Int positionOrigin);
		reader.ReadValueSafe(out Vector2Int positionDestination);
		move = new Move(positionOrigin, positionDestination);
	}

	public static void WriteValueSafe(this FastBufferWriter writer, Move move) {
		writer.WriteValueSafe(move.PositionOrigin);
		writer.WriteValueSafe(move.PositionDestination);
	}
}