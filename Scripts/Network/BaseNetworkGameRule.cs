using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class BaseNetworkGameRule : ScriptableObject
{
    public const string BotCountKey = "BotCount";
    public const string MatchTimeKey = "MatchTime";
    public const string MatchKillKey = "MatchKill";
    public const string MatchScoreKey = "MatchScore";

    [SerializeField]
    private string title;
    [SerializeField, TextArea]
    private string description;
    [SerializeField]
    private int defaultBotCount;
    [HideInInspector]
    public int botCount;
    [SerializeField, Tooltip("Time in seconds, 0 = Unlimit")]
    private int defaultMatchTime;
    [HideInInspector]
    public int matchTime;
    [SerializeField, Tooltip("Match kill limit, 0 = Unlimit")]
    private int defaultMatchKill;
    [HideInInspector]
    public int matchKill;
    [SerializeField, Tooltip("Match score limit, 0 = Unlimit")]
    private int defaultMatchScore;
    [HideInInspector]
    public int matchScore;
    protected float matchStartTime;
    protected BaseNetworkGameManager networkManager;
    protected bool isBotAdded;
    public string Title { get { return title; } }
    public string Description { get { return description; } }
    protected abstract BaseNetworkGameCharacter NewBot();
    protected abstract void EndMatch();
    public int DefaultBotCount { get { return defaultBotCount; } }
    public int DefaultMatchTime { get { return defaultMatchTime; } }
    public int DefaultMatchKill { get { return defaultMatchKill; } }
    public int DefaultMatchScore { get { return defaultMatchScore; } }
    public virtual bool HasOptionBotCount { get { return false; } }
    public virtual bool HasOptionMatchTime { get { return false; } }
    public virtual bool HasOptionMatchKill { get { return false; } }
    public virtual bool HasOptionMatchScore { get { return false; } }
    public virtual bool ShowZeroScoreWhenDead { get { return false; } }
    public virtual bool ShowZeroKillCountWhenDead { get { return false; } }
    public virtual bool ShowZeroAssistCountWhenDead { get { return false; } }
    public virtual bool ShowZeroDieCountWhenDead { get { return false; ; } }
    public abstract bool CanCharacterRespawn(BaseNetworkGameCharacter character, params object[] extraParams);
    public abstract bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams);
    public float RemainsMatchTime
    {
        get
        {
            if (HasOptionMatchTime && matchTime > 0 && Time.unscaledTime - matchStartTime < matchTime && !IsMatchEnded)
                return matchTime - (Time.unscaledTime - matchStartTime);
            return 0f;
        }
    }
    public bool IsMatchEnded { get; protected set; }

    public virtual void AddBots()
    {
        if (!HasOptionBotCount)
            return;

        for (var i = 0; i < botCount; ++i)
        {
            var character = NewBot();
            if (character == null)
                continue;
            NetworkServer.Spawn(character.gameObject);
            networkManager.RegisterCharacter(character);
        }
    }

    public virtual void ReadConfigs(Dictionary<string, string> configs)
    {
        if (configs.ContainsKey(BotCountKey))
            int.TryParse(configs[BotCountKey], out botCount);
        if (configs.ContainsKey(MatchTimeKey))
            int.TryParse(configs[MatchTimeKey], out matchTime);
        if (configs.ContainsKey(MatchKillKey))
            int.TryParse(configs[MatchKillKey], out matchKill);
        if (configs.ContainsKey(MatchScoreKey))
            int.TryParse(configs[MatchScoreKey], out matchScore);
    }

    public virtual void OnStartServer(BaseNetworkGameManager manager)
    {
        matchStartTime = Time.unscaledTime;
        networkManager = manager;
        isBotAdded = false;
        IsMatchEnded = false;
    }

    public virtual void OnUpdate()
    {
        if (!isBotAdded)
        {
            isBotAdded = true;
            AddBots();
        }

        if (HasOptionMatchTime && matchTime > 0 && Time.unscaledTime - matchStartTime >= matchTime && !IsMatchEnded)
        {
            IsMatchEnded = true;
            EndMatch();
        }
    }

    public virtual void OnUpdateCharacter(BaseNetworkGameCharacter character)
    {
        if (HasOptionMatchScore && matchScore > 0 && character.Score >= matchScore)
        {
            IsMatchEnded = true;
            EndMatch();
        }

        if (HasOptionMatchKill && matchKill > 0 && character.KillCount >= matchKill)
        {
            IsMatchEnded = true;
            EndMatch();
        }
    }

    public abstract void InitialClientObjects(NetworkClient client);
}
