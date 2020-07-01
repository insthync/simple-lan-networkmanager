using LiteNetLib.Utils;
using LiteNetLibManager;

[System.Serializable]
public struct NetworkGameScore : INetSerializable
{
    public static readonly NetworkGameScore Empty = new NetworkGameScore();
    public uint netId;
    public string playerName;
    public int score;
    public int killCount;
    public int assistCount;
    public int dieCount;

    public void Deserialize(NetDataReader reader)
    {
        netId = reader.GetPackedUInt();
        playerName = reader.GetString();
        score = reader.GetPackedInt();
        killCount = reader.GetPackedInt();
        assistCount = reader.GetPackedInt();
        dieCount = reader.GetPackedInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutPackedUInt(netId);
        writer.Put(playerName);
        writer.PutPackedInt(score);
        writer.PutPackedInt(killCount);
        writer.PutPackedInt(assistCount);
        writer.PutPackedInt(dieCount);
    }
}
