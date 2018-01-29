using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINetworkGameScores : MonoBehaviour
{
    public UINetworkGameScoreEntry[] userRankings;
    public UINetworkGameScoreEntry localRanking;

    public void UpdateRankings(NetworkGameScore[] rankings)
    {
        var i = 0;
        for (; i < rankings.Length; ++i)
        {
            var ranking = rankings[i];
            if (i < userRankings.Length)
            {
                var userRanking = userRankings[i];
                userRanking.SetData(i + 1, ranking);
            }

            var isLocal = BaseNetworkGameCharacter.Local != null && ranking.netId.Equals(BaseNetworkGameCharacter.Local.netId);
            if (isLocal)
                UpdateLocalRank(i + 1, ranking);
        }

        for (; i < userRankings.Length; ++i)
        {
            var userRanking = userRankings[i];
            userRanking.Clear();
        }
    }

    public void UpdateLocalRank(int rank, NetworkGameScore ranking)
    {
        if (localRanking != null)
            localRanking.SetData(rank, ranking);
    }
}
