#if UNITY_EDITOR
using ParrelSync;
using Unity.Netcode;
#endif
using UnityEngine;

public class GameTestingManager : MonoBehaviour {
    public static GameTestingManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
    }
    public string TestingUsername = "PiasekTester";
#if UNITY_EDITOR

	string testingOriginalUsername = "PiasekHost";
    string testingCloneUsername = "PiasekClient";
    void Start() {
        if (ClonesManager.IsClone()) {
            TestingUsername = testingCloneUsername;
            NetworkManager.Singleton.StartClient();
        } else {
            TestingUsername = testingOriginalUsername;
            NetworkManager.Singleton.StartHost();
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            GameSessionManager.Instance.InitializeTestGame();
        }
    }
#endif
}
