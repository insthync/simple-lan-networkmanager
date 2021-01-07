using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;

[RequireComponent(typeof(LiteNetLibDiscovery))]
public class SimpleLanNetworkManager : LiteNetLibGameManager
{
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

    public string roomName;
    
    private bool isLanHost;
    private int dirtyNumPlayers;

    protected override void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
        doNotDestroyOnSceneChanges = true;
        base.Awake();
    }

    public virtual void WriteBroadcastData()
    {
        var discoveryData = new NetworkDiscoveryData();
        discoveryData.roomName = roomName;
        discoveryData.playerName = PlayerSave.GetPlayerName();
        discoveryData.sceneName = Assets.onlineScene.SceneName;
        discoveryData.networkAddress = networkAddress;
        discoveryData.networkPort = networkPort;
        discoveryData.numPlayers = PlayersCount;
        discoveryData.maxPlayers = maxConnections;
        NetworkDiscovery.data = JsonUtility.ToJson(discoveryData);
    }

    protected virtual void Update()
    {
        if (!IsServer)
            return;

        if (isLanHost && PlayersCount != dirtyNumPlayers)
        {
            WriteBroadcastData();
            dirtyNumPlayers = PlayersCount;
        }
    }

    public void StartDedicateServer()
    {
        StartServer();
    }

    public void StartLanHost()
    {
        StartHost();
        isLanHost = true;
        WriteBroadcastData();
        // Stop discovery client because game started
        NetworkDiscovery.StopClient();
        // Start discovery server to allow clients to connect
        NetworkDiscovery.StartServer();
    }

    public void StartGameClient()
    {
        StartClient();
        // Stop discovery client because game started
        NetworkDiscovery.StopClient();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        isLanHost = false;
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        NetworkDiscovery.StopClient();
        NetworkDiscovery.StopServer();
    }

    public void StartHostAndQuitIfCannotListen()
    {
        if (!StartHost())
            Application.Quit();
    }

    public void StartServerAndQuitIfCannotListen()
    {
        if (!StartServer())
            Application.Quit();
    }
}
