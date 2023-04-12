using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Relay.Models;
using System;
using System.Linq;
using Unity.Services.Relay;
using TMPro;
using System.Net.Sockets;
using System.Net;

public class LoginSessionManager : MonoBehaviour {
    public static LoginSessionManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else { Instance = this; DontDestroyOnLoad(gameObject); };
    }

    public string Username { get; private set; } = "Chess Player";
	public string RelayCode { get; private set; } = null;
	public string IP { get; private set; } = null;
	public ushort Port { get; private set; } = 25565;
	public LobbyManager LobbyManager { get; set; } = null;

	[SerializeField] private LobbyManager lobbyManagerPrefab;
    private GameObject usernameCanvas;
    private GameObject connectionCanvas;
    private TMP_Text promptField;
    private Toggle relayServiceToggle;
    private UnityTransport unityTransport;

    void Start() {
        usernameCanvas = transform.GetChild(0).gameObject;
        connectionCanvas = transform.GetChild(1).gameObject;
        promptField = connectionCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TMP_Text>();
        relayServiceToggle = connectionCanvas.GetComponentInChildren<Toggle>();
        relayServiceToggle.onValueChanged.AddListener(UpdateConnectionPrompt);
        unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        AuthenticatePlayer();
	}

    async void AuthenticatePlayer() {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn) {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Player authenticated with id: " + AuthenticationService.Instance.PlayerId);
        }
    }

    public void PassUsername(string username) {
        Username = username;
    }

    public void AdvanceLoginCanvas() {
        if (Username.Length < 1) { Debug.LogError("Username must be at least 1 character long!"); return; }
        if (Username.Length > 32) { Debug.LogError("Username can be at most 32 characters long!"); return; }
        usernameCanvas.SetActive(false);
        connectionCanvas.SetActive(true);
    }

    private void UpdateConnectionPrompt(bool relayStatus) {
        promptField.text = relayStatus ? "Enter Code..." : "Enter IP...";
    }

    public void PassConnectionTarget(string target) {
        if (relayServiceToggle.isOn) {
            RelayCode = target;
            IP = null;
        } else {
            IP = target;
			RelayCode = null;
		}
    }

    public void ConnectClient() {
        if (relayServiceToggle.isOn) {
            ConnectRelayClient();
        } else {
            ConnectDirectClient();
        }
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
    }

    private async void ConnectRelayClient() {
        if (!AuthenticationService.Instance.IsSignedIn) { 
            Debug.LogError("Player not yet authenticated! Wait a couple of seconds before trying again."); 
            return;
        }

        RelayJoinData relayHostData;
        try {
            Debug.Log("Attempting to join relay game with code: " + RelayCode);
            relayHostData = await JoinRelayGame(RelayCode);
		} catch (Exception e) {
			Debug.LogError("Failed to join relay game with code: " + RelayCode);
			Debug.LogError(e.Message);
			return;
		}

		Debug.Log("Relay Host Data parameters: ");
        Debug.Log("IP: " + relayHostData.IPv4Address);
        Debug.Log("Port: " + relayHostData.Port);
        Debug.Log("Allocation ID: " + relayHostData.AllocationID);

        unityTransport.SetClientRelayData(relayHostData.IPv4Address, relayHostData.Port, relayHostData.AllocationIDBytes, relayHostData.Key, relayHostData.ConnectionData, relayHostData.HostConnectionData, true);

        StartClient();
    }

    private void ConnectDirectClient() {
        unityTransport.SetConnectionData(IP, Port);

        if (unityTransport.ConnectionData.ServerEndPoint == default) {
			Debug.LogError("The given IP address is invalid. Make sure the IP address is correct and try again.");
			return;
		}

		Debug.Log("Attempting to join directly to IP: " + IP);
		StartClient();
	}

	private void StartClient() {
        if (NetworkManager.Singleton.StartClient()) {
            Debug.Log("Client successfully started!\nConnecting to server...");
        } else {
            Debug.Log("Client could not be started.");
        }
    }

    private void Singleton_OnClientConnectedCallback(ulong obj) {
        Debug.Log("Client successfully connected!");
        connectionCanvas.SetActive(false);
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj) {
        Debug.Log("Client disconnected. (Or connection attempt failed/timed out)");
    }

    public void StartHost() {
        if (relayServiceToggle.isOn) {
            StartRelayHost();
        } else {
            StartDirectHost();
        }
    }

    private async void StartRelayHost() {
        if (!AuthenticationService.Instance.IsSignedIn) { 
            Debug.LogError("Player not yet authenticated! Wait a couple of seconds before trying again."); 
            return;
        }

        RelayHostData relayHostData;
        try {
            Debug.Log("Attempting to create a relay game...");
			relayHostData = await RegisterRelayHost();
		} catch(Exception e) {
			Debug.LogError("Failed to create a relay game.");
			Debug.LogError(e.Message);
            return;
		}

        Debug.Log("Relay Host Data parameters: ");
        Debug.Log("IP: " + relayHostData.IPv4Address);
        Debug.Log("Port: " + relayHostData.Port);
        Debug.Log("Allocation ID: " + relayHostData.AllocationID);

        unityTransport.SetHostRelayData(relayHostData.IPv4Address, relayHostData.Port, relayHostData.AllocationIDBytes, relayHostData.Key, relayHostData.ConnectionData, true);

        RelayCode = relayHostData.JoinCode;
        Debug.Log("Lobby code from Relay:");
        Debug.Log(RelayCode);
        ConnectHost();
    }

    private void StartDirectHost() {
        IP = GetLocalIPAddress();
        unityTransport.SetConnectionData(IP, Port, "0.0.0.0");
		ConnectHost();
    }

    string GetLocalIPAddress() {
		var host = Dns.GetHostEntry(Dns.GetHostName());
        System.Collections.Generic.List<string> interNetworkIPAddresses = new();
		foreach (var ip in host.AddressList) {
			if (ip.AddressFamily == AddressFamily.InterNetwork) {
                interNetworkIPAddresses.Add(ip.ToString());
			}
		}
        if (interNetworkIPAddresses.Count > 0) {
			Debug.Log("Found the following addresses, use one for connecting:" + string.Join(',', interNetworkIPAddresses));
			return interNetworkIPAddresses.First();
		}
        Debug.LogError("Did not find any network network adapters with an IPv4 address in the system.\nIf you know your ip address - use it for connecting.");
        return "n/a";
	}

    private void ConnectHost() {
        if (NetworkManager.Singleton.StartHost()) {
            Debug.Log("Host started successfully! \nAwaiting connections...");
            connectionCanvas.SetActive(false);
            var lobby = Instantiate(lobbyManagerPrefab);
            lobby.GetComponent<NetworkObject>().Spawn();
            Debug.Log("Lobby created!");
        }
        Debug.Log("Host could not be started.");
    }

    private struct RelayHostData {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] Key;
    }

    private async Task<RelayHostData> RegisterRelayHost() {

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

        var serverEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        RelayHostData data = new RelayHostData {
            IPv4Address = serverEndpoint.Host,
            Port = (ushort)serverEndpoint.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            Key = allocation.Key,
        };

        data.JoinCode = await RelayService.Instance.GetJoinCodeAsync(data.AllocationID);

        return data;
    }

    private struct RelayJoinData {
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] HostConnectionData;
        public byte[] Key;
    }

    private async Task<RelayJoinData> JoinRelayGame(string joinCode) {

        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var serverEndPoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");

        return new RelayJoinData { 
            IPv4Address = serverEndPoint.Host, 
            Port = (ushort)serverEndPoint.Port, 
            AllocationID = allocation.AllocationId, 
            AllocationIDBytes = allocation.AllocationIdBytes, 
            ConnectionData = allocation.ConnectionData, 
            HostConnectionData = allocation.HostConnectionData, 
            Key = allocation.Key };
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
			CustomLogger.Instance.LogBoxHidden = !CustomLogger.Instance.LogBoxHidden;
			Debug.Log(CustomLogger.Instance.LogBoxHidden ? "Log box hidden!" : "Log box Shown!");
        }
    }

    public void PlayerDisconnected() {
        connectionCanvas.SetActive(true);
    }
}
