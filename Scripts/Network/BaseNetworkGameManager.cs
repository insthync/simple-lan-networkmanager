using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class BaseNetworkGameManager : SimpleLanNetworkManager
{
    public static new BaseNetworkGameManager Singleton
    {
        get { return singleton as BaseNetworkGameManager; }
    }
    public static event System.Action<int> onClientError;

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
        if (NetworkServer.active)
            ServerUpdate();
        if (NetworkClient.active)
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
                NetworkServer.SendToAll(msgSendScores.OpId, msgSendScores);
            }
            updateScoreTime = Time.unscaledTime;
        }

        if (gameRule != null && Time.unscaledTime - updateMatchTime >= 1f)
        {
            RemainsMatchTime = gameRule.RemainsMatchTime;
            var msgMatchStatus = new OpMsgMatchStatus();
            msgMatchStatus.remainsMatchTime = gameRule.RemainsMatchTime;
            msgMatchStatus.isMatchEnded = gameRule.IsMatchEnded;
            NetworkServer.SendToAll(msgMatchStatus.OpId, msgMatchStatus);

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
        if (!NetworkServer.active)
            return;

        var msgKillNotify = new OpMsgKillNotify();
        msgKillNotify.killerName = killerName;
        msgKillNotify.victimName = victimName;
        msgKillNotify.weaponId = weaponId;
        NetworkServer.SendToAll(msgKillNotify.OpId, msgKillNotify);
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
            ranking.netId = character.netId;
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

    public override void OnStartClient(NetworkClient client)
    {
        base.OnStartClient(client);
        client.RegisterHandler(new OpMsgSendScores().OpId, ReadMsgSendScores);
        client.RegisterHandler(new OpMsgGameRule().OpId, ReadMsgGameRule);
        client.RegisterHandler(new OpMsgMatchStatus().OpId, ReadMsgMatchStatus);
        client.RegisterHandler(new OpMsgKillNotify().OpId, ReadMsgKillNotify);
        if (gameRule != null)
            gameRule.InitialClientObjects(client);
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        base.OnClientError(conn, errorCode);
        if (onClientError != null)
            onClientError(errorCode);
    }

    protected void ReadMsgSendScores(NetworkMessage netMsg)
    {
        var msg = netMsg.ReadMessage<OpMsgSendScores>();
        UpdateScores(msg.scores);
    }

    protected void ReadMsgGameRule(NetworkMessage netMsg)
    {
        var msg = netMsg.ReadMessage<OpMsgGameRule>();
        BaseNetworkGameRule foundGameRule;
        if (BaseNetworkGameInstance.GameRules.TryGetValue(msg.gameRuleName, out foundGameRule))
        {
            gameRule = foundGameRule;
            gameRule.InitialClientObjects(client);
        }
    }

    protected void ReadMsgMatchStatus(NetworkMessage netMsg)
    {
        var msg = netMsg.ReadMessage<OpMsgMatchStatus>();
        RemainsMatchTime = msg.remainsMatchTime;
        if (!IsMatchEnded && msg.isMatchEnded)
        {
            IsMatchEnded = true;
            MatchEndedAt = Time.unscaledTime;
        }
    }

    protected void ReadMsgKillNotify(NetworkMessage netMsg)
    {
        var msg = netMsg.ReadMessage<OpMsgKillNotify>();
        KillNotify(msg.killerName, msg.victimName, msg.weaponId);
    }

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);
        if (gameRule == null || !gameRule.IsMatchEnded)
        {
            var msgSendScores = new OpMsgSendScores();
            msgSendScores.scores = GetSortedScores();
            NetworkServer.SendToClient(conn.connectionId, msgSendScores.OpId, msgSendScores);
        }
        if (gameRule != null)
        {
            var msgGameRule = new OpMsgGameRule();
            msgGameRule.gameRuleName = gameRule.name;
            NetworkServer.SendToClient(conn.connectionId, msgGameRule.OpId, msgGameRule);
            var msgMatchTime = new OpMsgMatchStatus();
            msgMatchTime.remainsMatchTime = gameRule.RemainsMatchTime;
            msgMatchTime.isMatchEnded = gameRule.IsMatchEnded;
            NetworkServer.SendToClient(conn.connectionId, msgMatchTime.OpId, msgMatchTime);
        }
    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
    {
        var character = NewCharacter(extraMessageReader);
        if (character == null)
        {
            Debug.LogError("Cannot create new character for player " + conn.connectionId);
            return;
        }
        NetworkServer.AddPlayerForConnection(conn, character.gameObject, playerControllerId);
        RegisterCharacter(character);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        DestroyPlayersForConnection(conn);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Characters.Clear();
    }

    public void DestroyPlayersForConnection(NetworkConnection conn)
    {
        var playerControllers = conn.playerControllers;
        foreach (var playerController in playerControllers)
        {
            var character = playerController.gameObject.GetComponent<BaseNetworkGameCharacter>();
            if (character != null)
                Characters.Remove(character);
        }
        NetworkServer.DestroyPlayersForConnection(conn);
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        base.OnClientSceneChanged(conn);
        if (gameRule != null)
            gameRule.InitialClientObjects(client);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        canUpdateGameRule = true;
        if (gameRule != null && client != null)
            gameRule.InitialClientObjects(client);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (gameRule != null)
            gameRule.OnStartServer(this);

        updateScoreTime = 0f;
        updateMatchTime = 0f;
        RemainsMatchTime = 0f;
        IsMatchEnded = false;
        MatchEndedAt = 0f;
        // If online scene == offline scene or online scene is empty assume that it can update game rule immediately
        canUpdateGameRule = (string.IsNullOrEmpty(onlineScene) || offlineScene.Equals(onlineScene));
    }

    protected abstract BaseNetworkGameCharacter NewCharacter(NetworkReader extraMessageReader);
    protected abstract void UpdateScores(NetworkGameScore[] scores);
    protected abstract void KillNotify(string killerName, string victimName, string weaponId);
}
