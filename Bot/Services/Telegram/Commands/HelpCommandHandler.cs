using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Services.Telegram.Commands;

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

    public async Task HandlePlainTextWithKeyboard(string text, long fromChatId, ReplyKeyboardMarkup keyboard)
    {
        await messageView.ShowHelpWithKeyboard(fromChatId, keyboard);
    }
}