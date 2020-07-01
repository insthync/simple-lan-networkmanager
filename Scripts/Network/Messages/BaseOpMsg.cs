using LiteNetLib.Utils;
using LiteNetLibManager;

public abstract class BaseOpMsg : INetSerializable
{
    public abstract ushort OpId { get; }

    public abstract void Deserialize(NetDataReader reader);

    public abstract void Serialize(NetDataWriter writer);
}