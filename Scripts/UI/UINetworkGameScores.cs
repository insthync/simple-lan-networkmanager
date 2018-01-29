using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINetworkGameScores : MonoBehaviour
{
    public UINetworkGameScoreEntry[] userRankings;
    public UINetworkGameScoreEntry localRanking;

    public void UpdateRankings(NetworkGameScore[] rankings)
    {
        for (var i = 0; i < userRankings.Length; ++i)
        {
            var userRanking = userRankings[i];
            if (i < rankings.Length)
            {
                var ranking = rankings[i];
                userRanking.SetData(i + 1, ranking);

                var isLocal = BaseNetworkGameCharacter.Local != null && ranking.netId.Equals(BaseNetworkGameCharacter.Local.netId);
                if (isLocal)
                    UpdateLocalRank(i + 1, ranking);
            }
            else
                userRanking.Clear();
        }
    }

    public void UpdateLocalRank(int rank, NetworkGameScore ranking)
    {
        if (localRanking != null)
            localRanking.SetData(rank, ranking);
    }
}
