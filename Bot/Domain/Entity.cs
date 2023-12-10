namespace Bot.Domain;

public class Entity<TId, TStatus>
{
    public TId Id;
    public TStatus Status;

    public Entity(TId id, TStatus status)
    {
        Id = id;
        Status = status;
    }
}