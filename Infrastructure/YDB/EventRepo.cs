using System.Text;
using Domain;
using Newtonsoft.Json;
using Ydb.Sdk.Value;

namespace Infrastructure.YDB;

public class EventRepo
{
    protected virtual string UsersTableName => "users";
    protected virtual string EventsTableName => "events";
    protected virtual string PersonListsTableName => "person_lists";
    
    private readonly IBotDatabase botDatabase;

    public EventRepo(IBotDatabase botDatabase)
    {
        this.botDatabase = botDatabase;
    }

    public static async Task<EventRepo> InitWithDatabase(IBotDatabase botDatabase)
    {
        var model = new EventRepo(botDatabase);
        // await model.CreateTables();
        return model;
    }
    
    public async Task CreateUser(User user)
    {
        var id = YdbValue.MakeInt64(user.Id);
        var telegramId = YdbValue.MakeOptionalInt64(user.TelegramId);
        var firstName = YdbValue.MakeOptionalUtf8(user.FirstName);
        var lastName = YdbValue.MakeOptionalUtf8(user.LastName);
        var username = YdbValue.MakeOptionalUtf8(user.Username);
        
        await botDatabase.ExecuteModify($@"
            DECLARE $id AS Int64;
            DECLARE $telegram_id AS Int64?;
            DECLARE $first_name AS Utf8?;
            DECLARE $last_name AS Utf8?;
            DECLARE $username AS Utf8?;

            UPSERT INTO {UsersTableName} ( id, telegram_id, first_name, last_name, username )
            VALUES ( $id, $telegram_id, $first_name, $last_name, $username )

        ", new Dictionary<string, YdbValue>
        {
            { "$id", id },
            { "$telegram_id", telegramId },
            { "$first_name", firstName },
            { "$last_name", lastName },
            { "$username", username }
        });
    }

    public async Task<List<User>> FilterValidUsersByUsernames(IEnumerable<string> rawUsers)
    {
        var users = new List<User>();
        foreach (var rawUser in rawUsers)
        {
            var user = await GetUserByUsername(rawUser);
            if (user != null)
                users.Add(user);
        }
        return users;
    }

    public async Task<User?> GetUserByUsername(string username)
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $username AS Utf8;

            SELECT id, telegram_id, first_name, last_name, username
            FROM {UsersTableName}
            WHERE username = $username
        ", new Dictionary<string, YdbValue>
        {
            {"$username", YdbValue.MakeUtf8(username)}
        });

        if (rows is null || !rows.Any())
        {
            return null;
        }
        
        var row = rows.First();
        
        var id = row["id"].GetInt64();
        var telegramId = row["telegram_id"].GetOptionalInt64();
        var firstName = row["first_name"].GetOptionalUtf8();
        var lastName = row["last_name"].GetOptionalUtf8();
        return new User
        {
            Id = id,
            TelegramId = telegramId,
            FirstName = firstName,
            LastName = lastName,
            Username = username
        };
    }
    
    public async Task PushPersonList(PersonList personList)
    {
        var id = YdbValue.MakeUtf8(personList.Id);
        var name = YdbValue.MakeOptionalUtf8(personList.Name);
        var creator = YdbValue.MakeInt64(personList.CreatorId);
        var participants = YdbValue.MakeOptionalJson(JsonConvert.SerializeObject(personList.Participants));
        
        await botDatabase.ExecuteModify($@"
            DECLARE $id AS Utf8;
            DECLARE $name AS Utf8?;
            DECLARE $description AS Utf8?;
            DECLARE $creator AS Int64;
            DECLARE $participants AS Json?;

            UPSERT INTO {PersonListsTableName} ( id, name, description, creator, participants )
            VALUES ( $id, $name, $description, $creator, $participants )

        ", new Dictionary<string, YdbValue>
        {
            { "$id", id },
            { "$name", name },
            { "$creator", creator },
            { "$participants", participants }
        });
        await UpdateUserPersonList(personList);
    }
    
    private async Task UpdateUserPersonList(PersonList userEvent)
    {
        var simplePersonList = new SimplePersonList(userEvent.Id, userEvent.Name);
        var userPersonLists = await GetPersonListsByUserId(userEvent.CreatorId);
        var userPersonListsList = userPersonLists.ToList();
        var sameEvent = userPersonListsList.FirstOrDefault(e => e.Id == userEvent.Id);
        if (sameEvent != null)
        {
            userPersonListsList.Remove(sameEvent);
        }
        userPersonListsList.Add(simplePersonList);
        await PushUserPersonLists(userEvent.CreatorId, userPersonListsList);
    }
    
    private async Task PushUserPersonLists(long userId, IEnumerable<ISimple> personLists)
    {
        var personListsJson = JsonConvert.SerializeObject(personLists);
        var personListsJsonYdb = YdbValue.MakeOptionalJson(personListsJson);
        
        await botDatabase.ExecuteModify($@"
            DECLARE $user_id AS Int64;
            DECLARE $person_lists AS Json?;

            UPSERT INTO {UsersTableName} ( id, person_lists )
            VALUES ( $user_id, $person_lists )

        ", new Dictionary<string, YdbValue>
        {
            { "$user_id", YdbValue.MakeInt64(userId) },
            { "$person_lists", personListsJsonYdb }
        });
    }

    public async Task<List<User>> GetUsersFromPersonList(string name, long userId)
    {
        var personList = await GetPersonListByName(name, userId);
        if (personList is null)
        {
            return new List<User>();
        }

        var participants = personList.Participants;
        if (participants is null)
        {
            return new List<User>();
        }

        return participants.Select(participant =>
            new User { Id = participant.Id, FirstName = participant.ParticipantFirstName, 
                Username = participant.ParticipantUsername, TelegramId = participant.Id }).ToList();
    }

    private async Task<PersonList?> GetPersonListByName(string listName, long userId)
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $name AS Utf8;
            DECLARE $user_id AS Int64;

            SELECT id, name, participants, creator
            FROM {PersonListsTableName}
            WHERE name = $name AND creator = $user_id
        ", new Dictionary<string, YdbValue>
        {
            {"$name", YdbValue.MakeUtf8(listName)},
            {"$user_id", YdbValue.MakeInt64(userId)}
        });

        if (rows is null || !rows.Any())
        {
            return null;
        }
        
        var row = rows.First();
        
        var id = row["id"].GetUtf8();
        var name = row["name"].GetOptionalUtf8();
        var participantsJson = row["participants"].GetOptionalJson();
        var participants = participantsJson is null ?
            new List<PersonListParticipant>() :
            JsonConvert.DeserializeObject<List<PersonListParticipant>>(participantsJson);
        var creator = row["creator"].GetInt64();
        return new PersonList(id, name, participants, creator, EventStatus.Active);
    }

    
    public async Task PushEvent(Event userEvent)
    {
        var id = YdbValue.MakeUtf8(userEvent.Id);
        var name = YdbValue.MakeOptionalUtf8(userEvent.Name);
        var description = YdbValue.MakeOptionalUtf8(userEvent.Description);
        var date = YdbValue.MakeOptionalDatetime(userEvent.Date);
        var location = YdbValue.MakeOptionalJson(JsonConvert.SerializeObject(userEvent.Location));
        var picture = YdbValue.MakeOptionalUtf8(userEvent.Picture);
        var participants = YdbValue.MakeOptionalJson(JsonConvert.SerializeObject(userEvent.Participants));
        var creator = YdbValue.MakeInt64(userEvent.CreatorId);
        
        await botDatabase.ExecuteModify($@"
            DECLARE $id AS Utf8;
            DECLARE $name AS Utf8?;
            DECLARE $description AS Utf8?;
            DECLARE $date AS Datetime?;
            DECLARE $location AS Json?;
            DECLARE $picture AS Utf8?;
            DECLARE $participants AS Json?;
            DECLARE $creator AS Int64;

            UPSERT INTO {EventsTableName} ( id, name, description, date, location, picture, participants, creator )
            VALUES ( $id, $name, $description, $date, $location, $picture, $participants, $creator )

        ", new Dictionary<string, YdbValue>
        {
            { "$id", id },
            { "$name", name },
            { "$description", description },
            { "$date", date },
            { "$location", location },
            { "$picture", picture },
            { "$participants", participants },
            { "$creator", creator }
        });
        await UpdateUsersEvents(userEvent);
    }

    private async Task UpdateUsersEvents(Event userEvent)
    {
        var simpleEvent = new SimpleEvent(userEvent.Id, userEvent.Name);
        var participantsIds = userEvent.Participants?.Select(e => e.Id).ToList() 
                              ?? new List<long>();
        var enumerable = participantsIds.Append(userEvent.CreatorId);

        foreach (var userId in enumerable)
        {
            var userEvents = await GetEventsByUserId(userId);
            var userEventsList = userEvents.ToList();
            var sameEvent = userEventsList.FirstOrDefault(e => e.Id == userEvent.Id);
            if (sameEvent != null)
            {
                userEventsList.Remove(sameEvent);
            }
            userEventsList.Add(simpleEvent);
            await PushUserEvents(userId, userEventsList);
        }
    }
    
    private async Task PushUserEvents(long userId, IEnumerable<ISimple> events)
    {
        var eventsJson = JsonConvert.SerializeObject(events);
        var eventsJsonYdb = YdbValue.MakeOptionalJson(eventsJson);
        
        await botDatabase.ExecuteModify($@"
            DECLARE $user_id AS Int64;
            DECLARE $events AS Json?;

            UPSERT INTO {UsersTableName} ( id, events )
            VALUES ( $user_id, $events )

        ", new Dictionary<string, YdbValue>
        {
            { "$user_id", YdbValue.MakeInt64(userId) },
            { "$events", eventsJsonYdb }
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
        
        var id = row["id"].GetUtf8();
        var name = row["name"].GetOptionalUtf8();
        var participantsJson = row["participants"].GetOptionalJson();
        var participants = participantsJson is null ?
            new List<PersonListParticipant>() :
            JsonConvert.DeserializeObject<List<PersonListParticipant>>(participantsJson);
        var creator = row["creator"].GetInt64();
        return new PersonList(id, name, participants, creator, EventStatus.Active);
    }

    public async Task<Event?> GetEventById(string eventId)
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $event_id AS Utf8;

            SELECT id, name, description, date, location, picture, participants, creator
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
        
        
        var id = row["id"].GetUtf8();
        var name = row["name"].GetOptionalUtf8();
        var description = row["description"].GetOptionalUtf8();
        var locationJson = row["location"].GetOptionalJson();
        var location = locationJson is null ?
            new Location("") :
            JsonConvert.DeserializeObject<Location>(locationJson);
        var picture = row["picture"].GetOptionalUtf8();
        var datetime = row["date"].GetOptionalDatetime();
        var creator = row["creator"].GetInt64();
        var participants = row["participants"].GetOptionalJson();
        var participantsList = participants is null ?
            new List<EventParticipant>() :
            JsonConvert.DeserializeObject<List<EventParticipant>>(participants);
            
        return new Event(id, name, description, location, datetime, picture, participantsList, EventStatus.Active,
            creator);
    }

    public async Task<IEnumerable<ISimple>> GetPersonListsByUserId(long userId)
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
            return new List<ISimple>();
        }

        var personListsJson = rows.First()["person_lists"].GetOptionalJson();
        
        var personLists = JsonConvert.DeserializeObject<List<SimplePersonList>>(personListsJson) 
                          ?? new List<SimplePersonList>();
        return personLists;
    }
    
    public async Task<IEnumerable<ISimple>> GetEventsByUserId(long userId)
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
            return new List<ISimple>();
        }

        var eventsJson = rows.First()["events"].GetOptionalJson();
        
        var events = JsonConvert.DeserializeObject<List<SimpleEvent>>(eventsJson) 
                     ?? new List<SimpleEvent>();
        return events;
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
                id Utf8 NOT NULL,
                name Utf8,
                description Utf8,
                date Datetime,
                location Json,
                picture Utf8,
                participants Json,
                creator Int64 NOT NULL,
                PRIMARY KEY (id)
            );
            CREATE TABLE {PersonListsTableName} (
                id Utf8 NOT NULL,
                name Utf8,
                description Utf8,
                creator Int64 NOT NULL,
                participants Json,
                PRIMARY KEY (id)
            );
            CREATE TABLE {UsersTableName} (
                id Int64 NOT NULL,
                telegram_id Int64,
                first_name Utf8,
                last_name Utf8,
                username Utf8,
                events Json,
                person_lists Json,
                PRIMARY KEY (id)
            );
        ");
    }
}
