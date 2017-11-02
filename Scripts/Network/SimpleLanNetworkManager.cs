﻿using System.Collections;
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

    private bool isLanHost;
    private int dirtyNumPlayers;

    protected virtual void Update()
    {
        if (NetworkServer.active && isLanHost && numPlayers != dirtyNumPlayers)
        {
            var discoveryData = new NetworkDiscoveryData();
            discoveryData.playerName = PlayerSave.GetPlayerName();
            discoveryData.sceneName = onlineScene;
            discoveryData.networkAddress = networkAddress;
            discoveryData.networkPort = networkPort;
            discoveryData.numPlayers = numPlayers;
            discoveryData.maxPlayers = maxConnections;
            NetworkDiscovery.useNetworkManager = false;
            NetworkDiscovery.broadcastData = JsonUtility.ToJson(discoveryData);
            StartCoroutine(RestartDiscoveryBroadcast());
            dirtyNumPlayers = numPlayers;
        }
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

    public override void OnStopServer()
    {
        base.OnStopServer();
        isLanHost = false;
    }
}