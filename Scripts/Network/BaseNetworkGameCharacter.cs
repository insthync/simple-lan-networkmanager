using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class BaseNetworkGameCharacter : NetworkBehaviour, System.IComparable<BaseNetworkGameCharacter>
{
    public static BaseNetworkGameCharacter Local { get; private set; }

    [SyncVar]
    public string playerName;
    [SyncVar]
    public int score;
    [SyncVar]
    public int killCount;
    [SyncVar]
    public int assistCount;
    [SyncVar]
    public int dieCount;

    public abstract bool IsDead { get; }
    public int Score
    {
        get
        {
            if (IsDead)
                return 0;
            return score;
        }
    }
    public int KillCount
    {
        get
        {
            if (IsDead)
                return 0;
            return killCount;
        }
    }
    public int AssistCount
    {
        get
        {
            if (IsDead)
                return 0;
            return assistCount;
        }
    }
    public int DieCount
    {
        get
        {
            if (IsDead)
                return 0;
            return dieCount;
        }
    }

    protected BaseNetworkGameManager networkManager;
    public void RegisterNetworkGameManager(BaseNetworkGameManager networkManager)
    {
        this.networkManager = networkManager;
    }

    public bool CanRespawn(params object[] extraParams)
    {
        if (networkManager != null)
            return networkManager.CanCharacterRespawn(this, extraParams);
        return true;
    }
    
    public bool Respawn(params object[] extraParams)
    {
        if (networkManager != null)
            return networkManager.RespawnCharacter(this, extraParams);
        return true;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (Local != null)
            return;

        Local = this;
    }

    public int CompareTo(BaseNetworkGameCharacter other)
    {
        return ((-1 * Score.CompareTo(other.Score)) * 10) + netId.Value.CompareTo(other.netId.Value);
    }
}
