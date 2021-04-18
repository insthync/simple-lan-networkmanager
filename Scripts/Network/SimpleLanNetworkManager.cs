using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Transports.UNET;
using System.Threading.Tasks;
using MLAPI.Serialization.Pooled;
using MLAPI.Messaging;
using System.IO;
using MLAPI.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UNetTransport))]
[RequireComponent(typeof(LiteNetLibDiscovery))]
public class SimpleLanNetworkManager : MonoBehaviour
{
    public delegate void RegisterableFunc(ulong clientId, PooledNetworkReader reader);
    public readonly Dictionary<uint, RegisterableFunc> RegisteredFuncs = new Dictionary<uint, RegisterableFunc>();
    protected static SimpleLanNetworkManager singleton { get; set; }
    public static SimpleLanNetworkManager Singleton
    {
        get { return singleton; }
    }

    private LiteNetLibDiscovery networkDiscovery;
    public LiteNetLibDiscovery NetworkDiscovery
    {
        get
        {
            if (networkDiscovery == null)
                networkDiscovery = GetComponent<LiteNetLibDiscovery>();
            return networkDiscovery;
        }
    }

    private NetworkManager networkManager;
    public NetworkManager NetworkManager
    {
        get
        {
            if (networkManager == null)
                networkManager = GetComponent<NetworkManager>();
            return networkManager;
        }
    }

    private UNetTransport uNetTransport;
    public UNetTransport UNetTransport
    {
        get
        {
            if (uNetTransport == null)
                uNetTransport = GetComponent<UNetTransport>();
            return uNetTransport;
        }
    }

    public string roomName;
    
    private bool isLanHost;
    private int dirtyNumPlayers;
    private bool dirtyIsClient;
    private bool dirtyIsServer;

    protected virtual void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void Start()
    {
        NetworkManager.OnServerStarted += OnStartServer;
        NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback; ;
        NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback; ;
        NetworkSceneManager.OnSceneSwitchStarted += NetworkSceneManager_OnSceneSwitchStarted;
        CustomMessagingManager.OnUnnamedMessage += CustomMessagingManager_OnUnnamedMessage;
        RegisterMessages();
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if (NetworkManager.IsServer)
            OnPeerConnected(obj);
        else
            OnClientConnected();
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong obj)
    {
        if (NetworkManager.IsServer)
            OnPeerDisconnected(obj);
        else
            OnClientDisconnected();
    }

    private void NetworkSceneManager_OnSceneSwitchStarted(AsyncOperation operation)
    {
        OnSceneSwitchProgress(operation);
    }

    private void CustomMessagingManager_OnUnnamedMessage(ulong clientId, Stream stream)
    {
        using (var reader = PooledNetworkReader.Get(stream))
        {
            var msgId = reader.ReadUInt16();
            RegisterableFunc func;
            if (RegisteredFuncs.TryGetValue(msgId, out func))
            {
                func.Invoke(clientId, reader);
            }
        }
    }

    protected async void OnSceneSwitchProgress(AsyncOperation operation)
    {
        while (!operation.isDone)
        {
            await Task.Yield();
        }
        if (NetworkManager.IsClient)
        {
            OnClientOnlineSceneLoaded();
        }
        if (NetworkManager.IsServer)
        {
            OnServerOnlineSceneLoaded();
        }
    }

    public virtual void OnClientOnlineSceneLoaded()
    {

    }

    public virtual void OnServerOnlineSceneLoaded()
    {

    }

    protected virtual void RegisterMessages()
    {

    }

    public void RegisterMessage(ushort id, RegisterableFunc func)
    {
        RegisteredFuncs[id] = func;
    }

    public virtual void WriteBroadcastData()
    {
        var discoveryData = new NetworkDiscoveryData();
        discoveryData.roomName = roomName;
        discoveryData.playerName = PlayerSave.GetPlayerName();
        discoveryData.sceneName = SceneManager.GetActiveScene().name;
        discoveryData.networkAddress = UNetTransport.ConnectAddress;
        discoveryData.networkPort = UNetTransport.ServerListenPort;
        discoveryData.numPlayers = NetworkManager.ConnectedClientsList.Count;
        discoveryData.maxPlayers = UNetTransport.MaxConnections;
        NetworkDiscovery.data = JsonUtility.ToJson(discoveryData);
    }

    protected virtual void Update()
    {
        if (dirtyIsClient != NetworkManager.IsClient)
        {
            dirtyIsClient = NetworkManager.IsClient;
            if (!dirtyIsClient)
            {
                OnStopClient();
            }
        }

        if (dirtyIsServer != NetworkManager.IsServer)
        {
            dirtyIsServer = NetworkManager.IsServer;
            if (!dirtyIsServer)
            {
                OnStopServer();
            }
        }

        if (!NetworkManager.IsServer)
            return;

        if (isLanHost && NetworkManager.ConnectedClientsList.Count != dirtyNumPlayers)
        {
            WriteBroadcastData();
            dirtyNumPlayers = NetworkManager.ConnectedClientsList.Count;
        }
    }

    public void StartDedicateServer()
    {
        NetworkManager.StartServer();
    }

    public void StartLanHost()
    {
        NetworkManager.StartHost();
        isLanHost = true;
        WriteBroadcastData();
        // Stop discovery client because game started
        NetworkDiscovery.StopClient();
        // Start discovery server to allow clients to connect
        NetworkDiscovery.StartServer();
    }

    public virtual void StartGameClient()
    {
        NetworkManager.StartClient();
        // Stop discovery client because game started
        NetworkDiscovery.StopClient();
    }

    public virtual void OnStartServer()
    {

    }

    public virtual void OnPeerConnected(ulong connectionId)
    {

    }

    public virtual void OnPeerDisconnected(ulong connectionId)
    {

    }

    public virtual void OnClientConnected()
    {

    }

    public virtual void OnClientDisconnected()
    {

    }

    public virtual void OnStopClient()
    {

    }

    public virtual void OnStopServer()
    {
        isLanHost = false;
    }

    public virtual void OnStopHost()
    {
        NetworkDiscovery.StopClient();
        NetworkDiscovery.StopServer();
    }

    public async void StartHostAndQuitIfCannotListen()
    {
        var sockets = NetworkManager.StartHost();
        while (!sockets.IsDone)
            await Task.Yield();
        if (!sockets.Success)
            Application.Quit();
    }

    public async void StartServerAndQuitIfCannotListen()
    {
        var sockets = NetworkManager.StartServer();
        while (!sockets.IsDone)
            await Task.Yield();
        if (!sockets.Success)
            Application.Quit();
    }
}
