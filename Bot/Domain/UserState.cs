using Newtonsoft.Json;

namespace Bot.Domain;

public class UserState
{
    private State curState = State.Start;
    
    public string ToJson()
    {
        return JsonConvert.SerializeObject(curState);
    }
    
    public static UserState? FromJson(string json)
    {
        return JsonConvert.DeserializeObject<UserState>(json);
    }
    
    public void SetState(State state)
    {
        curState = state;
    }
}