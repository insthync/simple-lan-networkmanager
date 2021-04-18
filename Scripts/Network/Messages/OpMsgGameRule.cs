using MLAPI.Serialization;

public class OpMsgGameRule : BaseOpMsg
{
    public const ushort OpId = 10002;

    public string gameRuleName;

    public override void Deserialize(NetworkReader reader)
    {
        gameRuleName = reader.ReadString().ToString();
    }

    public override void Serialize(NetworkWriter writer)
    {
        writer.WriteString(gameRuleName);
    }
}
