/// <summary>
/// This class is used to store the game state in FEN format. It does not update automatically and behaves as a wrapper for the state string.
/// </summary>
public class FENGameState {
	public string BoardState;
	public string ActiveColor;
	public string CastlingAvailability;
	public string EnPassantTarget;
	public int HalfMoveClock;
	public int FullMoveNumber;

	public FENGameState(string fenString) {
		string[] fenParts = fenString.Split(' ');
		BoardState = fenParts[0];
		ActiveColor = fenParts[1];
		CastlingAvailability = fenParts[2];
		EnPassantTarget = fenParts[3];
		HalfMoveClock = int.Parse(fenParts[4]);
		FullMoveNumber = int.Parse(fenParts[5]);
	}

	public override string ToString() {
		return $"{BoardState} {ActiveColor} {CastlingAvailability} {EnPassantTarget} {HalfMoveClock} {FullMoveNumber}";
	}

	public bool IsWhiteTurn => ActiveColor == "w";

	public void SetActiveColor(bool isWhiteTurn) {
		ActiveColor = isWhiteTurn ? "w" : "b";
	}
}
