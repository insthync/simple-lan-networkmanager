using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class UILanGameCreate : UIBase
{
    public int maxPlayerCustomizable = 32;
    public InputField inputRoomName;
    public InputField inputMaxPlayer;
    [Header("Match Bot Count")]
    public GameObject containerBotCount;
    public InputField inputBotCount;
    [Header("Match Time")]
    public GameObject containerMatchTime;
    public InputField inputMatchTime;
    [Header("Match Kill")]
    public GameObject containerMatchKill;
    public InputField inputMatchKill;
    [Header("Match Score")]
    public GameObject containerMatchScore;
    public InputField inputMatchScore;
    [Header("Maps")]
    public Image previewImage;
    [HideInInspector]
    // TODO: Will be deleted later
    public MapSelection[] maps;
    public Dropdown mapList;
    [Header("Game rules")]
    public Dropdown gameRuleList;

    private BaseNetworkGameRule[] gameRules;

    private void Start()
    {
        if (maps != null && maps.Length > 0)
            UpdateNetworkGameInstanceMaps();
    }

    public virtual void OnClickCreateGame()
    {
        var selectedMap = GetSelectedMap();
        var selectedGameRule = GetSelectedGameRule();
        var networkManager = SimpleLanNetworkManager.Singleton;
        var networkGameManager = networkManager as BaseNetworkGameManager;

        if (selectedMap != null)
            networkManager.Assets.onlineScene.SceneName = selectedMap.scene.SceneName;

        if (selectedGameRule != null && networkGameManager != null)
        {
            selectedGameRule.BotCount = inputBotCount == null ? selectedGameRule.DefaultBotCount : int.Parse(inputBotCount.text);
            selectedGameRule.MatchTime = inputMatchTime == null ? selectedGameRule.DefaultMatchTime : int.Parse(inputMatchTime.text);
            selectedGameRule.MatchKill = inputMatchKill == null ? selectedGameRule.DefaultMatchKill : int.Parse(inputMatchKill.text);
            selectedGameRule.MatchScore = inputMatchScore == null ? selectedGameRule.DefaultMatchScore : int.Parse(inputMatchScore.text);
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

        if (containerBotCount != null)
            containerBotCount.SetActive(selected.HasOptionBotCount);

        if (containerMatchTime != null)
            containerMatchTime.SetActive(selected.HasOptionMatchTime);

        if (containerMatchKill != null)
            containerMatchKill.SetActive(selected.HasOptionMatchKill);

        if (containerMatchScore != null)
            containerMatchScore.SetActive(selected.HasOptionMatchScore);

        if (inputBotCount != null)
        {
            inputBotCount.contentType = InputField.ContentType.IntegerNumber;
            inputBotCount.text = selected.DefaultBotCount.ToString();
            inputBotCount.onValueChanged.RemoveListener(OnBotCountChanged);
            inputBotCount.onValueChanged.AddListener(OnBotCountChanged);
        }

        if (inputMatchTime != null)
        {
            inputMatchTime.contentType = InputField.ContentType.IntegerNumber;
            inputMatchTime.text = selected.DefaultMatchTime.ToString();
            inputMatchTime.onValueChanged.RemoveListener(OnMatchTimeChanged);
            inputMatchTime.onValueChanged.AddListener(OnMatchTimeChanged);
        }

        if (inputMatchKill != null)
        {
            inputMatchKill.contentType = InputField.ContentType.IntegerNumber;
            inputMatchKill.text = selected.DefaultMatchKill.ToString();
            inputMatchKill.onValueChanged.RemoveListener(OnMatchKillChanged);
            inputMatchKill.onValueChanged.AddListener(OnMatchKillChanged);
        }

        if (inputMatchScore != null)
        {
            inputMatchScore.contentType = InputField.ContentType.IntegerNumber;
            inputMatchScore.text = selected.DefaultMatchScore.ToString();
            inputMatchScore.onValueChanged.RemoveListener(OnMatchScoreChanged);
            inputMatchScore.onValueChanged.AddListener(OnMatchScoreChanged);
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
            inputMatchTime.text = matchTime.ToString();
    }

    public void OnMatchKillChanged(string value)
    {
        int matchKill = 0;
        if (!int.TryParse(value, out matchKill))
            inputMatchKill.text = matchKill.ToString();
    }

    public void OnMatchScoreChanged(string value)
    {
        int matchScore = 0;
        if (!int.TryParse(value, out matchScore))
            inputMatchScore.text = matchScore.ToString();
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

    [ContextMenu("Update Network Game Instance Maps")]
    public void UpdateNetworkGameInstanceMaps()
    {
        BaseNetworkGameInstance gameInstance = FindObjectOfType<BaseNetworkGameInstance>();
        if (gameInstance != null && (gameInstance.maps == null || gameInstance.maps.Length == 0))
            gameInstance.maps = maps;
    }
}
