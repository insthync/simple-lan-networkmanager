using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILanNetworkingEntry : MonoBehaviour
{
    public Text textRoomName;
    public Text textPlayerName;
    public Text textSceneName;
    public Text textNetworkAddress;
    public Text textPlayerCount;
    private string _networkAddress;
    private NetworkDiscoveryData _data;
    public void SetData(string networkAddress, NetworkDiscoveryData data)
    {
        _networkAddress = networkAddress;
        _data = data;
        if (textRoomName != null)
            textRoomName.text = data.roomName;
        if (textPlayerName != null)
            textPlayerName.text = data.playerName;
        if (textSceneName != null)
            textSceneName.text = data.sceneName;
        if (textNetworkAddress != null)
            textNetworkAddress.text = networkAddress + ":" + data.networkPort;
        if (textPlayerCount != null)
            textPlayerCount.text = data.numPlayers + "/" + data.maxPlayers;
    }

    public virtual void OnClickJoinButton()
    {
        var networkManager = SimpleLanNetworkManager.Singleton;
        networkManager.networkAddress = _networkAddress;
        networkManager.networkPort = _data.networkPort;
        networkManager.StartClient();
        networkManager.NetworkDiscovery.StopClient();
        networkManager.NetworkDiscovery.StopServer();
    }
}
