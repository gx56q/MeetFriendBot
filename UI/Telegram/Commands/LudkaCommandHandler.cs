using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace UI.Telegram.Commands;

public class LudkaCommandHandler : IChatCommandHandler
{
    public string Command => "/ludka";
    private readonly IMessageView messageView;
    
    public LudkaCommandHandler(IMessageView view)
    {
        messageView = view;
    }
    
    public async Task HandlePlainText(Message message, ReplyKeyboardMarkup? keyboard)
    {
        var fromChatId = message.Chat.Id;
        await messageView.SendDice(fromChatId, Emoji.SlotMachine);
    }
}