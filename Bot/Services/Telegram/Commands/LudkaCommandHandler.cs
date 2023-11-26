using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Services.Telegram.Commands;

public class LudkaCommandHandler : IChatCommandHandler
{
    public string Command => "/ludka";
    private readonly IMessageView messageView;
    
    public LudkaCommandHandler(IMessageView view)
    {
        messageView = view;
    }
    
    public async Task HandlePlainText(string text, long fromChatId, ReplyKeyboardMarkup? keyboard)
    {
        await messageView.SendDice(fromChatId, Emoji.SlotMachine);
    }
}