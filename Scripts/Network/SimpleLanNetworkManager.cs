using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(SimpleLanNetworkDiscovery))]
public class SimpleLanNetworkManager : NetworkManager
{
    public static SimpleLanNetworkManager Singleton
    {
        get { return singleton as SimpleLanNetworkManager; }
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
    public BaseNetworkGameRule gameRule;
    
    private bool isLanHost;
    private int dirtyNumPlayers;

    public virtual void WriteBroadcastData()
    {
        var discoveryData = new NetworkDiscoveryData();
        discoveryData.roomName = roomName;
        discoveryData.playerName = PlayerSave.GetPlayerName();
        discoveryData.sceneName = onlineScene;
        discoveryData.networkAddress = networkAddress;
        discoveryData.networkPort = networkPort;
        discoveryData.numPlayers = numPlayers;
        discoveryData.maxPlayers = maxConnections;
        NetworkDiscovery.useNetworkManager = false;
        NetworkDiscovery.broadcastData = JsonUtility.ToJson(discoveryData);
    }

    protected virtual void Update()
    {
        if (!NetworkServer.active)
            return;

        if (isLanHost && numPlayers != dirtyNumPlayers)
        {
            WriteBroadcastData();
            StartCoroutine(RestartDiscoveryBroadcast());
            dirtyNumPlayers = numPlayers;
        }

        if (gameRule != null)
            gameRule.OnUpdate();
    }

    public void FindLanHosts()
    {
        StartCoroutine(FindLanHostsRoutine());
    }

    private IEnumerator FindLanHostsRoutine()
    {
        if (NetworkDiscovery.running)
        {
            NetworkDiscovery.StopBroadcast();
            yield return new WaitForSeconds(0.5f);
        }
        NetworkDiscovery.Initialize();
        NetworkDiscovery.StartAsClient();
    }

    public void StartDedicateServer()
    {
        StartServer();
    }

    public void StartLanHost()
    {
        StartHost();
        isLanHost = true;
    }

    private IEnumerator RestartDiscoveryBroadcast()
    {
        if (NetworkDiscovery.running)
        {
            NetworkDiscovery.StopBroadcast();
            yield return new WaitForSeconds(0.5f);
        }
        NetworkDiscovery.Initialize();
        NetworkDiscovery.StartAsServer();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (gameRule != null)
            gameRule.OnStartServer();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        isLanHost = false;
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        if (gameRule != null)
            gameRule.OnServerSceneChanged(sceneName);
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
