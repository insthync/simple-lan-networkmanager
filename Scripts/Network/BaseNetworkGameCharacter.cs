using LiteNetLibManager;

public abstract class BaseNetworkGameCharacter : LiteNetLibBehaviour, System.IComparable<BaseNetworkGameCharacter>
{
    public static BaseNetworkGameCharacter Local { get; private set; }
    public static uint LocalNetId { get { return Local ? Local.ObjectId : 0; } }
    public static int LocalRank { get; set; }

    [SyncField]
    public string playerName;
    [SyncField]
    public byte playerTeam;
    [SyncField]
    public int score;
    [SyncField]
    public int killCount;
    [SyncField]
    public int assistCount;
    [SyncField]
    public int dieCount;

    public abstract bool IsDead { get; }
    public abstract bool IsBot { get; }

    public int Score
    {
        get
        {
            if (IsDead && NetworkManager != null && NetworkManager.gameRule != null && NetworkManager.gameRule.ShowZeroScoreWhenDead)
                return 0;
            return score;
        }
    }
    public int KillCount
    {
        get
        {
            if (IsDead && NetworkManager != null && NetworkManager.gameRule != null && NetworkManager.gameRule.ShowZeroKillCountWhenDead)
                return 0;
            return killCount;
        }
    }
    public int AssistCount
    {
        get
        {
            if (IsDead && NetworkManager != null && NetworkManager.gameRule != null && NetworkManager.gameRule.ShowZeroAssistCountWhenDead)
                return 0;
            return assistCount;
        }
    }
    public int DieCount
    {
        get
        {
            if (IsDead && NetworkManager != null && NetworkManager.gameRule != null && NetworkManager.gameRule.ShowZeroDieCountWhenDead)
                return 0;
            return dieCount;
        }
    }

    public BaseNetworkGameManager NetworkManager { get; protected set; }
    public void RegisterNetworkGameManager(BaseNetworkGameManager networkManager)
    {
        NetworkManager = networkManager;
    }

    public virtual bool CanRespawn(params object[] extraParams)
    {
        if (NetworkManager != null)
            return NetworkManager.CanCharacterRespawn(this, extraParams);
        return true;
    }

    public virtual bool Respawn(params object[] extraParams)
    {
        if (NetworkManager != null)
            return NetworkManager.RespawnCharacter(this, extraParams);
        return true;
    }

    public override void OnStartOwnerClient()
    {
        if (Local != null)
            return;
        Local = this;
        LocalRank = 0;
        NetworkManager = Manager as BaseNetworkGameManager;
    }

    protected virtual void Update()
    {
        if (NetworkManager != null)
            NetworkManager.OnUpdateCharacter(this);
    }

    public void ResetScore()
    {
        score = 0;
    }

    public void ResetKillCount()
    {
        killCount = 0;
    }

    public void ResetAssistCount()
    {
        assistCount = 0;
    }

    public void ResetDieCount()
    {
        dieCount = 0;
    }

    public int CompareTo(BaseNetworkGameCharacter other)
    {
        if (NetworkManager.RankedByKillCount)
            return (KillCount.CompareTo(other.KillCount) * -100) + (AssistCount.CompareTo(other.AssistCount) * -10);
        else
            return Score.CompareTo(other.Score) * -10;
    }
}
