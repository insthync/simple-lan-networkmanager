using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using System.Threading.Tasks;

public abstract class BaseNetworkGameRule : ScriptableObject
{
    public const string BotCountKey = "-gameBotCount";
    public const string MatchTimeKey = "-gameMatchTime";
    public const string MatchKillKey = "-gameMatchKill";
    public const string MatchScoreKey = "-gameMatchScore";

    [SerializeField]
    private string title = string.Empty;
    [SerializeField, TextArea]
    private string description = string.Empty;
    [SerializeField]
    private int defaultBotCount = 0;
    [System.NonSerialized]
    private int botCount = 0;
    [SerializeField, Tooltip("Time in seconds, 0 = Unlimit")]
    private int defaultMatchTime = 0;
    [System.NonSerialized]
    private int matchTime = 0;
    [SerializeField, Tooltip("Match kill limit, 0 = Unlimit")]
    private int defaultMatchKill = 0;
    [System.NonSerialized]
    private int matchKill = 0;
    [SerializeField, Tooltip("Match score limit, 0 = Unlimit")]
    private int defaultMatchScore = 0;
    [System.NonSerialized]
    private int matchScore = 0;
    protected float matchStartTime;
    protected bool isBotAdded;
    protected int teamScoreA;
    protected int teamScoreB;
    protected int teamKillA;
    protected int teamKillB;
    public string Title { get { return title; } }
    public string Description { get { return description; } }
    protected abstract BaseNetworkGameCharacter NewBot();
    public int DefaultBotCount { get { return defaultBotCount; } }
    public int DefaultMatchTime { get { return defaultMatchTime; } }
    public int DefaultMatchKill { get { return defaultMatchKill; } }
    public int DefaultMatchScore { get { return defaultMatchScore; } }
    public int BotCount { get { return botCount; } set { botCount = value; } }
    public int MatchTime { get { return matchTime; } set { matchTime = value; } }
    public int MatchKill { get { return matchKill; } set { matchKill = value; } }
    public int MatchScore { get { return matchScore; } set { matchScore = value; } }
    public virtual bool HasOptionBotCount { get { return false; } }
    public virtual bool HasOptionMatchTime { get { return false; } }
    public virtual bool HasOptionMatchKill { get { return false; } }
    public virtual bool HasOptionMatchScore { get { return false; } }
    public virtual bool IsTeamGameplay { get { return false; } }
    public virtual bool ShowZeroScoreWhenDead { get { return false; } }
    public virtual bool ShowZeroKillCountWhenDead { get { return false; } }
    public virtual bool ShowZeroAssistCountWhenDead { get { return false; } }
    public virtual bool ShowZeroDieCountWhenDead { get { return false; ; } }
    public virtual bool RankedByKillCount { get { return false; } }
    public abstract bool CanCharacterRespawn(BaseNetworkGameCharacter character, params object[] extraParams);
    public abstract bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams);

    protected readonly Dictionary<uint, int> CharacterCollectedScore = new Dictionary<uint, int>();
    protected readonly Dictionary<uint, int> CharacterCollectedKill = new Dictionary<uint, int>();

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
    public BaseNetworkGameManager NetworkManager { get { return BaseNetworkGameManager.Singleton; } }


    public async void DelayingAddBots()
    {
        await Task.Delay(1000);
        AddBots();
    }

    public virtual void AddBots()
    {
        if (!HasOptionBotCount)
            return;

        for (var i = 0; i < botCount; ++i)
        {
            var character = NewBot();
            if (character == null)
                continue;
            NetworkManager.Assets.NetworkSpawn(character.gameObject);
            NetworkManager.RegisterCharacter(character);
        }
    }

    protected virtual List<BaseNetworkGameCharacter> GetBots()
    {
        List<BaseNetworkGameCharacter> result = new List<BaseNetworkGameCharacter>(FindObjectsOfType<BaseNetworkGameCharacter>());
        for (int i = result.Count - 1; i >= 0; --i)
        {
            if (!result[i].IsBot)
                result.RemoveAt(i);
        }
        return result;
    }

    public virtual void ReadConfigs(string[] args)
    {
        botCount = EnvironmentArgsUtils.ReadArgsInt(args, BotCountKey, defaultBotCount);
        matchTime = EnvironmentArgsUtils.ReadArgsInt(args, MatchTimeKey, defaultMatchTime);
        matchKill = EnvironmentArgsUtils.ReadArgsInt(args, MatchKillKey, defaultMatchKill);
        matchScore = EnvironmentArgsUtils.ReadArgsInt(args, MatchScoreKey, defaultMatchScore);
    }

    public virtual void OnStartServer()
    {
        matchStartTime = Time.unscaledTime;
        teamScoreA = 0;
        teamScoreB = 0;
        teamKillA = 0;
        teamKillB = 0;
        IsMatchEnded = false;
        DelayingAddBots();
    }

    public virtual void OnStopConnection()
    {
        teamScoreA = 0;
        teamScoreB = 0;
        teamKillA = 0;
        teamKillB = 0;
        IsMatchEnded = false;
    }

    public virtual void OnUpdate()
    {
        if (HasOptionMatchTime && matchTime > 0 && Time.unscaledTime - matchStartTime >= matchTime && !IsMatchEnded)
        {
            IsMatchEnded = true;
        }
    }

    public virtual void OnScoreIncrease(BaseNetworkGameCharacter character, int increaseAmount)
    {
        if (!CharacterCollectedScore.ContainsKey(character.ObjectId))
            CharacterCollectedScore[character.ObjectId] = increaseAmount;
        else
            CharacterCollectedScore[character.ObjectId] += increaseAmount;

        if (IsTeamGameplay)
        {
            // TODO: Improve team codes
            switch (character.playerTeam)
            {
                case 1:
                    teamScoreA += increaseAmount;
                    break;
                case 2:
                    teamScoreB += increaseAmount;
                    break;
            }
        }
    }

    public virtual void OnKillIncrease(BaseNetworkGameCharacter character, int increaseAmount)
    {
        if (!CharacterCollectedKill.ContainsKey(character.ObjectId))
            CharacterCollectedKill[character.ObjectId] = increaseAmount;
        else
            CharacterCollectedKill[character.ObjectId] += increaseAmount;

        if (IsTeamGameplay)
        {
            // TODO: Improve team codes
            switch (character.playerTeam)
            {
                case 1:
                    teamKillA += increaseAmount;
                    break;
                case 2:
                    teamKillB += increaseAmount;
                    break;
            }
        }
    }

    public virtual void OnUpdateCharacter(BaseNetworkGameCharacter character)
    {
        if (IsMatchEnded)
            return;

        int checkScore = character.Score;
        int checkKill = character.KillCount;
        if (IsTeamGameplay)
        {
            // Use team score / kill as checker
            switch (character.playerTeam)
            {
                case 1:
                    checkScore = teamScoreA;
                    checkKill = teamKillA;
                    break;
                case 2:
                    checkScore = teamScoreB;
                    checkKill = teamKillB;
                    break;
            }
        }

        if (HasOptionMatchScore && matchScore > 0 && checkScore >= matchScore)
        {
            IsMatchEnded = true;
        }

        if (HasOptionMatchKill && matchKill > 0 && checkKill >= matchKill)
        {
            IsMatchEnded = true;
        }
    }

    public abstract void InitialClientObjects(LiteNetLibClient client);
    public abstract void RegisterPrefabs();
}
