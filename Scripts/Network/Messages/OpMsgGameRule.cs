using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class OpMsgGameRule : BaseOpMsg
{
    public override short OpId
    {
        get
        {
            return 10002;
        }
    }

    public string gameRuleName;
}
