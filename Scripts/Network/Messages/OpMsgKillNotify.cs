using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class OpMsgKillNotify : BaseOpMsg
{
    public override short OpId
    {
        get
        {
            return 10004;
        }
    }

    public string killerName;
    public string victimName;
    public string weaponId;
}
