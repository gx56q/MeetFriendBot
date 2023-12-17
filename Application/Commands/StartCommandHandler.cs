using Domain;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Commands;

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

    public async Task HandlePlainText(string text, long fromChatId, ReplyKeyboardMarkup? keyboard)
    {
        if (keyboard != null)
            await messageView.SayWithKeyboard(StartMessage, fromChatId, keyboard);
        else
            await messageView.Say(StartMessage, fromChatId);
    }
    
    public async Task HandlePlainTextWithKeyboard(string text, long fromChatId, ReplyKeyboardMarkup keyboard)
    {
        await messageView
            .SayWithKeyboard(StartMessage, fromChatId, keyboard);
    }
}