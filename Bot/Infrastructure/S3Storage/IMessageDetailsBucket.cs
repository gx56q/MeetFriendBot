using Telegram.Bot.Types;

namespace Bot.Infrastructure.S3Storage;

public interface IBucket
{
    Task<Domain.State> GetUserState(long userId);
    Task WriteUserState(long userId, Domain.State state);
    Task WriteEventDraft(long userId, Domain.Event draft);
    Task<Domain.Event> GetEventDraft(long userId);
    Task ClearEventDraft(long userId);
    Task<string?> GetEventPicture(string eventId);
    Task WriteEventPicture(string eventId, string picture);
    Task DeleteEventPicture(string eventId);
}
