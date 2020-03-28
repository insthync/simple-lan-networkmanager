using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;

[RequireComponent(typeof(SimpleLanNetworkDiscovery))]
public class SimpleLanNetworkManager : LiteNetLibGameManager
{
    protected static SimpleLanNetworkManager singleton { get; set; }
    public static SimpleLanNetworkManager Singleton
    {
        get { return singleton; }
    }

    private SimpleLanNetworkDiscovery networkDiscovery;
    public SimpleLanNetworkDiscovery NetworkDiscovery
    {
        get
        {
            if (networkDiscovery == null)
                networkDiscovery = GetComponent<SimpleLanNetworkDiscovery>();
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

    private IEnumerator StopNetworkDiscovery()
    {
        yield return null;
        NetworkDiscovery.StopClient();
        NetworkDiscovery.StopServer();
    }

    public void FindLanHosts()
    {
        StartCoroutine(FindLanHostsRoutine());
    }

    private IEnumerator FindLanHostsRoutine()
    {
        yield return StartCoroutine(StopNetworkDiscovery());
        NetworkDiscovery.StartClient();
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
        StartCoroutine(RestartDiscoveryBroadcast());
    }

    public void StartGameClient()
    {
        StartClient();
    }

    private IEnumerator RestartDiscoveryBroadcast()
    {
        yield return StartCoroutine(StopNetworkDiscovery());
        NetworkDiscovery.StartServer();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        isLanHost = false;
    }

    public void StartHostAndQuitIfCannotListen()
    {
        if (StartHost() == null)
            Application.Quit();
    }

    public void StartServerAndQuitIfCannotListen()
    {
        if (!StartServer())
            Application.Quit();
    }
}
