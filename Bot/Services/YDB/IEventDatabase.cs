using Bot.Services.Telegram;

namespace Bot.Services.YDB;

public interface IEventDatabase
{
    Task CreateEvent(Event userEvent);
    Task CreatePersonList(PersonList personList);
    Task<Event?> GetEventById(string guid);
    Task<PersonList?> GetPersonListById(string guid);
    Task<IEnumerable<Dictionary<string, string>>> GetEventsByUserId(long userId);
    Task<IEnumerable<Dictionary<string, string>>> GetPersonListsByUserId(long userId);
}