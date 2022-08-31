using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine.SceneManagement;

public class LoginSessionManager : MonoBehaviour {
    public static LoginSessionManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(this); else { Instance = this; DontDestroyOnLoad(gameObject); };
    }

    public string Username { get; private set; } = "Chess Player";
    public string IP { get; private set; } = "127.0.0.1";

    [SerializeField] private Canvas usernameCanvas;
    [SerializeField] private Canvas connectionCanvas;
    [SerializeField] private LobbyManager lobbyManager;
    private UNetTransport uNetTransport;

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
        Debug.Log("Client disconnected.");
        Debug.Log("Loading starting scene...");
        SceneManager.LoadScene(0);
    }

    public void StartHost() {
        if (NetworkManager.Singleton.StartHost()) {
            Debug.Log("Host started successfully! \nAwaiting connections...");
            connectionCanvas.gameObject.SetActive(false);
            var lobby = Instantiate(lobbyManager);

            //var test = lobby.GetComponent<NetworkObject>();
            //Debug.Log("dwa");


            //test.Spawn();


            Debug.Log("after");


            lobby.GetComponent<NetworkObject>().Spawn();
            Debug.Log("Lobby created!");
        }
    }

    void Start() {
        uNetTransport = NetworkManager.Singleton.GetComponent<UNetTransport>();
        uNetTransport.ConnectAddress = "192.168.1.39"; //Temporary local address
        uNetTransport.ConnectPort = 25565; //Temporary port
        uNetTransport.ServerListenPort = 25565; //Temporary port
    }
}
