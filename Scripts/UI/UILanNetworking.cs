using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILanNetworking : UIBase
{
    public UILanNetworkingEntry entryPrefab;
    public Transform gameListContainer;
    private readonly Dictionary<string, UILanNetworkingEntry> entries = new Dictionary<string, UILanNetworkingEntry>();
    private SimpleLanNetworkDiscovery discovery;
    
    private void OnEnable()
    {
        if (discovery != null)
            discovery = FindObjectOfType<SimpleLanNetworkDiscovery>();
        if (discovery != null)
            discovery.onReceivedBroadcast += OnReceivedBroadcast;
    }

    private void OnDisable()
    {
        if (discovery != null)
            discovery.onReceivedBroadcast -= OnReceivedBroadcast;
    }

    private void OnDestroy()
    {
        if (discovery != null)
            discovery.onReceivedBroadcast -= OnReceivedBroadcast;
    }

    private void OnReceivedBroadcast(System.Net.IPEndPoint fromAddress, string data)
    {
        Debug.Log("OnReceivedBroadcast data " + data);
        var discoveryData = JsonUtility.FromJson<NetworkDiscoveryData>(data);
        var key = discoveryData.networkAddress + "-" + discoveryData.networkPort;
        if (!entries.ContainsKey(key))
        {
            var newEntry = Instantiate(entryPrefab, gameListContainer);
            newEntry.SetData(discoveryData.networkAddress, discoveryData);
            newEntry.gameObject.SetActive(true);
            entries.Add(key, newEntry);
        }
    }

    public virtual void OnClickStartLanHost()
    {
        var networkManager = SimpleLanNetworkManager.Singleton;
        networkManager.WriteBroadcastData();
        networkManager.StartLanHost();
    }

    public virtual void OnClickRefreshLanGames()
    {
        for (var i = gameListContainer.childCount - 1; i >= 0; --i)
        {
            var child = gameListContainer.GetChild(i);
            Destroy(child.gameObject);
        }
        entries.Clear();
        SimpleLanNetworkManager.Singleton.FindLanHosts();
    }

    public override void Show()
    {
        base.Show();
        OnClickRefreshLanGames();
    }
}
