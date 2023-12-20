using Newtonsoft.Json;

namespace Domain
{
    public class EventParticipant : Entity<long>

    {
    public UserStatus ParticipantUserStatus { get; set; }
    public string? ParticipantUsername { get; set; } = "";
    public string? ParticipantFirstName { get; set; } = "Участник";

    public EventParticipant(long userId) : base(userId)
    {
        ParticipantUserStatus = UserStatus.Maybe;
    }

    [JsonConstructor]
    public EventParticipant(long userId, UserStatus participantUserStatus, string participantUsername,
        string participantFirstName) : base(userId)
    {
        ParticipantUserStatus = participantUserStatus;
        ParticipantUsername = participantUsername;
    }


    public string GetEmojiForStatus()
    {
        return ParticipantUserStatus switch
        {
            UserStatus.WillGo => "\ud83d\udc4d",
            UserStatus.WontGo => "\ud83d\udc4e",
            UserStatus.Maybe => "\ud83e\udd37",
            _ => ""
        };
    }
    }
}