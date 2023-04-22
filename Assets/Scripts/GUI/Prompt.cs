using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Prompt : MonoBehaviour {
	public UnityEvent OnClick;
	TMP_Text _promptText;
	public string PromptText {
		get => _promptText.text;
		set => _promptText.text = value;
	}

	void Awake() {
		_promptText = transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
	}

	public void TriggerClick() {
		OnClick.Invoke();
	}

	public void DestroyPrompt() {
		Destroy(gameObject);
	}
}
