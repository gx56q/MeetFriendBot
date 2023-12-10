using Newtonsoft.Json;

namespace Bot.Domain
{
    public class Participant
    {
        public string UserId { get; }
        public Status ParticipantStatus { get; set; }

        public Participant(string userId)
        {
            UserId = userId;
            ParticipantStatus = Status.Maybe;
        }

        [JsonConstructor]
        public Participant(string userId, Status participantStatus)
        {
            UserId = userId;
            ParticipantStatus = participantStatus;
        }
        
        public string GetEmojiForStatus()
        {
            return ParticipantStatus switch
            {
                Status.WillGo => "\ud83d\udc4d",
                Status.WontGo => "\ud83d\udc4e",
                Status.Maybe => "\ud83e\udd37",
                _ => ""
            };
        }
    }

    public enum Status
    {
        WillGo,
        WontGo,
        Maybe
    }
}