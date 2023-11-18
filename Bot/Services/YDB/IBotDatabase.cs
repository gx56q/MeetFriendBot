using System.Collections.Generic;
using Ydb.Sdk.Value;

namespace Bot.Services.YDB;

public interface IBotDatabase
{
    Task ExecuteScheme(string query);
    
    Task<IEnumerable<ResultSet.Row>?> ExecuteFind(
        string query, Dictionary<string, YdbValue> parameters);

    Task ExecuteModify(string query, Dictionary<string, YdbValue> parameters);
}