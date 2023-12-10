using Newtonsoft.Json;

namespace Bot.Domain;

public class UserState
{
    private State currentState = State.Start;
    
    public string ToJson()
    {
        return JsonConvert.SerializeObject(currentState);
    }
    
    public static UserState? FromJson(string json)
    {
        return JsonConvert.DeserializeObject<UserState>(json);
    }
}