using Newtonsoft.Json.Linq;

namespace Bot.Services.Telegram
{
    public class PersonList
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Participants { get; set; }
        public string Status { get; set; }

        public PersonList(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Participants = new List<string>();
            Status = "draft";
        }

        private PersonList(string id, string name, List<string> participants)
        {
            Id = id;
            Name = name;
            Participants = participants;
            Status = "draft";
        }

        public static PersonList FromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var id = jObject["id"]?.ToString();
            var name = jObject["name"]?.ToString();
            var participants = jObject["participants"]?.ToObject<List<string>>() ?? new List<string>();
            var status = jObject["status"]?.ToString() ?? "draft";
            return new PersonList(id!, name!, participants) { Status = status };
        }

        public void SetStatus(string status)
        {
            Status = status;
        }
    }
}