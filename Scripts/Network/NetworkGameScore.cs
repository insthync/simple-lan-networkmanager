using MLAPI.Serialization;

[System.Serializable]
public struct NetworkGameScore
{
    public static readonly NetworkGameScore Empty = new NetworkGameScore();
    public ulong netId;
    public string playerName;
    public byte team;
    public int score;
    public int killCount;
    public int assistCount;
    public int dieCount;

    public void Deserialize(NetworkReader reader)
    {
        netId = reader.ReadUInt64Packed();
        playerName = reader.ReadString().ToString();
        team = reader.ReadByteDirect();
        score = reader.ReadInt32Packed();
        killCount = reader.ReadInt32Packed();
        assistCount = reader.ReadInt32Packed();
        dieCount = reader.ReadInt32Packed();
    }

    public void Serialize(NetworkWriter writer)
    {
        writer.WriteUInt64Packed(netId);
        writer.WriteString(playerName);
        writer.WriteByte(team);
        writer.WriteInt32Packed(score);
        writer.WriteInt32Packed(killCount);
        writer.WriteInt32Packed(assistCount);
        writer.WriteInt32Packed(dieCount);
    }
}
