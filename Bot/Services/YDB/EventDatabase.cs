using System.Text;
using Bot.Services.Telegram;
using Ydb.Sdk.Value;

namespace Bot.Services.YDB;

public class EventDatabase : IEventDatabase
{
    protected virtual string UsersTableName => "users";
    protected virtual string EventsTableName => "events";
    protected virtual string PersonListsTableName => "person_lists";
    
    private readonly IBotDatabase botDatabase;

    public EventDatabase(IBotDatabase botDatabase)
    {
        this.botDatabase = botDatabase;
    }

    public static async Task<EventDatabase> InitWithDatabase(IBotDatabase botDatabase)
    {
        var model = new EventDatabase(botDatabase);
        await model.CreateTables();
        return model;
    }
    
    public async Task CreatePersonList(PersonList personList)
    {
        var id = YdbValue.MakeUtf8(personList.Id.ToString());
        var name = YdbValue.MakeUtf8(personList.Name);
        var creator = YdbValue.MakeInt64(personList.Creator);
        var participants = YdbValue.MakeList((IReadOnlyList<YdbValue>)
            personList.Participants.Select(YdbValue.MakeInt64));
        
        await botDatabase.ExecuteModify($@"
            DECLARE $id AS Utf8;
            DECLARE $name AS Utf8;
            DECLARE $description AS Utf8;
            DECLARE $creator AS Int64;
            DECLARE $participants AS List<Int64>;

            UPSERT INTO {PersonListsTableName} ( id, name, description, creator, participants )
            VALUES ( $id, $name, $description, $creator, $participants )

        ", new Dictionary<string, YdbValue>
        {
            { "$id", id },
            { "$name", name },
            { "$creator", creator },
            { "$participants", participants }
        });
    }
    
    public async Task CreateEvent(Event userEvent)
    {
        var id = YdbValue.MakeUtf8(userEvent.Id.ToString());
        var name = YdbValue.MakeUtf8(userEvent.Name);
        var description = YdbValue.MakeUtf8(userEvent.Description);
        var date = YdbValue.MakeDatetime(userEvent.Date ?? DateTime.Now + TimeSpan.FromDays(1));
        var picture = YdbValue.MakeUtf8(userEvent.Picture);
        var participants = YdbValue.MakeList((IReadOnlyList<YdbValue>)userEvent.Participants.Select(p =>
            YdbValue.MakeStruct(new Dictionary<string, YdbValue>
            {
                { "user_id", YdbValue.MakeInt64(p.UserId) },
                { "status", YdbValue.MakeUtf8(p.ParticipantStatus.ToString()) }
            })));
        var creator = YdbValue.MakeInt64(userEvent.Creator);
        
        await botDatabase.ExecuteModify($@"
            DECLARE $id AS Utf8;
            DECLARE $name AS Utf8;
            DECLARE $description AS Utf8;
            DECLARE $date AS DateTime;
            DECLARE $picture AS Utf8;
            DECLARE $participants AS List<Struct<
                user_id: Int64,
                status: Utf8
            >>;
            DECLARE $creator AS Int64;

            UPSERT INTO {EventsTableName} ( id, name, description, date, picture, participants, creator )
            VALUES ( $id, $name, $description, $date, $picture, $participants, $creator )

        ", new Dictionary<string, YdbValue>
        {
            { "$id", id },
            { "$name", name },
            { "$description", description },
            { "$date", date },
            { "$picture", picture },
            { "$participants", participants },
            { "$creator", creator }
        });
    }
    
    public async Task<PersonList?> GetPersonListById(string personListId)
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $person_list_id AS Utf8;

            SELECT id, name, participants, creator
            FROM {PersonListsTableName}
            WHERE id = $person_list_id
        ", new Dictionary<string, YdbValue>
        {
            {"$person_list_id", YdbValue.MakeUtf8(personListId)}
        });

        if (rows is null || !rows.Any())
        {
            return null;
        }
        
        var row = rows.First();
        return PersonList.CreateFromYdb(row);
    }

    public async Task<Event?> GetEventById(string eventId)
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $event_id AS Utf8;

            SELECT id, name, description, date, picture, participants, creator
            FROM {EventsTableName}
            WHERE id = $event_id
        ", new Dictionary<string, YdbValue>
        {
            {"$event_id", YdbValue.MakeUtf8(eventId)}
        });

        if (rows is null || !rows.Any())
        {
            return null;
        }
        
        var row = rows.First();
        return Event.CreateFromYdb(row);
    }

    public async Task<IEnumerable<Dictionary<string, string>>> GetPersonListsByUserId(long userId)
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $user_id AS Int64;

            SELECT person_lists
            FROM {UsersTableName}
            WHERE id = $user_id

        ", new Dictionary<string, YdbValue>
        {
            {"$user_id", YdbValue.MakeInt64(userId)}
        });
        
        if (rows is null || !rows.Any())
        {
            return new List<Dictionary<string, string>>();
        }
        
        var personListsStructs = rows.First()["person_lists"].GetList();
        
        var personLists = personListsStructs.Select(e => e.GetStruct()).ToList();
        var personListsIds = personLists.Select(e => 
            Encoding.UTF8.GetString(e["id"].GetString()));
        var personListsNames = personLists.Select(e =>
            Encoding.UTF8.GetString(e["name"].GetString()));
        
        return personListsIds.Zip(personListsNames, (id, name) => new Dictionary<string, string>
        {
            {id, name}
        });
    }
    
    public async Task<IEnumerable<Dictionary<string, string>>> GetEventsByUserId(long userId)
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $user_id AS Int64;

            SELECT events
            FROM {UsersTableName}
            WHERE id = $user_id
        ", new Dictionary<string, YdbValue>
        {
            {"$user_id", YdbValue.MakeInt64(userId)}
        });

        if (rows is null || !rows.Any())
        {
            return new List<Dictionary<string, string>>();
        }
        
        var eventsStructs = rows.First()["events"].GetList();
        
        var events = eventsStructs.Select(e => e.GetStruct()).ToList();
        var eventsIds = events.Select(e => 
            Encoding.UTF8.GetString(e["id"].GetString()));
        var eventsNames = events.Select(e => 
            Encoding.UTF8.GetString(e["name"].GetString()));
        
        return eventsIds.Zip(eventsNames, (id, name) => new Dictionary<string, string>
        {
            {id, name}
        });
    }

    public async Task ClearAll()
    {
        await botDatabase.ExecuteScheme($@"
            DROP TABLE {EventsTableName};
            DROP TABLE {PersonListsTableName};
            DROP TABLE {UsersTableName};
        ");
    }

    public async Task CreateTables()
    {
        await botDatabase.ExecuteScheme($@"
            CREATE TABLE {EventsTableName} (
                id String NOT NULL,
                name String NOT NULL,
                description String,
                date DateTime NOT NULL,
                picture string,
                participants List<Struct<
                    user_id: Int64,
                    status: String
                >>,
                creator Int64 NOT NULL,
                PRIMARY KEY (id)
            );
            CREATE TABLE {PersonListsTableName} (
                id String NOT NULL,
                name String NOT NULL,
                description String,
                creator Int64 NOT NULL,
                participants List<Int64>,
                PRIMARY KEY (id)
            );
            CREATE TABLE {UsersTableName} (
                id Int64 NOT NULL,
                first_name String NOT NULL,
                last_name String,
                username String,
                events List<Struct<
                    id: String,
                    name: String>>,
                person_lists List<Struct<
                    id: String,
                    name: String>>,
                PRIMARY KEY (id)
            );
        ");
    }
}
