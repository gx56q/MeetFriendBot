using Bot.Domain;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Application.Commands;

public class HelpCommandHandler : IChatCommandHandler
{
    public string Command => "/help";
    private readonly IMessageView messageView;
    
    public HelpCommandHandler(IMessageView view)
    {
        messageView = view;
    }
    
    public async Task HandlePlainText(string text, long fromChatId, ReplyKeyboardMarkup? keyboard)
    {
        await messageView.ShowHelp(fromChatId, null);
    }
}