using MLAPI.Serialization;

public class OpMsgSendScores : BaseOpMsg
{
    public const ushort OpId = 10001;

    public NetworkGameScore[] scores;

    public override void Deserialize(NetworkReader reader)
    {
        int length = reader.ReadInt32Packed();
        scores = new NetworkGameScore[length];
        for (int i = 0; i < length; ++i)
        {
            var score = new NetworkGameScore();
            score.Deserialize(reader);
            scores[i] = score;
        }
    }

    public override void Serialize(NetworkWriter writer)
    {
        if (scores == null)
        {
            writer.WriteInt32Packed(0);
            return;
        }
        writer.WriteInt32Packed(scores.Length);
        for (int i = 0; i < scores.Length; ++i)
        {
            scores[i].Serialize(writer);
        }
    }
}
