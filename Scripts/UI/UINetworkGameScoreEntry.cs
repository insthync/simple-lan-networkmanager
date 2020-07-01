using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINetworkGameScoreEntry : MonoBehaviour
{
    public Text textRank;
    public Text textName;
    public Text textScore;
    public Text textKillCount;
    public Text textAssistCount;
    public Text textDieCount;
    public Color normalTextColor = Color.white;
    public Color localTextColor = Color.yellow;
    public void SetData(int rank, NetworkGameScore ranking)
    {
        Clear();
        if (ranking.Equals(NetworkGameScore.Empty) || ranking.netId == 0)
            return;
        if (textRank != null)
            textRank.text = "#" + rank;
        if (textName != null)
            textName.text = ranking.playerName;
        if (textScore != null)
            textScore.text = ranking.score.ToString("N0");
        if (textKillCount != null)
            textKillCount.text = ranking.killCount.ToString("N0");
        if (textAssistCount != null)
            textAssistCount.text = ranking.killCount.ToString("N0");
        if (textDieCount != null)
            textDieCount.text = ranking.killCount.ToString("N0");

        var isLocal = BaseNetworkGameCharacter.Local != null && ranking.netId.Equals(BaseNetworkGameCharacter.Local.ObjectId);
        SetTextColor(isLocal, textRank);
        SetTextColor(isLocal, textName);
        SetTextColor(isLocal, textScore);
        SetTextColor(isLocal, textKillCount);
    }

    public void Clear()
    {
        if (textRank != null)
            textRank.text = "";
        if (textName != null)
            textName.text = "";
        if (textScore != null)
            textScore.text = "";
        if (textKillCount != null)
            textKillCount.text = "";
    }

    private void SetTextColor(bool isLocal, Text text)
    {
        if (text == null)
            return;
        text.color = isLocal ? localTextColor : normalTextColor;
    }
}
