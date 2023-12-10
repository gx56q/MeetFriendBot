using Newtonsoft.Json.Linq;

namespace Bot.Domain
{
    public class PersonList
    {
        public string Id { get; }
        public string Name { get; set; }
        public List<Participant> Participants { get; set; }
        public EventStatus Status { get; set; }

        public PersonList(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Participants = new List<Participant>();
            Status = EventStatus.Draft;
        }

        private PersonList(string id, string name, List<Participant> participants)
        {
            Id = id;
            Name = name;
            Participants = participants;
            Status = EventStatus.Draft;
        }

        public static PersonList FromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var id = jObject["id"]?.ToString();
            var name = jObject["name"]?.ToString();
            var participantsIdString = jObject["participants"]?.ToObject<List<string>>() ?? new List<string>();
            var participants = new List<Participant>();
            if (participantsIdString.Count != 0)
                participants.AddRange(participantsIdString.Select(participant => new Participant(participant)));
            var status = jObject["status"]?.ToString() ?? "draft";
            return status == "draft" ? new PersonList(id!, name!, participants) { Status = EventStatus.Draft }
                : new PersonList(id!, name!, participants) { Status = EventStatus.Active };
        }

        public void SetStatus(EventStatus status)
        {
            Status = status;
        }
    }
}