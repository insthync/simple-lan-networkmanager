using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpMsgMatchStatus : BaseOpMsg
{
    public override short OpId
    {
        get
        {
            return 10003;
        }
    }

    public float remainsMatchTime;
    public bool isMatchEnded;
}
