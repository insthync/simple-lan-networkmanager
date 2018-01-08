using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UIEnterNetworkAddress : UIBase
{
    public InputField inputAddress;
    public InputField inputPort;
    protected override void Awake()
    {
        base.Awake();
        inputPort.contentType = InputField.ContentType.IntegerNumber;
    }

    public virtual void OnClickConnect()
    {
        var networkManager = NetworkManager.singleton;
        networkManager.networkAddress = inputAddress.text;
        networkManager.networkPort = int.Parse(inputPort.text);
        networkManager.StartClient();
    }
}
