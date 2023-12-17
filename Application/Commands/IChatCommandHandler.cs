using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Commands;

public interface IChatCommandHandler
{
    string Command { get; }
    
    Task HandlePlainText(string text, long fromChatId, ReplyKeyboardMarkup? keyboard);
}