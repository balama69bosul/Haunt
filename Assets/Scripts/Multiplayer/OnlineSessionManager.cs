using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// First online foundation for Haunt.
/// Creates a two-player Relay room and lets another player join with a code.
/// The actual investigator/ghost network prefabs are integrated in the next stage.
/// </summary>
public sealed class OnlineSessionManager : MonoBehaviour
{
    public static OnlineSessionManager Instance { get; private set; }

    private NetworkManager _networkManager;
    private UnityTransport _transport;

    private string _joinCode = string.Empty;
    private string _joinCodeInput = string.Empty;
    private string _status = "Online multiplayer ready.";
    private bool _isBusy;
    private bool _showPanel = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAtStartup()
    {
        if (Instance != null)
            return;

        GameObject sessionObject = new GameObject("OnlineSessionManager");
        sessionObject.AddComponent<OnlineSessionManager>();
        DontDestroyOnLoad(sessionObject);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateNetworkManager();
    }

    private void CreateNetworkManager()
    {
        if (NetworkManager.Singleton != null)
        {
            _networkManager = NetworkManager.Singleton;
            _transport = _networkManager.GetComponent<UnityTransport>();
            return;
        }

        GameObject networkObject = new GameObject("HauntNetworkManager");
        DontDestroyOnLoad(networkObject);

        _transport = networkObject.AddComponent<UnityTransport>();
        _networkManager = networkObject.AddComponent<NetworkManager>();
        _networkManager.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = _transport,
            EnableSceneManagement = true,
            ConnectionApproval = false
        };

        _networkManager.OnClientConnectedCallback += OnClientConnected;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public async void CreateRoom()
    {
        if (_isBusy || IsConnected())
            return;

        _isBusy = true;
        _status = "Connecting to Unity services...";

        try
        {
            await InitializeUnityServices();

            _status = "Creating online room...";
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            _transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                true);

            if (!_networkManager.StartHost())
                throw new InvalidOperationException("Netcode could not start the host.");

            _status = "Room created. Send this code to your friend.";
        }
        catch (Exception exception)
        {
            _status = "Could not create room: " + exception.Message;
            Debug.LogException(exception);
        }
        finally
        {
            _isBusy = false;
        }
    }

    public async void JoinRoom(string code)
    {
        if (_isBusy || IsConnected())
            return;

        string normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCode.Length == 0)
        {
            _status = "Enter the room code first.";
            return;
        }

        _isBusy = true;
        _status = "Joining online room...";

        try
        {
            await InitializeUnityServices();

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(normalizedCode);

            _transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData,
                true);

            if (!_networkManager.StartClient())
                throw new InvalidOperationException("Netcode could not start the client.");

            _status = "Connecting to host...";
        }
        catch (Exception exception)
        {
            _status = "Could not join room: " + exception.Message;
            Debug.LogException(exception);
        }
        finally
        {
            _isBusy = false;
        }
    }

    public void LeaveRoom()
    {
        if (_networkManager != null && _networkManager.IsListening)
            _networkManager.Shutdown();

        _joinCode = string.Empty;
        _status = "Disconnected.";
    }

    private static async Task InitializeUnityServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private bool IsConnected()
    {
        return _networkManager != null && _networkManager.IsListening;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (_networkManager == null)
            return;

        if (clientId == _networkManager.LocalClientId)
            _status = "Connected to online session.";
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (_networkManager == null)
            return;

        if (clientId == _networkManager.LocalClientId)
        {
            _joinCode = string.Empty;
            _status = "Disconnected from online session.";
        }
    }

    private void OnGUI()
    {
        if (!_showPanel)
        {
            if (GUI.Button(new Rect(10, 10, 110, 30), "Online"))
                _showPanel = true;
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 330, 260), GUI.skin.box);
        GUILayout.Label("HAUNT - ONLINE MULTIPLAYER");
        GUILayout.Space(6);
        GUILayout.Label(_status);

        if (!string.IsNullOrEmpty(_joinCode))
        {
            GUILayout.Label("ROOM CODE");
            GUILayout.TextField(_joinCode);
        }

        GUILayout.Space(8);
        GUI.enabled = !_isBusy && !IsConnected();
        if (GUILayout.Button("Create room (Investigator)"))
            CreateRoom();

        GUILayout.Label("Join a friend's room");
        _joinCodeInput = GUILayout.TextField(_joinCodeInput, 12);
        if (GUILayout.Button("Join room (Ghost)"))
            JoinRoom(_joinCodeInput);

        GUI.enabled = true;
        if (IsConnected() && GUILayout.Button("Leave room"))
            LeaveRoom();

        if (GUILayout.Button("Hide panel"))
            _showPanel = false;

        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (_networkManager != null)
        {
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        Instance = null;
    }
}