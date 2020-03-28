using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using System.Text.RegularExpressions;

public class UINetworkClientError : MonoBehaviour
{
    public static UINetworkClientError Singleton { get; private set; }
    public UIMessageDialog messageDialog;

    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Singleton = this;
        BaseNetworkGameManager.onClientDisconnected += OnClientDisconnected;
    }

    public void OnClientDisconnected(DisconnectInfo disconnectInfo)
    {
        if (disconnectInfo.Reason == DisconnectReason.DisconnectPeerCalled)
            return;

        if (messageDialog == null)
            return;
        
        messageDialog.Show(Regex.Replace(disconnectInfo.Reason.ToString(), "(?!^)([A-Z])", " $1"));
    }
}
