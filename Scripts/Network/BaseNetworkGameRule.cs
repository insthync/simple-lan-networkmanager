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
    private float matchStartTime;
    private bool isMatchEnded;
    public SimpleLanNetworkManager manager { get; protected set; }
    public string Title { get { return title; } }
    public string Description { get { return description; } }
    public BaseNetworkGameRule(SimpleLanNetworkManager manager)
    {
        this.manager = manager;
    }
    protected abstract void AddBot();
    protected abstract void EndMatch();

    public virtual void ReadConfigs(Dictionary<string, string> configs)
    {
        if (configs.ContainsKey(BotCountKey))
            int.TryParse(configs[BotCountKey], out botCount);
        if (configs.ContainsKey(MatchTimeKey))
            int.TryParse(configs[MatchTimeKey], out matchTime);
    }

    public virtual void OnServerSceneChanged(string sceneName)
    {
        for (var i = 0; i < botCount; ++i)
        {
            AddBot();
        }
    }

    public virtual void OnStartServer()
    {
        matchStartTime = Time.unscaledTime;
        isMatchEnded = false;
    }

    public virtual void OnUpdate()
    {
        if (matchTime > 0 && Time.unscaledTime - matchStartTime >= matchTime && !isMatchEnded)
        {
            isMatchEnded = true;
            EndMatch();
        }
    }
}
