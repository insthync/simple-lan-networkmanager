using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using LiteNetLib;
using LiteNetLib.Utils;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using MLAPI.Spawning;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;
using MLAPI.Connection;

public abstract class BaseNetworkGameManager : SimpleLanNetworkManager
{
    public static new BaseNetworkGameManager Singleton
    {
        get { return singleton as BaseNetworkGameManager; }
    }
    public static event System.Action onClientDisconnected;

    public BaseNetworkGameRule gameRule;
    public int serverRestartDelay = 3;
    public bool doNotKeepPlayerScore;
    protected float updateScoreTime;
    protected float updateMatchTime;
    protected bool canUpdateGameRule;
    public readonly List<BaseNetworkGameCharacter> Characters = new List<BaseNetworkGameCharacter>();
    public readonly Dictionary<string, ulong> PlayerCharacterObjectIds = new Dictionary<string, ulong>();
    public readonly Dictionary<string, NetworkGameScore> PlayerScores = new Dictionary<string, NetworkGameScore>();
    public float RemainsMatchTime { get; protected set; }
    public bool IsMatchEnded { get; protected set; }
    public float MatchEndedAt { get; protected set; }

    public int CountAliveCharacters()
    {
        var count = 0;
        foreach (var character in Characters)
        {
            if (character == null)
                continue;
            if (!character.IsDead)
                ++count;
        }
        return count;
    }

    public int CountDeadCharacters()
    {
        var count = 0;
        foreach (var character in Characters)
        {
            if (character == null)
                continue;
            if (character.IsDead)
                ++count;
        }
        return count;
    }

    protected override void Start()
    {
        base.Start();
        NetworkManager.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
    }

    protected override void Update()
    {
        base.Update();
        if (NetworkManager.IsServer)
            ServerUpdate();
        if (NetworkManager.IsClient)
            ClientUpdate();
    }

    protected virtual void FixedUpdate()
    {
        if (NetworkManager.IsServer && !doNotKeepPlayerScore)
        {
            // Store score
            NetworkObject tempIdentity;
            BaseNetworkGameCharacter tempCharacter;
            foreach (var kvPair in PlayerCharacterObjectIds)
            {
                if (!NetworkSpawnManager.SpawnedObjects.TryGetValue(kvPair.Value, out tempIdentity))
                    continue;
                tempCharacter = tempIdentity.GetComponent<BaseNetworkGameCharacter>();
                if (!tempCharacter)
                    continue;
                PlayerScores[kvPair.Key] = new NetworkGameScore()
                {
                    netId = tempCharacter.NetworkObjectId,
                    playerName = tempCharacter.playerName,
                    team = tempCharacter.playerTeam,
                    score = tempCharacter.Score,
                    killCount = tempCharacter.KillCount,
                    assistCount = tempCharacter.AssistCount,
                    dieCount = tempCharacter.DieCount,
                };
            }
        }
    }

    protected virtual void ServerUpdate()
    {
        if (gameRule != null && canUpdateGameRule)
        {
            gameRule.OnUpdate();

            if (!IsMatchEnded && gameRule.IsMatchEnded)
            {
                UpdateMatchScores();
                UpdateMatchStatus();
                RestartWhenMatchEnd();
                IsMatchEnded = true;
                MatchEndedAt = Time.unscaledTime;
            }

            if (!IsMatchEnded && Time.unscaledTime - updateMatchTime >= 1f)
            {
                RemainsMatchTime = gameRule.RemainsMatchTime;
                UpdateMatchStatus();
                updateMatchTime = Time.unscaledTime;
            }
        }

        if (!IsMatchEnded && Time.unscaledTime - updateScoreTime >= 1f)
        {
            if (gameRule == null || !gameRule.IsMatchEnded)
                UpdateMatchScores();
            updateScoreTime = Time.unscaledTime;
        }
    }

    protected virtual void ClientUpdate()
    {

    }

    protected void UpdateMatchScores()
    {
        var msgSendScores = new OpMsgSendScores();
        msgSendScores.scores = GetSortedScores();
        var buffer = new NetworkBuffer();
        var writer = new NetworkWriter(buffer);
        msgSendScores.Serialize(writer);
        CustomMessagingManager.SendUnnamedMessage(null, buffer);
    }

    protected void UpdateMatchStatus()
    {
        var msgMatchStatus = new OpMsgMatchStatus();
        msgMatchStatus.remainsMatchTime = gameRule.RemainsMatchTime;
        msgMatchStatus.isMatchEnded = gameRule.IsMatchEnded;
        var buffer = new NetworkBuffer();
        var writer = new NetworkWriter(buffer);
        msgMatchStatus.Serialize(writer);
        CustomMessagingManager.SendUnnamedMessage(null, buffer);
    }

    protected async void RestartWhenMatchEnd()
    {
        // Only server will restart when match end
        if (!NetworkManager.IsServer || NetworkManager.IsClient) return;
        NetworkManager.StopServer();
        Debug.Log($"Server stopped, will be started in {serverRestartDelay} seconds");
        await Task.Delay(serverRestartDelay * 1000);
        NetworkManager.StartServer();
    }

    public void SendKillNotify(string killerName, string victimName, string weaponId)
    {
        if (!NetworkManager.IsServer)
            return;

        var msgKillNotify = new OpMsgKillNotify();
        msgKillNotify.killerName = killerName;
        msgKillNotify.victimName = victimName;
        msgKillNotify.weaponId = weaponId;
        var buffer = new NetworkBuffer();
        var writer = new NetworkWriter(buffer);
        msgKillNotify.Serialize(writer);
        CustomMessagingManager.SendUnnamedMessage(null, buffer);
    }

    public NetworkGameScore[] GetSortedScores()
    {
        for (var i = Characters.Count - 1; i >= 0; --i)
        {
            var character = Characters[i];
            if (character == null)
                Characters.RemoveAt(i);
        }
        Characters.Sort();
        var scores = new NetworkGameScore[Characters.Count];
        for (var i = 0; i < Characters.Count; ++i)
        {
            var character = Characters[i];
            var score = new NetworkGameScore();
            score.netId = character.NetworkObjectId;
            score.playerName = character.playerName;
            score.team = character.playerTeam;
            score.score = character.Score;
            score.killCount = character.KillCount;
            score.assistCount = character.AssistCount;
            score.dieCount = character.DieCount;
            if (score.netId == BaseNetworkGameCharacter.LocalNetId)
                BaseNetworkGameCharacter.LocalRank = i + 1;
            scores[i] = score;
        }
        return scores;
    }

    public void RegisterCharacter(BaseNetworkGameCharacter character, string deviceUniqueIdentifier = "")
    {
        if (character == null || Characters.Contains(character))
            return;
        character.RegisterNetworkGameManager(this);
        Characters.Add(character);
        if (!string.IsNullOrEmpty(deviceUniqueIdentifier))
        {
            PlayerCharacterObjectIds[deviceUniqueIdentifier] = character.NetworkObjectId;
            NetworkGameScore gameScore;
            if (!doNotKeepPlayerScore && PlayerScores.TryGetValue(deviceUniqueIdentifier, out gameScore))
            {
                character.score = gameScore.score;
                character.killCount = gameScore.killCount;
                character.assistCount = gameScore.assistCount;
                character.dieCount = gameScore.dieCount;
            }
        }
    }

    public bool CanCharacterRespawn(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        if (gameRule != null)
            return gameRule.CanCharacterRespawn(character, extraParams);
        return true;
    }

    public bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        if (gameRule != null)
            return gameRule.RespawnCharacter(character, extraParams);
        return true;
    }

    public virtual void OnScoreIncrease(BaseNetworkGameCharacter character, int increaseAmount)
    {
        if (gameRule != null && Characters.Contains(character))
            gameRule.OnScoreIncrease(character, increaseAmount);
    }

    public virtual void OnKillIncrease(BaseNetworkGameCharacter character, int increaseAmount)
    {
        if (gameRule != null && Characters.Contains(character))
            gameRule.OnKillIncrease(character, increaseAmount);
    }

    public virtual void OnUpdateCharacter(BaseNetworkGameCharacter character)
    {
        if (gameRule != null && Characters.Contains(character))
            gameRule.OnUpdateCharacter(character);
    }

    public override void OnStartServer()
    {
        updateScoreTime = 0f;
        updateMatchTime = 0f;
        RemainsMatchTime = 0f;
        IsMatchEnded = false;
        MatchEndedAt = 0f;
        canUpdateGameRule = false;
    }

    protected override void RegisterMessages()
    {
        RegisterMessage(OpMsgSendScores.OpId, ReadMsgSendScores);
        RegisterMessage(OpMsgGameRule.OpId, ReadMsgGameRule);
        RegisterMessage(OpMsgMatchStatus.OpId, ReadMsgMatchStatus);
        RegisterMessage(OpMsgKillNotify.OpId, ReadMsgKillNotify);
    }

    protected void ReadMsgSendScores(ulong clientId, PooledNetworkReader reader)
    {
        var msg = new OpMsgSendScores();
        msg.Deserialize(reader);
        UpdateScores(msg.scores);
    }

    protected void ReadMsgGameRule(ulong clientId, PooledNetworkReader reader)
    {
        if (NetworkManager.IsServer)
            return;
        var msg = new OpMsgGameRule();
        msg.Deserialize(reader);
        BaseNetworkGameRule foundGameRule;
        if (BaseNetworkGameInstance.GameRules.TryGetValue(msg.gameRuleName, out foundGameRule))
        {
            gameRule = foundGameRule;
            gameRule.InitialClientObjects();
        }
    }

    protected void ReadMsgMatchStatus(ulong clientId, PooledNetworkReader reader)
    {
        var msg = new OpMsgMatchStatus();
        msg.Deserialize(reader);
        RemainsMatchTime = msg.remainsMatchTime;
        if (!IsMatchEnded && msg.isMatchEnded)
        {
            IsMatchEnded = true;
            MatchEndedAt = Time.unscaledTime;
        }
    }

    protected void ReadMsgKillNotify(ulong clientId, PooledNetworkReader reader)
    {
        var msg = new OpMsgKillNotify();
        msg.Deserialize(reader);
        KillNotify(msg.killerName, msg.victimName, msg.weaponId);
    }

    public override void OnClientDisconnected()
    {
        if (onClientDisconnected != null)
            onClientDisconnected.Invoke();
    }

    public override void OnStopClient()
    {
        if (gameRule != null)
        {
            gameRule.OnStopConnection();
            gameRule = null;
        }
    }

    public override void OnStopServer()
    {
        Characters.Clear();
    }

    public override void StartGameClient()
    {
        var buffer = new NetworkBuffer();
        var writer = new NetworkWriter(buffer);
        writer.WriteString(GetPlayerUDID());
        PrepareCharacter(writer);
        NetworkManager.NetworkConfig.ConnectionData = buffer.GetBuffer();
        base.StartGameClient();
    }

    private void NetworkManager_ConnectionApprovalCallback(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        if (IsMatchEnded)
        {
            // Match ended, don't approve new connection
            callback.Invoke(false, null, false, null, null);
        }
        // Send game rule data before spawn player's character
        if (gameRule == null || !gameRule.IsMatchEnded)
        {
            // send scores
            var msgSendScores = new OpMsgSendScores();
            msgSendScores.scores = GetSortedScores();
            var buffer = new NetworkBuffer();
            var writer = new NetworkWriter(buffer);
            msgSendScores.Serialize(writer);
            CustomMessagingManager.SendUnnamedMessage(new List<ulong>() { clientId }, buffer);
        }
        if (gameRule != null)
        {
            // send game rule
            var msgGameRule = new OpMsgGameRule();
            msgGameRule.gameRuleName = gameRule.name;
            var buffer = new NetworkBuffer();
            var writer = new NetworkWriter(buffer);
            msgGameRule.Serialize(writer);
            CustomMessagingManager.SendUnnamedMessage(new List<ulong>() { clientId }, buffer);
            // Set match time
            var msgMatchTime = new OpMsgMatchStatus();
            msgMatchTime.remainsMatchTime = gameRule.RemainsMatchTime;
            msgMatchTime.isMatchEnded = gameRule.IsMatchEnded;
            buffer = new NetworkBuffer();
            writer = new NetworkWriter(buffer);
            msgMatchTime.Serialize(writer);
            CustomMessagingManager.SendUnnamedMessage(new List<ulong>() { clientId }, buffer);
        }
        var reader = new NetworkReader(new NetworkBuffer(connectionData));
        var deviceUniqueIdentifier = reader.ReadString().ToString();
        var character = NewCharacter(reader);
        if (character == null)
        {
            Debug.LogError("Cannot create new character for player " + clientId);
            callback.Invoke(false, null, false, null, null);
            return;
        }
        // Approved
        callback.Invoke(false, null, true, null, null);
        var networkObj = character.GetComponent<NetworkObject>();
        networkObj.SpawnAsPlayerObject(clientId);
        RegisterCharacter(character, deviceUniqueIdentifier);
    }

    public override void OnPeerDisconnected(ulong connectionId)
    {
        NetworkClient player;
        if (NetworkManager.ConnectedClients.TryGetValue(connectionId, out player))
        {
            foreach (var spawnedObject in player.OwnedObjects)
            {
                var character = spawnedObject.GetComponent<BaseNetworkGameCharacter>();
                if (character != null)
                    Characters.Remove(character);
            }
        }
    }

    protected void RegisterGamerulePrefabs()
    {
        foreach (var gameRule in BaseNetworkGameInstance.GameRules.Values)
        {
            if (gameRule == null) continue;
            gameRule.RegisterPrefabs();
        }
    }

    public override void OnClientOnlineSceneLoaded()
    {
        base.OnClientOnlineSceneLoaded();
        RegisterGamerulePrefabs();
        // Scene loaded, then the client will send ready message to server
    }

    public override void OnServerOnlineSceneLoaded()
    {
        base.OnServerOnlineSceneLoaded();
        RegisterGamerulePrefabs();
        if (gameRule != null)
        {
            gameRule.OnStartServer();
            if (NetworkManager.IsClient)
                gameRule.InitialClientObjects();
        }
        canUpdateGameRule = true;
    }

    protected virtual string GetPlayerUDID()
    {
        // May override to use something like `userID` which stored at database
        return SystemInfo.deviceUniqueIdentifier;
    }

    /// <summary>
    /// Prepare character at client
    /// </summary>
    /// <param name="writer"></param>
    protected abstract void PrepareCharacter(NetworkWriter writer);
    /// <summary>
    /// Create new character at server
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    protected abstract BaseNetworkGameCharacter NewCharacter(NetworkReader reader);
    protected abstract void UpdateScores(NetworkGameScore[] scores);
    protected abstract void KillNotify(string killerName, string victimName, string weaponId);
}
