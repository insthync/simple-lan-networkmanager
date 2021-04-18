using MLAPI.Serialization;

public class OpMsgKillNotify : BaseOpMsg
{
    public const ushort OpId = 10004;

    public string killerName;
    public string victimName;
    public string weaponId;

    public override void Deserialize(NetworkReader reader)
    {
        killerName = reader.ReadString().ToString();
        victimName = reader.ReadString().ToString();
        weaponId = reader.ReadString().ToString();
    }

    public override void Serialize(NetworkWriter writer)
    {
        writer.WriteString(killerName);
        writer.WriteString(victimName);
        writer.WriteString(weaponId);
    }
}
