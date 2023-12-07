using Newtonsoft.Json;

namespace Bot.Services.Telegram
{
    public class Participant
    {
        public long UserId { get; }
        public Status ParticipantStatus { get; set; }

        public Participant(long userId)
        {
            UserId = userId;
            ParticipantStatus = Status.Maybe;
        }

        [JsonConstructor]
        public Participant(long userId, Status participantStatus)
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