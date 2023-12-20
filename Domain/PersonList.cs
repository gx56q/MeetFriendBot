using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Domain
{
    public class PersonList : Entity<string>
    {
        public string? Name { get; set; }
        public List<PersonListParticipant>? Participants { get; set; }
        public long CreatorId { get; set; }
        public EventStatus Status { get; set; }
        public int? InlinedMessageId { get; set; }
        
        [JsonConstructor]
        public PersonList(string id, string? name, List<PersonListParticipant>? participants, long creatorId, 
            EventStatus status, int? inlinedMessageId) : base(id)
        {
            Name = name;
            Participants = participants;
            CreatorId = creatorId;
            Status = status;
            InlinedMessageId = inlinedMessageId;
        }

        public PersonList(long creatorId) : base(Guid.NewGuid().ToString())
        {
            CreatorId = creatorId;
            Name = null;
            Participants = null;
            Status = EventStatus.Draft;
        }

        public PersonList(string id, string name, List<PersonListParticipant> participants, long creatorId, 
            EventStatus status) : base(id)
        {
            Name = name;
            Participants = participants;
            CreatorId = creatorId;
            Status = status;
        }
    }
}