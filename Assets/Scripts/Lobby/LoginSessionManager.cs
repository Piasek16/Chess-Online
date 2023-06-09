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
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

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
	private GameObject connectingStatusCanvas;
	private Prompt quitCurrentActivityPrompt;

	void Start() {
		usernameCanvas = transform.GetChild(0).gameObject;
		connectionCanvas = transform.GetChild(1).gameObject;
		promptField = connectionCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TMP_Text>();
		relayServiceToggle = connectionCanvas.GetComponentInChildren<Toggle>();
		relayServiceToggle.onValueChanged.AddListener(UpdateConnectionPrompt);
		unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
		AuthenticatePlayer();
		NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
		if (Camera.main.aspect < 1.32f)
			Debug.LogWarning("Aspect ratio is too small for the game to be played properly! - Use an aspect ratio of 4:3 or higher.");
		SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
		usernameCanvas.GetComponentInChildren<TMP_InputField>().onSubmit.AddListener(username => { PassUsername(username); AdvanceLoginCanvas(); });
		connectionCanvas.GetComponentInChildren<TMP_InputField>().onSubmit.AddListener(target => { PassConnectionTarget(target); ConnectClient(); });
		connectingStatusCanvas = transform.GetChild(3).gameObject;
		quitCurrentActivityPrompt = transform.GetChild(2).GetComponent<Prompt>();
	}

	private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1) {
		if (arg1.buildIndex != 0) return;
		unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
	}

	void OnDestroy() {
		if (NetworkManager.Singleton == null)
			return;
		NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
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
		if (Username.Length > 29) { Debug.LogError("Username can be at most 29 characters long!"); return; }
		usernameCanvas.SetActive(false);
		connectionCanvas.SetActive(true);
		StartCoroutine(SelectNextCanvasAtEndOfFrame());
	}

	IEnumerator SelectNextCanvasAtEndOfFrame() {
		yield return new WaitForEndOfFrame();
		connectionCanvas.GetComponentInChildren<TMP_InputField>().Select();
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
		StartAnimatingConnectingProcess();
		if (relayServiceToggle.isOn) {
			ConnectRelayClient();
		} else {
			ConnectDirectClient();
		}
	}

	void StartAnimatingConnectingProcess() {
		connectingStatusCanvas.SetActive(true);
		StartCoroutine(nameof(AnimateConnectingStatus));
	}

	IEnumerator AnimateConnectingStatus() {
		var field = connectingStatusCanvas.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
		while (true) {
			field.text += ".";
			if (field.text.Length >= 4 && field.text[^4..] == "....")
				field.text = field.text.Replace("....", "");
			yield return new WaitForSeconds(1);
		}
	}

	void StopAnimatingConnectingProcess() {
		StopCoroutine(nameof(AnimateConnectingStatus));
		connectingStatusCanvas.SetActive(false);
	}

	private async void ConnectRelayClient() {
		if (!AuthenticationService.Instance.IsSignedIn) {
			Debug.LogError("Player not yet authenticated! Wait a couple of seconds before trying again.");
			StopAnimatingConnectingProcess();
			return;
		}

		RelayJoinData relayHostData;
		try {
			Debug.Log("Attempting to join relay game with code: " + RelayCode);
			relayHostData = await JoinRelayGame(RelayCode);
		} catch (Exception e) {
			Debug.LogError("Failed to join relay game with code: " + RelayCode);
			Debug.LogError(e.Message);
			StopAnimatingConnectingProcess();
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
			StopAnimatingConnectingProcess();
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
			StopAnimatingConnectingProcess();
		}
	}

	private void Singleton_OnClientConnectedCallback(ulong obj) {
		Debug.Log("Client successfully connected!");
		connectionCanvas.SetActive(false);
		StopAnimatingConnectingProcess();
	}

	private void Singleton_OnClientDisconnectCallback(ulong obj) {
		if (obj == 0) { // Server client id is guaranteed to be 0
			Debug.Log("[INFO] Server disconnected.");
			Disconnect();
			return;
		}
		if (NetworkManager.Singleton.LocalClientId != obj)
			return;
		Debug.Log("Client disconnected. (Or connection attempt failed/timed out)");
		Disconnect();
		StopAnimatingConnectingProcess();
	}

	public void CancelConnectionAttempt() {
		Debug.Log("Connection attempt cancelled.");
		NetworkManager.Singleton.Shutdown();
		StopAnimatingConnectingProcess();
	}

	public void Disconnect() {
		NetworkManager.Singleton.Shutdown();
		connectionCanvas.SetActive(true);
		if (SceneManager.GetActiveScene().buildIndex != 0)
			SceneManager.LoadScene(0);
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
		} catch (Exception e) {
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
		List<string> interNetworkIPAddresses = new();
		foreach (var ip in host.AddressList) {
			if (ip.AddressFamily == AddressFamily.InterNetwork) {
				interNetworkIPAddresses.Add(ip.ToString());
			}
		}
		if (interNetworkIPAddresses.Count > 0) {
			Debug.Log("Found the following addresses, use one for connecting: " + string.Join(", ", interNetworkIPAddresses));
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
		} else {
			Debug.Log("Host could not be started.");
		}
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
			Key = allocation.Key
		};
	}

	bool PromptVisible { get => quitCurrentActivityPrompt.gameObject.activeSelf; set => quitCurrentActivityPrompt.gameObject.SetActive(value); }
	enum UserActivity { InMenu, InLobby, PlayingGame };
	UserActivity userActivity;
	void Update() {
		if (Input.GetKeyDown(KeyCode.BackQuote)) {
			CustomLogger.Instance.LogBoxHidden = !CustomLogger.Instance.LogBoxHidden;
			Debug.Log(CustomLogger.Instance.LogBoxHidden ? "Log box hidden!" : "Log box Shown!");
		}
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (PromptVisible) {
				PromptVisible = false;
				return;
			}
			userActivity = DetermineUserActivity();
			string activityQuitText = userActivity switch {
				UserActivity.PlayingGame => "Surrender game?",
				UserActivity.InLobby => "Quit from lobby?",
				_ => "Exit game?",
			};
			PromptVisible = true;
			quitCurrentActivityPrompt.PromptText = activityQuitText;
		}
	}

	UserActivity DetermineUserActivity() {
		if (SceneManager.GetActiveScene().name == "ClassicMode") {
			return UserActivity.PlayingGame;
		}
		if (LobbyManager != null) {
			return UserActivity.InLobby;
		}
		return UserActivity.InMenu;
	}

	public void ProceedWithPrompt() {
		PromptVisible = false;
		switch (userActivity) {
			case UserActivity.PlayingGame:
				GameSessionManager.Instance.SurrenderGame();
				break;
			case UserActivity.InLobby:
				LobbyManager.QuitLobby();
				break;
			default:
				GetComponent<QuitScript>().ExitGame();
				break;
		}
	}
}
