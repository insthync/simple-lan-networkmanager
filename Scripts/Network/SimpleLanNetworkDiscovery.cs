using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SimpleLanNetworkDiscovery : NetworkDiscovery
{
    public static System.Action<string, string> onReceivedBroadcast;
    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        if (onReceivedBroadcast != null)
            onReceivedBroadcast(fromAddress, data);
    }
}
