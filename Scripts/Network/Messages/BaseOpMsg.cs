using UnityEngine.Networking;

public abstract class BaseOpMsg : MessageBase
{
    public abstract short OpId { get; }
}