using MLAPI.Serialization;

public abstract class BaseOpMsg
{
    public abstract void Deserialize(NetworkReader reader);

    public abstract void Serialize(NetworkWriter writer);
}