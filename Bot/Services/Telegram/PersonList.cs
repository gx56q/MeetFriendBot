using System.Text;
using Newtonsoft.Json.Linq;
using Ydb.Sdk.Value;

namespace Bot.Services.Telegram
{
    public class PersonList
    {
        public Guid Id { get; }
        public string Name { get; set; }
        public List<long> Participants { get; set; }
        public long Creator { get; set; }
        public DbStatus Status { get; set; }

        public PersonList(string name, long creator)
        {
            Id = Guid.NewGuid();
            Name = name;
            Participants = new List<long>();
            Creator = creator;
            Status = DbStatus.Draft;
        }

        private PersonList(Guid id, string name, List<long> participants, DbStatus status = DbStatus.Draft)
        {
            Id = id;
            Name = name;
            Participants = participants;
            Status = status;
        }

        public static PersonList FromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var id = jObject["id"]?.ToObject<Guid>() ?? Guid.NewGuid();
            var name = jObject["name"]?.ToString();
            var participants = jObject["participants"]?.ToObject<List<long>>() ?? new List<long>();
            var status = Enum.Parse<DbStatus>(jObject["status"]?.ToString() ?? "Draft");
            return new PersonList(id, name!, participants, status);
        }

        public void SetStatus(DbStatus status)
        {
            Status = status;
        }

        public static PersonList CreateFromYdb(ResultSet.Row row)
        {
            var id = Encoding.UTF8.GetString(row["id"].GetString());
            var name = Encoding.UTF8.GetString(row["name"].GetString());
            var participants = row["participants"].GetList().Select(p => p.GetStruct()).Select(p =>
                p["user_id"].GetInt64()).ToList();
            return new PersonList(Guid.Parse(id), name, participants, DbStatus.Active);
        }
    }
}