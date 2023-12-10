using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Application.Commands;

public interface IChatCommandHandler
{
    string Command { get; }
    
    Task HandlePlainText(string text, long fromChatId, ReplyKeyboardMarkup? keyboard);
}