namespace Bot.Services.YDB;

public class SimplePersonList: ISimple
{
    public string Id { get; set; }
    public string Name { get; set; }
    
    public SimplePersonList(string id, string name)
    {
        Id = id;
        Name = name;
    }
}