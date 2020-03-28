using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using System.Net.Sockets;

public class UINetworkClientError : MonoBehaviour
{
    public static UINetworkClientError Singleton { get; private set; }
    public UIMessageDialog messageDialog;
    public string roomFullMessage = "The room is full, try another room.";

    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Singleton = this;
        BaseNetworkGameManager.onClientError += OnClientError;
    }

    public void OnClientError(SocketError error)
    {
        if (messageDialog == null)
            return;

        switch (error)
        {
            case SocketError.ConnectionRefused:
                if (!string.IsNullOrEmpty(roomFullMessage))
                    messageDialog.Show(roomFullMessage);
                break;
        }
    }
}
