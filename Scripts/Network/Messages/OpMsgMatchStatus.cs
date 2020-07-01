using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;

public class OpMsgMatchStatus : BaseOpMsg
{
    public override ushort OpId
    {
        get
        {
            return 10003;
        }
    }

    public float remainsMatchTime;
    public bool isMatchEnded;

    public override void Deserialize(NetDataReader reader)
    {
        remainsMatchTime = reader.GetFloat();
        isMatchEnded = reader.GetBool();
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(remainsMatchTime);
        writer.Put(isMatchEnded);
    }
}
