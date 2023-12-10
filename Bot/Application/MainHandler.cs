using Bot.Domain;
using Bot.Infrastructure.S3Storage;
using Bot.Infrastructure.YDB;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Application;

public interface IMainHandler
{
    ReplyKeyboardMarkup GetMainKeyboard();
    Task HandleEditAction(CallbackQuery callbackQuery);
}


public class MainHandler : IMainHandler
{
    private readonly IMessageView messageView;
    private readonly IBucket bucket;
    private readonly IBotDatabase botDatabase;
    
    public MainHandler(
        IMessageView messageView,
        IBucket bucket,
        IBotDatabase botDatabase)
    {
        this.messageView = messageView;
        this.bucket = bucket;
        this.botDatabase = botDatabase;
    }
    
    public ReplyKeyboardMarkup GetMainKeyboard()
    {
        var row1 = new KeyboardButton[]
        {
            new("\ud83d\udcc5 Мои встречи"),
        };
        var row2 = new KeyboardButton[]
        {
            new("\ud83c\udfd7 Создать встречу"),
            new("Мои люди")
        };
        var keyboard = new[]
        {
            row1,
            row2
        };
        var replyKeyboardMarkup = new ReplyKeyboardMarkup(keyboard)
        {
            ResizeKeyboard = true
        };
        return replyKeyboardMarkup;
    }

    public async Task HandleEditAction(CallbackQuery callbackQuery)
    { 
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data;
        var field = data!.Split("_")[1];
        
        await bucket.WriteUserState(userId, Enum.Parse<State>("Editing"+field)); 
        var key = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton("Назад") 
        })
        { 
            ResizeKeyboard = true 
        }; 
        switch (field.ToLower())
        { 
            case "listname":
                await messageView.SayWithKeyboard("Отправьте название или нажмите кнопку назад для отмены", 
                    chatId, key);
                   break;
            case "listparticipants":
                await messageView.SayWithKeyboard("Отправьте участников или нажмите кнопку назад для отмены", 
                        chatId, key);
                    break;
            case "name":
                   await messageView.SayWithKeyboard("Отправьте название или нажмите кнопку назад для отмены", 
                       chatId, key);
                   break;
               case "description":
                   await messageView.SayWithKeyboard("Отправьте описание или нажмите кнопку назад для отмены",
                       chatId, key);
                   break;
               case "location":
                   await messageView.SayWithKeyboard("Отправьте место или нажмите кнопку назад для отмены",
                       chatId, key);
                   break;
               case "date":
                   await messageView.SayWithKeyboard("Отправьте дату или нажмите кнопку назад для отмены",
                       chatId, key);
                   break;
               case "picture":
                   var draft = await bucket.GetEventDraft(userId);
                   if (draft.Picture == true)
                   {
                       var deletePhotoKeyboard = new ReplyKeyboardMarkup(new[]
                       {
                           new KeyboardButton("Удалить фото"),
                           new KeyboardButton("Назад")
                       })
                       {
                           ResizeKeyboard = true
                       };
                       await messageView.SayWithKeyboard("Отправьте фото или нажмите кнопку назад для отмены",
                           chatId, deletePhotoKeyboard);
                   }
                   else
                       await messageView.SayWithKeyboard("Отправьте фото или нажмите кнопку назад для отмены", 
                           chatId, key);
                   break;
               case "participants":
                   await messageView.SayWithKeyboard("Отправьте участников или нажмите кнопку назад для отмены",
                       chatId, key);
                   break;
               default:
                   await messageView.Say("Неизвестное поле", chatId);
                   break; 
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }
}