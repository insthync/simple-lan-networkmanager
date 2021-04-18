using MLAPI;
using MLAPI.NetworkVariable;

public abstract class BaseNetworkGameCharacter : NetworkBehaviour, System.IComparable<BaseNetworkGameCharacter>
{
    public static BaseNetworkGameCharacter Local { get; private set; }
    public static ulong LocalNetId { get { return Local ? Local.NetworkObjectId : 0; } }
    public static int LocalRank { get; set; }

    public NetworkVariableString playerName = new NetworkVariableString();
    public NetworkVariableByte playerTeam = new NetworkVariableByte();
    public NetworkVariableInt score = new NetworkVariableInt();
    public NetworkVariableInt killCount = new NetworkVariableInt();
    public NetworkVariableInt assistCount = new NetworkVariableInt();
    public NetworkVariableInt dieCount = new NetworkVariableInt();

    public abstract bool IsDead { get; }
    public abstract bool IsBot { get; }

    public int Score
    {
        get
        {
            if (IsDead && NetworkGameManager != null && NetworkGameManager.gameRule != null && NetworkGameManager.gameRule.ShowZeroScoreWhenDead)
                return 0;
            return score.Value;
        }
    }
    public int KillCount
    {
        get
        {
            if (IsDead && NetworkGameManager != null && NetworkGameManager.gameRule != null && NetworkGameManager.gameRule.ShowZeroKillCountWhenDead)
                return 0;
            return killCount.Value;
        }
    }
    public int AssistCount
    {
        get
        {
            if (IsDead && NetworkGameManager != null && NetworkGameManager.gameRule != null && NetworkGameManager.gameRule.ShowZeroAssistCountWhenDead)
                return 0;
            return assistCount.Value;
        }
    }
    public int DieCount
    {
        get
        {
            if (IsDead && NetworkGameManager != null && NetworkGameManager.gameRule != null && NetworkGameManager.gameRule.ShowZeroDieCountWhenDead)
                return 0;
            return dieCount.Value;
        }
    }

    public BaseNetworkGameManager NetworkGameManager { get { return BaseNetworkGameManager.Singleton; } }

    public virtual bool CanRespawn(params object[] extraParams)
    {
        if (NetworkGameManager != null)
            return NetworkGameManager.CanCharacterRespawn(this, extraParams);
        return true;
    }

    public virtual bool Respawn(params object[] extraParams)
    {
        if (NetworkGameManager != null)
            return NetworkGameManager.RespawnCharacter(this, extraParams);
        return true;
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (Local != null)
            return;
        Local = this;
        LocalRank = 0;
    }

    protected virtual void Update()
    {
        if (NetworkGameManager != null)
            NetworkGameManager.OnUpdateCharacter(this);
    }

    public void ResetScore()
    {
        score.Value = 0;
    }

    public void ResetKillCount()
    {
        killCount.Value = 0;
    }

    public void ResetAssistCount()
    {
        assistCount.Value = 0;
    }

    public void ResetDieCount()
    {
        dieCount.Value = 0;
    }

    public int CompareTo(BaseNetworkGameCharacter other)
    {
        return Score.CompareTo(other.Score) * -10;
    }
}
