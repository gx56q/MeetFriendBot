using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Domain
{
    public class Event : Entity<string>
    {
        [JsonConstructor]
        public Event(
            string id,
            string? name,
            string? description,
            string? location,
            DateTime? date,
            string? picture,
            List<EventParticipant>? participants,
            int? inlinedMessageId,
            long creator,
            EventStatus status = EventStatus.Draft) : base(id)
        {
            Name = name;
            Description = description;
            Location = location;
            Date = date;
            Picture = picture;
            Participants = participants;
            InlinedMessageId = inlinedMessageId;
            CreatorId = creator;
            Status = status;
        }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? Date { get; set; }
        public string? Picture { get; set; }
        public List<EventParticipant>? Participants { get; set; }
        public int? InlinedMessageId { get; set; }
        public long CreatorId { get; set; }
        public EventStatus Status { get; set; }

        public Event(long creatorId) : this(
            id: Guid.NewGuid().ToString(),
            name: null,
            description: null,
            location: null,
            date: null,
            picture: null,
            participants: null,
            inlinedMessageId: null,
            creator: creatorId)
        {
        }

        public Event(string id, string name, string? description, string location,
            DateTime? date, string? picture, List<EventParticipant>? participants, EventStatus status, long creator) : base(id)
        {
            Name = name;
            Description = description;
            Location = location;
            Date = date;
            Picture = picture;
            Participants = participants;
            Status = status;
            CreatorId = creator;
        }

        public static Event FromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var id = jObject["id"].ToString();
            var name = jObject["name"]?.ToString();
            var description = jObject["description"]?.ToString();
            var location = jObject["location"]?.ToObject<string>();
            var date = jObject["date"]?.ToObject<DateTime>();
            var picture = jObject["picture"]?.ToObject<string>();
            var participants = jObject["participants"]?.ToObject<List<EventParticipant>>() ?? new List<EventParticipant>();
            var status = jObject["status"].ToObject<EventStatus>();
            var creator = jObject["creator"].ToObject<long>();
            return new Event(id!, name!, description!, location!, date!, picture, participants, status, creator!);
        }

        public Dictionary<string, string> GetFields()
        {
            var fields = new Dictionary<string, string>();

            var properties = typeof(Event).GetProperties();

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                
                var translatedName = TranslateToRussian(propertyName);

                if (translatedName != null)
                    fields.Add(propertyName, translatedName);
            }

            return fields;
        }

        private static string? TranslateToRussian(string propertyName)
        {
            return propertyName switch
            {
                "Name" => "Название",
                "Description" => "Описание",
                "Location" => "Место проведения",
                "Date" => "Время проведения",
                "Participants" => "Участники",
                "CreatorId" => "Организатор",
                _ => null
            };
        }
    }
}
