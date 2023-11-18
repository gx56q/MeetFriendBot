using Bot.Services.Telegram;
using Telegram.Bot.Types;

namespace Bot.Services.S3Storage;

public interface IBucket
{
    Task<State> GetUserState(long userId);
    Task WriteUserState(long userId, State state);
    Task WriteEventDraft(long userId, Event draft);
    Task<Event> GetEventDraft(long userId);
    Task ClearEventDraft(long userId);
    Task<string?> GetEventPicture(string eventId);
    Task WriteEventPicture(string eventId, string picture);
    Task DeleteEventPicture(string eventId);
}