using Newtonsoft.Json;

namespace Bot.Domain
{
    public class Participant
    {
        public string UserId { get; }
        public UserStatus ParticipantUserStatus { get; set; }

        public Participant(string userId)
        {
            UserId = userId;
            ParticipantUserStatus = UserStatus.Maybe;
        }

        [JsonConstructor]
        public Participant(string userId, UserStatus participantUserStatus)
        {
            UserId = userId;
            ParticipantUserStatus = participantUserStatus;
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