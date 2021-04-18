using MLAPI.Serialization;

public class OpMsgMatchStatus : BaseOpMsg
{
    public const ushort OpId = 10003;

    public float remainsMatchTime;
    public bool isMatchEnded;

    public override void Deserialize(NetworkReader reader)
    {
        remainsMatchTime = reader.ReadSingle();
        isMatchEnded = reader.ReadBool();
    }

    public override void Serialize(NetworkWriter writer)
    {
        writer.WriteSingle(remainsMatchTime);
        writer.WriteBool(isMatchEnded);
    }
}
