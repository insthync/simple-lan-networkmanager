using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class MapSelection
{
    public string mapName;
    public SceneNameField scene;
    public Sprite previewImage;
    public BaseNetworkGameRule[] availableGameRules;
}

public abstract class BaseNetworkGameInstance : MonoBehaviour
{
    public const string ARG_SERVER_START = "-gameServerStart";
    public const string ARG_SERVER_PORT = "-gameServerPort";
    public const string ARG_SERVER_MAX_CONNECTIONS = "-gameMaxConnections";
    public const string ARG_SERVER_GAME_ONLINE_SCENE = "-gameOnlineScene";
    public const string ARG_SERVER_GAME_RULE = "-gameRule";
    public static BaseNetworkGameInstance Singleton { get; private set; }
    public MapSelection[] maps;
    public static Dictionary<string, BaseNetworkGameRule> GameRules = new Dictionary<string, BaseNetworkGameRule>();
    public static Dictionary<string, MapSelection> MapListBySceneNames = new Dictionary<string, MapSelection>();
    protected virtual void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
        DontDestroyOnLoad(gameObject);
        SetupMaps();
    }

    public void SetupMaps()
    {
        MapListBySceneNames.Clear();
        GameRules.Clear();
        foreach (var map in maps)
        {
            foreach (var gameRule in map.availableGameRules)
            {
                if (!GameRules.ContainsKey(gameRule.name))
                    GameRules[gameRule.name] = gameRule;
            }
            MapListBySceneNames[map.scene.SceneName] = map;
        }
    }

    protected virtual void Start()
    {
        var args = System.Environment.GetCommandLineArgs();
        // Android fix
        if (args == null)
            args = new string[0];
        // Set manager instance
        var manager = SimpleLanNetworkManager.Singleton as BaseNetworkGameManager;
        // If game running in batch mode, run as server
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || EnvironmentArgsUtils.IsArgsProvided(args, ARG_SERVER_START))
        {
            Application.targetFrameRate = 30;
            Debug.Log("Running as server in batch mode");
            var serverPort = manager.networkPort;
            manager.networkPort = EnvironmentArgsUtils.ReadArgsInt(args, ARG_SERVER_PORT, serverPort);
            var maxConnections = manager.maxConnections;
            manager.maxConnections = EnvironmentArgsUtils.ReadArgsInt(args, ARG_SERVER_MAX_CONNECTIONS, maxConnections);
            var onlineScene = manager.Assets.onlineScene.SceneName;
            manager.Assets.onlineScene.SceneName = EnvironmentArgsUtils.ReadArgs(args, ARG_SERVER_GAME_ONLINE_SCENE, onlineScene);

            if (GameRules.Count > 0)
            {
                var allGameRules = new List<BaseNetworkGameRule>(GameRules.Values);
                var gameRule = allGameRules[0];
                var gameRuleName = EnvironmentArgsUtils.ReadArgs(args, ARG_SERVER_GAME_RULE);
                if (!string.IsNullOrEmpty(gameRuleName) && GameRules.ContainsKey(gameRuleName))
                    gameRule = GameRules[gameRuleName];
                manager.gameRule = gameRule;
            }

            if (manager.gameRule != null)
                manager.gameRule.ReadConfigs(args);

            manager.StartDedicateServer();
        }
    }
}
