namespace Bot.Services.Telegram;

public class Matches
{
    private readonly HashSet<string> myEventsMatches = new()
    {
        "\ud83d\udcc5 мои встречи",
        "мои встречи",
        "встречи",
        "мои события",
        "список встреч",
        "список событий",
        "события",
        "мои"
    };
    
    private readonly HashSet<string> newEventMatches = new()
    {
        "\ud83c\udfd7 создать встречу",
        "создать встречу",
        "новая встреча",
        "новое событие",
        "встреча",
        "новая",
        "создать"
    };
    
    private readonly HashSet<string> myPeopleMatches = new()
    {
        "мои люди",
        "люди"
    };
}