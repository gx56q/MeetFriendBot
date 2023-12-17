using Bot.Domain;
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
            Location? location,
            Date? date,
            Picture? picture,
            List<Participant>? participants,
            int? inlinedMessageId,
            string creator,
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
        public Location? Location { get; set; }
        public Date? Date { get; set; }
        public Picture? Picture { get; set; }
        public List<Participant>? Participants { get; set; }
        public int? InlinedMessageId { get; set; }
        public string CreatorId { get; set; }
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
            creator: creatorId.ToString())
        {
        }

        public Event(string id, string name, string? description, Location location,
            Date? date, Picture? picture, List<Participant>? participants, EventStatus status, string creator) : base(id)
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
            var location = jObject["location"]?.ToObject<Location?>();
            var date = jObject["date"]?.ToObject<Date?>();
            var picture = jObject["picture"]?.ToObject<Picture?>();
            var participants = jObject["participants"]?.ToObject<List<Participant>>() ?? new List<Participant>();
            var status = jObject["status"].ToObject<EventStatus>();
            var creator = jObject["creator"]?.ToString();
            return new Event(id!, name!, description!, location!, date!, picture, participants, status, creator!);
        }

        public Dictionary<string, string> GetFields()
        {
            var fields = new Dictionary<string, string>();

            var properties = typeof(Event).GetProperties();

            foreach (var property in properties)
            {
                var propertyName = property.Name;

                var propertyValue = property.GetValue(this);

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
                "Picture" => "Фото",
                "Participants" => "Участники",
                "Creator" => "Организатор",
                _ => null
            };
        }
    }
}
