using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ydb.Sdk.Value;

namespace Bot.Services.Telegram
{
    public class Event
    {
        // TODO: change from string to normal types
        [JsonConstructor]
        public Event(
            Guid id,
            string? name,
            string? description,
            string? location,
            DateTime? date,
            string? picture,
            List<Participant>? participants,
            int? inlinedMessageId,
            long creator,
            DbStatus status = DbStatus.Draft)
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

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? Date { get; set; }
        public string? Picture { get; set; }
        public List<Participant>? Participants { get; set; }
        public int? InlinedMessageId { get; set; }
        public long Creator { get; set; }
        public DbStatus Status { get; private set; }
        
        public Event(Guid id, string name, string? description, string? location,
            DateTime date, string? picture, List<Participant>? participants, DbStatus status, long creator)
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
        
        public static Event CreateFromCreatorId(long creatorId)
        {
            return new Event(
                id: Guid.NewGuid(),
                name: string.Empty, 
                description: null,
                location: null,
                date: DateTime.Now,
                picture: null,
                participants: null,
                inlinedMessageId: null,
                creator: creatorId);
        }
        
        public static Event CreateFromYdb(ResultSet.Row row)
        {
            var nameBytes = row["name"].GetString();
            var descriptionBytes = row["description"].GetString();
            var locationBytes = row["location"].GetString();
            var pictureBytes = row["picture"].GetString();
            
            var name = Encoding.UTF8.GetString(nameBytes);
            var description = Encoding.UTF8.GetString(descriptionBytes);
            var location = Encoding.UTF8.GetString(locationBytes);
            var picture = Encoding.UTF8.GetString(pictureBytes);
            
            var id = Guid.Parse(Encoding.UTF8.GetString(row["id"].GetString()));
            var datetime = row["date"].GetDatetime();
            var creator = row["creator"].GetInt64();
            var participants = row["participants"].GetList();
            var participantsList = participants.Select(p => 
                new Participant(p.GetStruct()["user_id"].GetInt64(), 
                    Enum.Parse<Status>(Encoding.UTF8.GetString(p.GetStruct()["status"].GetString()))))
                .ToList();
            
            return new Event(id, name, description, location, datetime, picture, participantsList, DbStatus.Active,
                creator);
        }
        
        
        // for tests
        public static Event FromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var id = jObject["id"]?.ToObject<Guid?>();
            var name = jObject["name"]?.ToString();
            var description = jObject["description"]?.ToString();
            var location = jObject["location"]?.ToString();
            var date = jObject["date"]?.ToObject<DateTime>();
            var picture = jObject["picture"]?.ToString();
            var participants = jObject["participants"]?.ToObject<List<Participant>>() ?? new List<Participant>();
            var status = Enum.Parse<DbStatus>(jObject["status"]?.ToString() ?? "draft");
            var creator = jObject["creator"]?.ToObject<long?>();
            return new Event(id ?? Guid.NewGuid(),
                name,
                description, 
                location, 
                date ?? DateTime.Now,
                picture,
                participants,
                status,
                creator ?? 0);
        }

        public Dictionary<string, string> GetFields()
        {
            var fields = new Dictionary<string, string>();

            var properties = typeof(Event).GetProperties();

            foreach (var property in properties)
            {
                var propertyName = property.Name;

                // var propertyValue = property.GetValue(this);

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

        public void SetPicture(string picture)
        {
            Picture = picture;
        }

        public void Activate()
        {
            Status = DbStatus.Active;
        }

        public void SetStatus(DbStatus status)
        {
            Status = status;
        }
    }
}
