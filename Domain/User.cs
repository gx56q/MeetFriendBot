namespace Domain;

public class User
{
    public long Id { get; set; }
    public long? TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<Event>? Events { get; set; }
    public List<PersonList>? PersonLists { get; set; }
}