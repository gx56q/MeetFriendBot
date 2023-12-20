namespace Domain;

public class Entity<TId>
{
    public TId Id;

    public Entity(TId id)
    {
        Id = id;
    }
}