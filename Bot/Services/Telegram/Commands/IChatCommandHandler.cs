using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Services.Telegram.Commands;

public interface IChatCommandHandler
{
    string Command { get; }
    
    Task HandlePlainText(string text, long fromChatId, ReplyKeyboardMarkup? keyboard);
}