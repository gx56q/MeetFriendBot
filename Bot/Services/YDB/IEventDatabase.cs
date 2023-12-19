using Bot.Services.Telegram;

namespace Bot.Services.YDB;

public interface IEventDatabase
{
    Task CreateEvent(Event userEvent);
    Task CreatePersonList(PersonList personList);
    Task<Event?> GetEventById(string guid);
    Task<PersonList?> GetPersonListById(string guid);
    Task<IEnumerable<ISimple>> GetEventsByUserId(long userId);
    Task<IEnumerable<ISimple>> GetPersonListsByUserId(long userId);
}