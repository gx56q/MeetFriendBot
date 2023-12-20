using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace UI.Telegram.Commands;

public class HelpCommandHandler : IChatCommandHandler
{
    public string Command => "/help";
    
    private readonly IMessageView messageView;
    
    public HelpCommandHandler(IMessageView view)
    {
        messageView = view;
    }
    
    public async Task HandlePlainText(Message message, ReplyKeyboardMarkup? keyboard)
    {
        var fromChatId = message.Chat.Id;
        await messageView.ShowHelp(fromChatId, null);
    }
}