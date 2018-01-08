using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILanNetworking : UIBase
{
    public UILanNetworkingEntry entryPrefab;
    public Transform gameListContainer;
    private readonly Dictionary<string, UILanNetworkingEntry> entries = new Dictionary<string, UILanNetworkingEntry>();

    private void OnEnable()
    {
        SimpleLanNetworkDiscovery.onReceivedBroadcast += OnReceivedBroadcast;
    }

    private void OnDisable()
    {
        SimpleLanNetworkDiscovery.onReceivedBroadcast -= OnReceivedBroadcast;
    }

    private void OnDestroy()
    {
        SimpleLanNetworkDiscovery.onReceivedBroadcast -= OnReceivedBroadcast;
    }

    private void OnReceivedBroadcast(string fromAddress, string data)
    {
        Debug.Log("fromAddress " + fromAddress + " data " + data);
        var discoveryData = JsonUtility.FromJson<NetworkDiscoveryData>(data);
        var key = fromAddress + "-" + discoveryData.networkPort;
        if (!entries.ContainsKey(key))
        {
            var newEntry = Instantiate(entryPrefab, gameListContainer);
            newEntry.SetData(fromAddress, discoveryData);
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
