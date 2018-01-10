using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class OpMsgSendScores : BaseOpMsg
{
    public override short OpId
    {
        get
        {
            return 10001;
        }
    }

    public NetworkGameScore[] scores;
}
