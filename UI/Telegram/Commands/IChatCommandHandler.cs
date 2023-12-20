using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace UI.Telegram.Commands;

public interface IChatCommandHandler
{
    string Command { get; }
    
    Task HandlePlainText(Message message, ReplyKeyboardMarkup? keyboard);
}