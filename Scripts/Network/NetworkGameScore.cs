using UnityEngine.Networking;

[System.Serializable]
public struct NetworkGameScore
{
    public static readonly NetworkGameScore Empty = new NetworkGameScore();
    public NetworkInstanceId netId;
    public string playerName;
    public int score;
    public int killCount;
    public int assistCount;
    public int dieCount;
}
