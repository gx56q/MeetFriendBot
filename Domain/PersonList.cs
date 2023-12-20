using Newtonsoft.Json.Linq;

namespace Domain
{
    public class PersonList : Entity<string>
    {
        public string Name { get; set; }
        // TODO: store PersonListParticipants instead of long
        public List<long> Participants { get; set; }
        public long CreatorId { get; set; }
        public EventStatus Status { get; set; }

        public PersonList(string name) : base(Guid.NewGuid().ToString())
        {
            Name = name;
            Participants = new List<long>();
            Status = EventStatus.Draft;
        }

        public PersonList(string id, string name, List<long> participants, long creatorId, 
            EventStatus status) : base(id)
        {
            Name = name;
            Participants = participants;
            CreatorId = creatorId;
            Status = status;
        }

        public static PersonList FromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var id = jObject["id"]?.ToString();
            var name = jObject["name"]?.ToString();
            var participants = jObject["participants"]?.ToObject<List<long>>() ?? new List<long>();
            var status = Enum.Parse<EventStatus>(jObject["status"]?.ToString() ?? "Draft");
            var creator = jObject["creator"]?.ToObject<long>() ?? 0;
            return new PersonList(id!, name!, participants, creator, status);
        }
    }
}