using Domain;

namespace Infrastructure.S3Storage;

public interface IBucket
{
    Task<State> GetUserState(long userId);
    Task WriteUserState(long userId, State state);
    Task WriteDraft<T>(long userId, T draft);
    Task<Event> GetEventDraft(long userId);
    Task<PersonList> GetPersonListDraft(long userId);
    Task ClearDraft(long userId);
    Task<string?> GetEventPicture(string eventId);
    Task WriteEventPicture(string eventId, string picture);
    Task DeleteEventPicture(string eventId);
}
