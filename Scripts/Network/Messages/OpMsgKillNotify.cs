using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using LiteNetLib.Utils;

public class OpMsgKillNotify : BaseOpMsg
{
    public override ushort OpId
    {
        get
        {
            return 10004;
        }
    }

    public string killerName;
    public string victimName;
    public string weaponId;

    public override void Deserialize(NetDataReader reader)
    {
        killerName = reader.GetString();
        victimName = reader.GetString();
        weaponId = reader.GetString();
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(killerName);
        writer.Put(victimName);
        writer.Put(weaponId);
    }
}
