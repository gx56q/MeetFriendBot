using Newtonsoft.Json.Linq;

namespace Bot.Domain
{
    public class PersonList: Entity<string, string>
    {
        public string Id { get; }
        public string Name { get; set; }
        public List<string> Participants { get; set; }
        public string Status { get; set; }

        public PersonList(string name, string id = null, string status = "draft")
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