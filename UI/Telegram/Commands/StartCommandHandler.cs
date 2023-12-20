using Infrastructure.YDB;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace UI.Telegram.Commands;

public class StartCommandHandler : IChatCommandHandler
{
    public string Command => "/start";
    
    private readonly IMessageView messageView;

    private const string StartMessage = "Привет!\nЯ Meet Mate, ваш дружелюбный помощник в организации встреч и событий! \ud83e\udd1d\ud83c\udf89" +
                                        "\nВместе мы сделаем планирование и координацию ваших встреч легким и приятным." +
                                        "\nВведите /help, чтобы узнать о моих возможностях и начать планировать!";

    public StartCommandHandler(IMessageView view)
    {
        messageView = view;
    }

    public async Task HandlePlainText(Message message, ReplyKeyboardMarkup? keyboard)
    {
        var fromChatId = message.Chat.Id;
        if (keyboard != null)
            await messageView.SayWithKeyboard(StartMessage, fromChatId, keyboard);
        else
            await messageView.Say(StartMessage, fromChatId);
    }
}