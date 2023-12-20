namespace Domain;

public class SimpleEvent: ISimple
{
    public string Id { get; set; }
    public string Name { get; set; }
    
    public SimpleEvent(string id, string name)
    {
        Id = id;
        Name = name;
    }
}