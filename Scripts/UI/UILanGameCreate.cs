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
        public BaseNetworkGameRule[] availableGameRules;
    }

    public int maxPlayerCustomizable = 32;
    public InputField inputRoomName;
    public InputField inputMaxPlayer;
    public InputField inputBotCount;
    public InputField inputMatchTime;
    public Image previewImage;
    [Header("Map list")]
    public MapSelection[] maps;
    public Dropdown mapList;
    [Header("Game rule list")]
    public Dropdown gameRuleList;

    private BaseNetworkGameRule[] gameRules;

    public virtual void OnClickCreateGame()
    {
        var selectedMap = GetSelectedMap();
        var selectedGameRule = GetSelectedGameRule();
        var networkManager = SimpleLanNetworkManager.Singleton;
        var networkGameManager = networkManager as BaseNetworkGameManager;

        if (selectedMap != null)
            networkManager.onlineScene = selectedMap.scene.SceneName;

        if (selectedGameRule != null && networkGameManager != null)
        {
            selectedGameRule.botCount = inputBotCount == null ? 0 : int.Parse(inputBotCount.text);
            selectedGameRule.matchTime = inputMatchTime == null ? 0 : int.Parse(inputMatchTime.text);
            networkGameManager.gameRule = selectedGameRule;
        }

        if (inputMaxPlayer != null)
            networkManager.maxConnections = int.Parse(inputMaxPlayer.text);

        networkManager.WriteBroadcastData();
        networkManager.StartLanHost();
    }

    public void OnMapListChange(int value)
    {
        if (gameRuleList != null)
            gameRuleList.ClearOptions();

        var selected = GetSelectedMap();
        
        if (selected == null)
        {
            Debug.LogError("Invalid map selection");
            return;
        }

        previewImage.sprite = selected.previewImage;
        gameRules = selected.availableGameRules;

        if (gameRuleList != null)
        {
            gameRuleList.AddOptions(gameRules.Select(a => new Dropdown.OptionData(a.Title)).ToList());
            gameRuleList.onValueChanged.RemoveListener(OnGameRuleListChange);
            gameRuleList.onValueChanged.AddListener(OnGameRuleListChange);
        }

        OnGameRuleListChange(0);
    }

    public void OnGameRuleListChange(int value)
    {
        var selected = GetSelectedGameRule();

        if (selected == null)
        {
            Debug.LogError("Invalid game rule selection");
            return;
        }
    }

    public void OnMaxPlayerChanged(string value)
    {
        int maxPlayer = maxPlayerCustomizable;
        if (!int.TryParse(value, out maxPlayer) || maxPlayer > maxPlayerCustomizable)
            inputMaxPlayer.text = maxPlayer.ToString();
    }

    public void OnBotCountChanged(string value)
    {
        int botCount = 0;
        if (!int.TryParse(value, out botCount))
            inputBotCount.text = botCount.ToString();
    }

    public void OnMatchTimeChanged(string value)
    {
        int matchTime = 0;
        if (!int.TryParse(value, out matchTime))
            inputBotCount.text = matchTime.ToString();
    }

    public override void Show()
    {
        base.Show();

        if (mapList != null)
        {
            mapList.ClearOptions();
            mapList.AddOptions(maps.Select(a => new Dropdown.OptionData(a.mapName)).ToList());
            mapList.onValueChanged.RemoveListener(OnMapListChange);
            mapList.onValueChanged.AddListener(OnMapListChange);
        }

        if (inputMaxPlayer != null)
        {
            inputMaxPlayer.contentType = InputField.ContentType.IntegerNumber;
            inputMaxPlayer.text = maxPlayerCustomizable.ToString();
            inputMaxPlayer.onValueChanged.RemoveListener(OnMaxPlayerChanged);
            inputMaxPlayer.onValueChanged.AddListener(OnMaxPlayerChanged);
        }

        if (inputBotCount != null)
        {
            inputBotCount.contentType = InputField.ContentType.IntegerNumber;
            inputBotCount.text = "0";
            inputBotCount.onValueChanged.RemoveListener(OnBotCountChanged);
            inputBotCount.onValueChanged.AddListener(OnBotCountChanged);
        }

        if (inputMatchTime != null)
        {
            inputMatchTime.contentType = InputField.ContentType.IntegerNumber;
            inputMatchTime.text = "0";
            inputMatchTime.onValueChanged.RemoveListener(OnMatchTimeChanged);
            inputMatchTime.onValueChanged.AddListener(OnMatchTimeChanged);
        }

        OnMapListChange(0);
    }

    public MapSelection GetSelectedMap()
    {
        var text = mapList.captionText.text;
        return maps.FirstOrDefault(m => m.mapName == text);
    }

    public BaseNetworkGameRule GetSelectedGameRule()
    {
        var text = gameRuleList.captionText.text;
        return gameRules.FirstOrDefault(m => m.Title == text);
    }
}
