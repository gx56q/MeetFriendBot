using Domain;
using Infrastructure.S3Storage;
using Infrastructure.YDB;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace UI.Telegram.AppLogic;

public interface IMainHandler
{
    ReplyKeyboardMarkup GetMainKeyboard();
    Task HandleEditAction(CallbackQuery callbackQuery);
}


public class MainHandler : IMainHandler
{
    private readonly IMessageView messageView;
    private readonly IBucket bucket;
    private readonly EventRepo eventRepo;
    
    public MainHandler(
        IMessageView messageView,
        IBucket bucket,
        EventRepo eventRepo)
    {
        this.messageView = messageView;
        this.bucket = bucket;
        this.eventRepo = eventRepo;
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
                await messageView.SayWithKeyboard("Отправьте username участников через запятую, пробел или новую строку," +
                                                  " или нажмите кнопку назад для отмены", 
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
                   if (draft.Picture is null)
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