using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bot.Services.Telegram
{
    public class Event
    {
        // TODO: change from string to normal types
        [JsonConstructor]
        public Event(
            string id,
            string? name,
            string? description,
            string? location,
            string? date,
            bool? picture,
            List<Participant>? participants,
            int? inlinedMessageId,
            string creator,
            string status = "draft")
        {
            Id = id;
            Name = name;
            Description = description;
            Location = location;
            Date = date;
            Picture = picture;
            Participants = participants;
            InlinedMessageId = inlinedMessageId;
            Creator = creator;
            Status = status;
        }

        public string Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Date { get; set; }
        public bool? Picture { get; set; }

        public List<Participant>? Participants { get; set; }
        public int? InlinedMessageId { get; set; }

        public string Creator { get; set; }
        public string Status = "draft";

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

        public Event(string id, string name, string? description, string location,
            string date, bool? picture, List<Participant> participants, string status, string creator)
        {
            Id = id;
            Name = name;
            Description = description;
            Location = location;
            Date = date;
            Picture = picture;
            Participants = participants;
            Status = status;
            Creator = creator;
        }

        public static Event FromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var id = jObject["id"]?.ToString();
            var name = jObject["name"]?.ToString();
            var description = jObject["description"]?.ToString();
            var location = jObject["location"]?.ToString();
            var date = jObject["date"]?.ToString();
            var picture = jObject["picture"]?.ToObject<bool?>();
            var participants = jObject["participants"]?.ToObject<List<Participant>>() ?? new List<Participant>();
            var status = jObject["status"]?.ToString();
            var creator = jObject["creator"]?.ToString();
            return new Event(id!, name!, description, location!, date!, picture, participants, status!, creator!);
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

        public void SetPicture(bool? picture)
        {
            Picture = picture;
        }

        public void Activate()
        {
            Status = "active";
        }

        public void SetStatus(string status)
        {
            Status = status;
        }
    }
}