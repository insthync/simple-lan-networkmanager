using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class BaseNetworkGameManager : SimpleLanNetworkManager
{
    public BaseNetworkGameRule gameRule;
    public float updateScoreDuration = 1;
    protected float updateScoreTime;
    protected readonly List<BaseNetworkGameCharacter> Characters = new List<BaseNetworkGameCharacter>();
    protected bool canUpdateGameRule;

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

        if (Time.unscaledTime - updateScoreTime >= updateScoreDuration)
        {
            var msgSendScores = new OpMsgSendScores();
            msgSendScores.scores = GetSortedScores();
            NetworkServer.SendToAll(msgSendScores.OpId, msgSendScores);
            updateScoreTime = Time.unscaledTime;
        }
    }

    protected virtual void ClientUpdate()
    {

    }

    public NetworkGameScore[] GetSortedScores()
    {
        Characters.Sort();
        var scores = new NetworkGameScore[Characters.Count];
        for (var i = 0; i < Characters.Count; ++i)
        {
            var character = Characters[i];
            var ranking = new NetworkGameScore();
            ranking.netId = character.netId;
            ranking.playerName = character.playerName;
            ranking.score = character.score;
            ranking.killCount = character.killCount;
            ranking.assistCount = character.assistCount;
            ranking.dieCount = character.dieCount;
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

    public override void OnStartClient(NetworkClient client)
    {
        base.OnStartClient(client);
        client.RegisterHandler(new OpMsgSendScores().OpId, ReadMsgSendScores);
    }

    protected void ReadMsgSendScores(NetworkMessage netMsg)
    {
        var msg = netMsg.ReadMessage<OpMsgSendScores>();
        UpdateScores(msg.scores);
    }

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);
        var msgSendScores = new OpMsgSendScores();
        msgSendScores.scores = GetSortedScores();
        NetworkServer.SendToClient(conn.connectionId, msgSendScores.OpId, msgSendScores);
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

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        canUpdateGameRule = true;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (gameRule != null)
            gameRule.OnStartServer(this);

        // If online scene == offline scene or online scene is empty assume that it can update game rule immediately
        canUpdateGameRule = (string.IsNullOrEmpty(onlineScene) || offlineScene.Equals(onlineScene));
    }

    protected abstract BaseNetworkGameCharacter NewCharacter(NetworkReader extraMessageReader);
    protected abstract void UpdateScores(NetworkGameScore[] scores);
}
