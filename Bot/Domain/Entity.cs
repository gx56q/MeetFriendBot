namespace Bot.Domain;

public class Entity<TId, TStatus>
{
    public TId Id;
    public TStatus Status;
}