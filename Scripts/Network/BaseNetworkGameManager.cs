using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using LiteNetLib;
using LiteNetLib.Utils;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Net.Sockets;

public abstract class BaseNetworkGameManager : SimpleLanNetworkManager
{
    public static new BaseNetworkGameManager Singleton
    {
        get { return singleton as BaseNetworkGameManager; }
    }
    public static event System.Action<DisconnectReason, SocketError, byte[]> onClientDisconnected;

    public BaseNetworkGameRule gameRule;
    public int serverRestartDelay = 3;
    public bool doNotKeepPlayerScore;
    protected float updateScoreTime;
    protected float updateMatchTime;
    protected bool canUpdateGameRule;
    public readonly List<BaseNetworkGameCharacter> Characters = new List<BaseNetworkGameCharacter>();
    public readonly Dictionary<string, uint> PlayerCharacterObjectIds = new Dictionary<string, uint>();
    public readonly Dictionary<string, NetworkGameScore> PlayerScores = new Dictionary<string, NetworkGameScore>();
    public float RemainsMatchTime { get; protected set; }
    public bool IsMatchEnded { get; protected set; }
    public float MatchEndedAt { get; protected set; }
    public bool RankedByKillCount
    {
        get
        {
            if (gameRule != null)
                return gameRule.RankedByKillCount;
            return false;
        }
    }

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

    protected override void Update()
    {
        base.Update();
        if (IsServer)
            ServerUpdate();
        if (IsClient)
            ClientUpdate();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (IsServer && !doNotKeepPlayerScore)
        {
            // Store score
            LiteNetLibIdentity tempIdentity;
            BaseNetworkGameCharacter tempCharacter;
            foreach (var kvPair in PlayerCharacterObjectIds)
            {
                if (!Assets.TryGetSpawnedObject(kvPair.Value, out tempIdentity))
                    continue;
                tempCharacter = tempIdentity.GetComponent<BaseNetworkGameCharacter>();
                if (!tempCharacter)
                    continue;
                PlayerScores[kvPair.Key] = new NetworkGameScore()
                {
                    netId = tempCharacter.ObjectId,
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
        ServerSendPacketToAllConnections(0, DeliveryMethod.ReliableOrdered, msgSendScores.OpId, msgSendScores);
    }

    protected void UpdateMatchStatus()
    {
        var msgMatchStatus = new OpMsgMatchStatus();
        msgMatchStatus.remainsMatchTime = gameRule.RemainsMatchTime;
        msgMatchStatus.isMatchEnded = gameRule.IsMatchEnded;
        ServerSendPacketToAllConnections(0, DeliveryMethod.ReliableOrdered, msgMatchStatus.OpId, msgMatchStatus);
    }

    protected async void RestartWhenMatchEnd()
    {
        // Only server will restart when match end
        if (!IsServer || IsClient) return;
        StopServer();
        Debug.Log($"Server stopped, will be started in {serverRestartDelay} seconds");
        await Task.Delay(serverRestartDelay * 1000);
        StartServer();
    }

    public void SendKillNotify(string killerName, string victimName, string weaponId)
    {
        if (!IsServer)
            return;

        var msgKillNotify = new OpMsgKillNotify();
        msgKillNotify.killerName = killerName;
        msgKillNotify.victimName = victimName;
        msgKillNotify.weaponId = weaponId;
        ServerSendPacketToAllConnections(0, DeliveryMethod.ReliableOrdered, msgKillNotify.OpId, msgKillNotify);
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
            score.netId = character.ObjectId;
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
            PlayerCharacterObjectIds[deviceUniqueIdentifier] = character.ObjectId;
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
        base.OnStartServer();
        updateScoreTime = 0f;
        updateMatchTime = 0f;
        RemainsMatchTime = 0f;
        IsMatchEnded = false;
        MatchEndedAt = 0f;
        canUpdateGameRule = false;
    }

    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        RegisterClientMessage(new OpMsgSendScores().OpId, ReadMsgSendScores);
        RegisterClientMessage(new OpMsgGameRule().OpId, ReadMsgGameRule);
        RegisterClientMessage(new OpMsgMatchStatus().OpId, ReadMsgMatchStatus);
        RegisterClientMessage(new OpMsgKillNotify().OpId, ReadMsgKillNotify);
    }

    protected void ReadMsgSendScores(MessageHandlerData messageHandler)
    {
        var msg = messageHandler.ReadMessage<OpMsgSendScores>();
        UpdateScores(msg.scores);
    }

    protected void ReadMsgGameRule(MessageHandlerData messageHandler)
    {
        if (IsServer)
            return;
        var msg = messageHandler.ReadMessage<OpMsgGameRule>();
        BaseNetworkGameRule foundGameRule;
        if (BaseNetworkGameInstance.GameRules.TryGetValue(msg.gameRuleName, out foundGameRule))
        {
            gameRule = foundGameRule;
            gameRule.InitialClientObjects(Client);
        }
    }

    protected void ReadMsgMatchStatus(MessageHandlerData messageHandler)
    {
        var msg = messageHandler.ReadMessage<OpMsgMatchStatus>();
        RemainsMatchTime = msg.remainsMatchTime;
        if (!IsMatchEnded && msg.isMatchEnded)
        {
            IsMatchEnded = true;
            MatchEndedAt = Time.unscaledTime;
        }
    }

    protected void ReadMsgKillNotify(MessageHandlerData messageHandler)
    {
        var msg = messageHandler.ReadMessage<OpMsgKillNotify>();
        KillNotify(msg.killerName, msg.victimName, msg.weaponId);
    }

    protected override UniTaskVoid HandleClientReadyRequest(RequestHandlerData requestHandler, EmptyMessage request, RequestProceedResultDelegate<EmptyMessage> result)
    {
        // Send game rule data before spawn player's character
        if (gameRule == null || !gameRule.IsMatchEnded)
        {
            var msgSendScores = new OpMsgSendScores();
            msgSendScores.scores = GetSortedScores();
            ServerSendPacket(requestHandler.ConnectionId, 0, DeliveryMethod.ReliableOrdered, msgSendScores.OpId, msgSendScores);
        }
        if (gameRule != null)
        {
            var msgGameRule = new OpMsgGameRule();
            msgGameRule.gameRuleName = gameRule.name;
            ServerSendPacket(requestHandler.ConnectionId, 0, DeliveryMethod.ReliableOrdered, msgGameRule.OpId, msgGameRule);
            var msgMatchTime = new OpMsgMatchStatus();
            msgMatchTime.remainsMatchTime = gameRule.RemainsMatchTime;
            msgMatchTime.isMatchEnded = gameRule.IsMatchEnded;
            ServerSendPacket(requestHandler.ConnectionId, 0, DeliveryMethod.ReliableOrdered, msgMatchTime.OpId, msgMatchTime);
        }
        return base.HandleClientReadyRequest(requestHandler, request, result);
    }

    public override void SerializeClientReadyData(NetDataWriter writer)
    {
        base.SerializeClientReadyData(writer);
        writer.Put(GetPlayerUDID());
        PrepareCharacter(writer);
    }

    public override async UniTask<bool> DeserializeClientReadyData(LiteNetLibIdentity playerIdentity, long connectionId, NetDataReader reader)
    {
        await UniTask.Yield();
        var deviceUniqueIdentifier = reader.GetString();
        var character = NewCharacter(reader);
        if (character == null)
        {
            Debug.LogError("Cannot create new character for player " + connectionId);
            return false;
        }
        Assets.NetworkSpawn(character.gameObject, 0, connectionId);
        RegisterCharacter(character, deviceUniqueIdentifier);
        return true;
    }

    public override void OnClientDisconnected(DisconnectReason reason, SocketError socketError, byte[] data)
    {
        base.OnClientDisconnected(reason, socketError, data);
        if (onClientDisconnected != null)
            onClientDisconnected.Invoke(reason, socketError, data);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (gameRule != null)
        {
            gameRule.OnStopConnection();
            gameRule = null;
        }
    }

    public override void OnPeerConnected(long connectionId)
    {
        base.OnPeerConnected(connectionId);
        if (IsMatchEnded)
        {
            // Kick new connection while match is end
            Server.Transport.ServerDisconnect(connectionId);
        }
    }

    public override void OnPeerDisconnected(long connectionId, DisconnectReason reason, SocketError socketError)
    {
        LiteNetLibPlayer player;
        if (Players.TryGetValue(connectionId, out player))
        {
            foreach (var spawnedObject in player.GetSpawnedObjects())
            {
                var character = spawnedObject.GetComponent<BaseNetworkGameCharacter>();
                if (character != null)
                    Characters.Remove(character);
            }
        }
        base.OnPeerDisconnected(connectionId, reason, socketError);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Characters.Clear();
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
            if (IsClient)
                gameRule.InitialClientObjects(Client);
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
    protected abstract void PrepareCharacter(NetDataWriter writer);
    /// <summary>
    /// Create new character at server
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    protected abstract BaseNetworkGameCharacter NewCharacter(NetDataReader reader);
    protected abstract void UpdateScores(NetworkGameScore[] scores);
    protected abstract void KillNotify(string killerName, string victimName, string weaponId);
}
