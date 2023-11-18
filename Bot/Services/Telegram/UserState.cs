using Newtonsoft.Json;

namespace Bot.Services.Telegram;

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

public enum State
{
    Start,
    CreatingEvent,
    EditingEvent,
    EditingName,
    EditingDescription,
    EditingLocation,
    EditingDate,
    EditingPicture,
    EditingParticipants,
    EditingList,
    EditingListName,
    EditingListParticipants
}