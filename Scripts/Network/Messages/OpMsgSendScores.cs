using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using LiteNetLib.Utils;

public class OpMsgSendScores : BaseOpMsg
{
    public override ushort OpId
    {
        get
        {
            return 10001;
        }
    }

    public NetworkGameScore[] scores;

    public override void Deserialize(NetDataReader reader)
    {
        int length = reader.GetPackedInt();
        scores = new NetworkGameScore[length];
        for (int i = 0; i < length; ++i)
        {
            var score = new NetworkGameScore();
            score.Deserialize(reader);
            scores[i] = score;
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        if (scores == null)
        {
            writer.PutPackedInt(0);
            return;
        }
        writer.PutPackedInt(scores.Length);
        for (int i = 0; i < scores.Length; ++i)
        {
            scores[i].Serialize(writer);
        }
    }
}
