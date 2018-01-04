using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UILanGameCreate : UIBase
{
    [System.Serializable]
    public class MapSelection
    {
        public string mapName;
        public SceneNameField scene;
        public Sprite previewImage;
    }
    public int maxPlayerCustomizable = 32;
    public InputField inputMaxPlayer;
    public Image previewImage;
    public MapSelection[] maps;
    public Dropdown mapList;

    public void OnClickCreateGame()
    {
        var selectedMap = GetSelectedMap();
        var networkManager = SimpleLanNetworkManager.Singleton;
        networkManager.maxConnections = int.Parse(inputMaxPlayer.text);
        networkManager.onlineScene = selectedMap.scene.SceneName;

        var discoveryData = new NetworkDiscoveryData();
        discoveryData.playerName = PlayerSave.GetPlayerName();
        discoveryData.networkAddress = networkManager.networkAddress;
        discoveryData.networkPort = networkManager.networkPort;
        networkManager.NetworkDiscovery.useNetworkManager = false;
        networkManager.NetworkDiscovery.broadcastData = JsonUtility.ToJson(discoveryData);

        networkManager.StartLanHost();
    }

    public void OnMapListChange(int value)
    {
        var selected = GetSelectedMap();

        if (selected == null)
        {
            Debug.LogError("Invalid map selection");
            return;
        }

        previewImage.sprite = selected.previewImage;
    }

    public void OnMaxPlayerChanged(string value)
    {
        int maxPlayer = maxPlayerCustomizable;
        if (!int.TryParse(value, out maxPlayer) || maxPlayer > maxPlayerCustomizable)
            inputMaxPlayer.text = maxPlayer.ToString();
    }

    public override void Show()
    {
        base.Show();
        
        mapList.ClearOptions();
        mapList.AddOptions(maps.Select(m => new Dropdown.OptionData(m.mapName)).ToList());
        mapList.onValueChanged.RemoveListener(OnMapListChange);
        mapList.onValueChanged.AddListener(OnMapListChange);

        inputMaxPlayer.contentType = InputField.ContentType.IntegerNumber;
        inputMaxPlayer.text = maxPlayerCustomizable.ToString();
        inputMaxPlayer.onValueChanged.RemoveListener(OnMaxPlayerChanged);
        inputMaxPlayer.onValueChanged.AddListener(OnMaxPlayerChanged);

        OnMapListChange(0);
    }

    public MapSelection GetSelectedMap()
    {
        var text = mapList.captionText.text;
        return maps.FirstOrDefault(m => m.mapName == text);
    }
}
