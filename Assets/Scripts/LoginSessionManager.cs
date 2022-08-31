using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine.SceneManagement;

public class LoginSessionManager : MonoBehaviour {
    public static LoginSessionManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else { Instance = this; DontDestroyOnLoad(gameObject); };
    }

    public string Username { get; private set; } = "Chess Player";
    public string IP { get; private set; } = "127.0.0.1";

    [SerializeField] private Canvas usernameCanvas;
    [SerializeField] private Canvas connectionCanvas;
    [SerializeField] private LobbyManager lobbyManagerPrefab;
    private UNetTransport uNetTransport;

    void Start() {
        uNetTransport = NetworkManager.Singleton.GetComponent<UNetTransport>();
        uNetTransport.ConnectPort = 25565; //Temporary port
        uNetTransport.ServerListenPort = 25565; //Temporary port
    }

    public void PassUsername(string username) {
        Username = username;
    }

    public void AdvanceLoginCanvas() {
        usernameCanvas.gameObject.SetActive(false);
        connectionCanvas.gameObject.SetActive(true);
    }

    public void PassIP(string ip) {
        IP = ip;
        uNetTransport.ConnectAddress = IP;
    }

    public void ConnectClient() {
        if (NetworkManager.Singleton.StartClient()) {
            Debug.Log("Client started successfully!");
            Debug.Log("Attempting to connect with " + IP);
        }
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
    }

    private void Singleton_OnClientConnectedCallback(ulong obj) {
        Debug.Log("Client successfully connected!");
        connectionCanvas.gameObject.SetActive(false);
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj) {
        Debug.Log("Client disconnected. (Or connection attempt timed out)");
        Debug.Log("Loading starting scene... (this will end with a bricked game because of manual references)");
        SceneManager.LoadScene(0);
    }

    public void StartHost() {
        if (NetworkManager.Singleton.StartHost()) {
            Debug.Log("Host started successfully! \nAwaiting connections...");
            connectionCanvas.gameObject.SetActive(false);
            var lobby = Instantiate(lobbyManagerPrefab);
            lobby.GetComponent<NetworkObject>().Spawn();
            Debug.Log("Lobby created!");
        }
    }
}
