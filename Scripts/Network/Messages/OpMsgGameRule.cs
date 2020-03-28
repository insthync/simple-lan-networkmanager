using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using LiteNetLib.Utils;

public class OpMsgGameRule : BaseOpMsg
{
    public override ushort OpId
    {
        get
        {
            return 10002;
        }
    }

    public string gameRuleName;

    public override void Deserialize(NetDataReader reader)
    {
        gameRuleName = reader.GetString();
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(gameRuleName);
    }
}
