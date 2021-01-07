using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using LiteNetLib;
using LiteNetLib.Utils;
using Cysharp.Threading.Tasks;

public abstract class BaseNetworkGameManager : SimpleLanNetworkManager
{
    public static new BaseNetworkGameManager Singleton
    {
        get { return singleton as BaseNetworkGameManager; }
    }
    public static event System.Action<DisconnectInfo> onClientDisconnected;

    public BaseNetworkGameRule gameRule;
    protected float updateScoreTime;
    protected float updateMatchTime;
    protected bool canUpdateGameRule;
    public readonly List<BaseNetworkGameCharacter> Characters = new List<BaseNetworkGameCharacter>();
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

    protected override void Update()
    {
        base.Update();
        if (IsServer)
            ServerUpdate();
        if (IsClient)
            ClientUpdate();
    }

    protected virtual void ServerUpdate()
    {
        if (gameRule != null && canUpdateGameRule)
            gameRule.OnUpdate();

        if (Time.unscaledTime - updateScoreTime >= 1f)
        {
            if (gameRule == null || !gameRule.IsMatchEnded)
            {
                var msgSendScores = new OpMsgSendScores();
                msgSendScores.scores = GetSortedScores();
                ServerSendPacketToAllConnections(DeliveryMethod.ReliableOrdered, msgSendScores.OpId, msgSendScores);
            }
            updateScoreTime = Time.unscaledTime;
        }

        if (gameRule != null && Time.unscaledTime - updateMatchTime >= 1f)
        {
            RemainsMatchTime = gameRule.RemainsMatchTime;
            var msgMatchStatus = new OpMsgMatchStatus();
            msgMatchStatus.remainsMatchTime = gameRule.RemainsMatchTime;
            msgMatchStatus.isMatchEnded = gameRule.IsMatchEnded;
            ServerSendPacketToAllConnections(DeliveryMethod.ReliableOrdered, msgMatchStatus.OpId, msgMatchStatus);

            if (!IsMatchEnded && gameRule.IsMatchEnded)
            {
                IsMatchEnded = true;
                MatchEndedAt = Time.unscaledTime;
            }

            updateMatchTime = Time.unscaledTime;
        }
    }

    protected virtual void ClientUpdate()
    {

    }

    public void SendKillNotify(string killerName, string victimName, string weaponId)
    {
        if (!IsServer)
            return;

        var msgKillNotify = new OpMsgKillNotify();
        msgKillNotify.killerName = killerName;
        msgKillNotify.victimName = victimName;
        msgKillNotify.weaponId = weaponId;
        ServerSendPacketToAllConnections(DeliveryMethod.ReliableOrdered, msgKillNotify.OpId, msgKillNotify);
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
            var ranking = new NetworkGameScore();
            ranking.netId = character.ObjectId;
            ranking.playerName = character.playerName;
            ranking.score = character.Score;
            ranking.killCount = character.KillCount;
            ranking.assistCount = character.AssistCount;
            ranking.dieCount = character.DieCount;
            scores[i] = ranking;
        }
        return scores;
    }

    public void RegisterCharacter(BaseNetworkGameCharacter character)
    {
        if (character == null || Characters.Contains(character))
            return;
        character.RegisterNetworkGameManager(this);
        Characters.Add(character);
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

    public void OnUpdateCharacter(BaseNetworkGameCharacter character)
    {
        if (gameRule != null)
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

    protected override void RegisterClientMessages()
    {
        base.RegisterClientMessages();
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
            ServerSendPacket(requestHandler.ConnectionId, DeliveryMethod.ReliableOrdered, msgSendScores.OpId, msgSendScores);
        }
        if (gameRule != null)
        {
            var msgGameRule = new OpMsgGameRule();
            msgGameRule.gameRuleName = gameRule.name;
            ServerSendPacket(requestHandler.ConnectionId, DeliveryMethod.ReliableOrdered, msgGameRule.OpId, msgGameRule);
            var msgMatchTime = new OpMsgMatchStatus();
            msgMatchTime.remainsMatchTime = gameRule.RemainsMatchTime;
            msgMatchTime.isMatchEnded = gameRule.IsMatchEnded;
            ServerSendPacket(requestHandler.ConnectionId, DeliveryMethod.ReliableOrdered, msgMatchTime.OpId, msgMatchTime);
        }
        return base.HandleClientReadyRequest(requestHandler, request, result);
    }

    public override void SerializeClientReadyData(NetDataWriter writer)
    {
        base.SerializeClientReadyData(writer);
        PrepareCharacter(writer);
    }

    public override async UniTask<bool> DeserializeClientReadyData(LiteNetLibIdentity playerIdentity, long connectionId, NetDataReader reader)
    {
        await UniTask.Yield();
        var character = NewCharacter(reader);
        if (character == null)
        {
            Debug.LogError("Cannot create new character for player " + connectionId);
            return false;
        }
        Assets.NetworkSpawn(character.gameObject, 0, connectionId);
        RegisterCharacter(character);
        return true;
    }

    public override void OnClientDisconnected(DisconnectInfo disconnectInfo)
    {
        base.OnClientDisconnected(disconnectInfo);
        if (onClientDisconnected != null)
            onClientDisconnected.Invoke(disconnectInfo);
    }

    public override void OnPeerDisconnected(long connectionId, DisconnectInfo disconnectInfo)
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
        base.OnPeerDisconnected(connectionId, disconnectInfo);
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
