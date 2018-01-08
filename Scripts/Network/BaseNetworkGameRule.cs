using System.Collections.Generic;
using UnityEngine;

public abstract class BaseNetworkGameRule : ScriptableObject
{
    public const string BotCountKey = "BotCount";
    public const string MatchTimeKey = "MatchTime";

    [SerializeField]
    private string title;
    [SerializeField, TextArea]
    private string description;
    public int botCount;
    [Tooltip("Time in seconds, 0 = Unlimit")]
    public int matchTime;
    protected float matchStartTime;
    protected bool isAddBotOnUpdate;
    protected bool isBotAdded;
    protected bool isMatchEnded;
    public string Title { get { return title; } }
    public string Description { get { return description; } }
    protected abstract void AddBot();
    protected abstract void EndMatch();

    public virtual void AddBots()
    {
        for (var i = 0; i < botCount; ++i)
        {
            AddBot();
        }
    }

    public virtual void ReadConfigs(Dictionary<string, string> configs)
    {
        if (configs.ContainsKey(BotCountKey))
            int.TryParse(configs[BotCountKey], out botCount);
        if (configs.ContainsKey(MatchTimeKey))
            int.TryParse(configs[MatchTimeKey], out matchTime);
    }

    public virtual void OnServerSceneChanged(string sceneName)
    {
        if (!isBotAdded)
        {
            AddBots();
            isBotAdded = true;
        }
    }

    public virtual void OnStartServer(bool addBotOnUpdate)
    {
        matchStartTime = Time.unscaledTime;
        isAddBotOnUpdate = addBotOnUpdate;
        isBotAdded = false;
        isMatchEnded = false;
    }

    public virtual void OnUpdate()
    {
        if (isAddBotOnUpdate && !isBotAdded)
        {
            AddBots();
            isBotAdded = true;
        }

        if (matchTime > 0 && Time.unscaledTime - matchStartTime >= matchTime && !isMatchEnded)
        {
            isMatchEnded = true;
            EndMatch();
        }
    }
}
