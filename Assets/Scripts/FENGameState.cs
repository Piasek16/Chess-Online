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

	public FENGameState() {}

	public override string ToString() {
		return $"{BoardState} {ActiveColor} {CastlingAvailability} {EnPassantTarget} {HalfMoveClock} {FullMoveNumber}";
	}

	public bool IsWhiteTurn => ActiveColor == "w";

	public void SetActiveColor(bool isWhiteTurn) {
		ActiveColor = isWhiteTurn ? "w" : "b";
	}

	public static FENGameState CollectFENState() {
		FENGameState state = new();
		state.BoardState = BoardManager.Instance.GetFENBoardState();
		state.CastlingAvailability = BoardManager.Instance.GetFENCastlingRights();
		state.EnPassantTarget = BoardManager.Instance.GetFENEnPassantTarget();
		state.HalfMoveClock = GameSessionManager.Instance.OfficialFENGameState.HalfMoveClock;
		state.FullMoveNumber = GameSessionManager.Instance.OfficialFENGameState.FullMoveNumber;
		state.SetActiveColor(GameSessionManager.Instance.OfficialFENGameState.IsWhiteTurn);
		return state;
	}
}
