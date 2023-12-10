using Ydb.Sdk.Value;

namespace Bot.Infrastructure.YDB;

public class EventRepo
{
    public virtual string TableName => "message_dates";

    private readonly IBotDatabase botDatabase;

    public EventRepo(IBotDatabase botDatabase)
    {
        this.botDatabase = botDatabase;
    }

    public static async Task<EventRepo> InitWithDatabase(IBotDatabase botDatabase)
    {
        var model = new EventRepo(botDatabase);
        await model.CreateTable();
        return model;
    }

    public async Task<ulong> GetPastWeekUsersCount()
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $current_date AS Date;

            SELECT COUNT(*) AS user_count
            FROM {TableName} view idx_lmd
            WHERE last_message_date >= DateTime::MakeDatetime(DateTime::StartOfWeek($current_date));
        ", new Dictionary<string, YdbValue>
        {
            {"$current_date", YdbValue.MakeDate(DateTime.Now)},
        });
        
        return rows?.First()["user_count"].GetUint64() ?? 0;
    }

    public async void UpdateOrInsertDateTime(long chatId, DateTime? dateTime = null)
    {
        await botDatabase.ExecuteModify($@"
            DECLARE $chat_id AS Int64;
            DECLARE $date_time AS DateTime;

            UPSERT INTO {TableName} ( chat_id, last_message_date )
            VALUES ( $chat_id, $date_time )
        ", new Dictionary<string, YdbValue>
        {
            {"$chat_id", YdbValue.MakeInt64(chatId)},
            {"$date_time", YdbValue.MakeDatetime(dateTime ?? DateTime.Now)},
        });
    }

    public async Task<DateTime?> FindLastMessageDateTime(long chatId)
    {
        var rows = await botDatabase.ExecuteFind($@"
            DECLARE $chat_id AS Int64;

            SELECT last_message_date
            FROM {TableName}
            WHERE chat_id = $chat_id
        ", new Dictionary<string, YdbValue>
        {
            {"$chat_id", YdbValue.MakeInt64(chatId)}
        });

        if (rows is null || !rows.Any())
        {
            return null;
        }
        
        return rows.First()["last_message_date"].GetOptionalDatetime();
    }

    public async Task ClearAll()
    {
        await botDatabase.ExecuteScheme($@"
            DROP TABLE {TableName};
        ");
    }

    public async Task CreateTable()
    {
        await botDatabase.ExecuteScheme($@"
            CREATE TABLE {TableName} (
                chat_id Int64 NOT NULL,
                last_message_date DateTime,
                INDEX idx_lmd GLOBAL ON (last_message_date),
                PRIMARY KEY (chat_id)
            )
        ");
    }
}
