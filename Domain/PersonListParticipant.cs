using Newtonsoft.Json;

namespace Domain;

public class PersonListParticipant: Entity<long>
{
    public string? ParticipantUsername { get; set; }
    public string? ParticipantFirstName { get; set; }
    
    public PersonListParticipant(long userId) : base(userId)
    {
    }
    
    [JsonConstructor]
    public PersonListParticipant(long userId, string? participantUsername,
        string? participantFirstName) : base(userId)
    {
        ParticipantUsername = participantUsername;
        ParticipantFirstName = participantFirstName;
    }
}