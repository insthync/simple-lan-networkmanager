﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
    [Tooltip("Match kill limit, 0 = Unlimit")]
    public int matchKill;
    [Tooltip("Match score limit, 0 = Unlimit")]
    public int matchScore;
    protected float matchStartTime;
    protected BaseNetworkGameManager networkManager;
    protected bool isBotAdded;
    protected bool isMatchEnded;
    public string Title { get { return title; } }
    public string Description { get { return description; } }
    protected abstract BaseNetworkGameCharacter NewBot();
    protected abstract void EndMatch();
    public abstract bool HasOptionBotCount { get; }
    public abstract bool HasOptionMatchTime { get; }
    public abstract bool HasOptionMatchKill { get; }
    public abstract bool HasOptionMatchScore { get; }
    public abstract bool CanCharacterRespawn(BaseNetworkGameCharacter character, params object[] extraParams);
    public abstract bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams);

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
    }

    public virtual void OnStartServer(BaseNetworkGameManager manager)
    {
        matchStartTime = Time.unscaledTime;
        networkManager = manager;
        isBotAdded = false;
        isMatchEnded = false;
    }

    public virtual void OnUpdate()
    {
        if (!isBotAdded)
        {
            AddBots();
            isBotAdded = true;
        }

        if (HasOptionMatchTime && matchTime > 0 && Time.unscaledTime - matchStartTime >= matchTime && !isMatchEnded)
        {
            isMatchEnded = true;
            EndMatch();
        }
    }

    public virtual void OnUpdateCharacter(BaseNetworkGameCharacter character)
    {
        if (HasOptionMatchScore && matchScore > 0 && character.Score >= matchScore)
        {
            isMatchEnded = true;
            EndMatch();
        }

        if (HasOptionMatchKill && matchKill > 0 && character.KillCount >= matchKill)
        {
            isMatchEnded = true;
            EndMatch();
        }
    }

    public abstract void InitialClientObjects(NetworkClient client);
}
