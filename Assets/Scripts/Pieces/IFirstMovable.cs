/// <summary>
/// Interface marking pieces that require a first move check for a special action.
/// This action is regarded as a privilege and therefore should be false by default.
/// </summary>
public interface IFirstMovable {
	bool FirstMove { get; set; }
	void ReinitializeValues();
}
