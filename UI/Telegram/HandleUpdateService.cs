using System.Text.RegularExpressions;
using Domain;
using Infrastructure.Api.Geocode;
using Infrastructure.Api.Maps;
using Infrastructure.Api.Taxi;
using Infrastructure.S3Storage;
using Infrastructure.YDB;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UI.Telegram.AppLogic;
using UI.Telegram.Commands;

namespace UI.Telegram;

public class HandleUpdateService
{
    private readonly IMessageView messageView;
    private readonly IChatCommandHandler[] commands;
    private readonly IBucket bucket;
    private readonly EventRepo eventRepo;

    private readonly MyEventsHandler myEventsHandler;
    private readonly MyListsHandler myListsHandler;
    private readonly IMainHandler mainHandler;
    
    public HandleUpdateService(
        IMessageView messageView, 
        IChatCommandHandler[] commands,
        IBucket bucket,
        IBotDatabase botDatabase,
        IGeocodeApi geocodeApi,
        ITaxiApi taxi,
        IMapsApi mapsApi)
    {
        this.messageView = messageView;
        this.commands = commands;
        this.bucket = bucket;
        
        eventRepo = EventRepo.InitWithDatabase(botDatabase).Result;
        mainHandler = new MainHandler(messageView, bucket, eventRepo);
        myEventsHandler = new MyEventsHandler(messageView, bucket, eventRepo, geocodeApi, taxi, mapsApi,
            mainHandler);
        myListsHandler = new MyListsHandler(messageView, bucket, eventRepo, mainHandler);
    }

    public async Task Handle(Update update)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage => BotOnMessageReceived(update.EditedMessage!),
            UpdateType.CallbackQuery => BotOnCallbackQuery(update.CallbackQuery!),
            _ => UnknownUpdateHandlerAsync()
        };
        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(update, exception);
        }
    }
    
    private async Task HandleNonCommandMessage(long fromChatId)
    {
        await messageView.Say("Я не понимаю, что вы хотите", fromChatId);
    }
    
    private async Task BotOnMessageReceived(Message message) 
    {
        if (message.Type == MessageType.Text)
            if (message.ForwardFrom != null)
                // TODO: ask to add to contact list
                await HandleForward(message);
            else
                await HandlePlainText(message);
        else if (message.Type == MessageType.Photo)
            await HandlePhoto(message);
    }

    private async Task HandlePhoto(Message message)
    {
        var state = await bucket.GetUserState(message.From!.Id);
        if (state == State.EditingPicture)
            await myEventsHandler.EditPicture(message);
        else
            await messageView.Say("Я не понимаю, что вы хотите", message.Chat.Id);
    }
        
    private async Task HandleForward(Message message)
    {
        var forwardFromId = message.ForwardFrom!.Id;
        await messageView.Say("Спасибо за пересылку!", forwardFromId);
    }
    
    private async Task HandlePlainText(Message message)
    {
        var text = message.Text;
        if (text == null) return;
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;
        
        var command = commands.FirstOrDefault(c => text.StartsWith(c.Command));
        if (command != null)
        {
            if (command is StartCommandHandler)
            {
                var username = message.From!.Username;
                var firstName = message.From.FirstName;
                var lastName = message.From.LastName;
                var user = new Domain.User
                {
                    Id = userId,
                    TelegramId = userId,
                    Username = username,
                    FirstName = firstName,
                    LastName = lastName
                };
                await eventRepo.CreateUser(user);
            }
            await command.HandlePlainText(message, mainHandler.GetMainKeyboard());
            return;
        }
        
        var state = await bucket.GetUserState(userId);
        
        switch (state)
        {
            case State.EditingList:
                await myListsHandler.ActionInterrupted(message);
                return;
            case State.CreatingList:
                await myListsHandler.ActionInterrupted(message);
                return;
            case State.EditingListParticipants:
                await myListsHandler.EditListParticipants(message);
                return;
            case State.EditingListName:
                await myListsHandler.EditListName(message);
                return;
            case State.EditingEvent:
                // TODO: show event edit message with keyboard
                await myEventsHandler.ActionInterrupted(message, state);
                return;
            case State.EditingPicture:
                await myEventsHandler.EditPicture(message);
                return;
            case State.EditingName:
                await myEventsHandler.EditName(message);
                return;
            case State.EditingDescription:
                await myEventsHandler.EditDescription(message);
                return;
            case State.EditingLocation:
                await myEventsHandler.EditLocation(message);
                return;
            case State.EditingDate:
               await myEventsHandler.EditDate(message); 
               return;
            case State.EditingParticipants:
                await myEventsHandler.EditParticipants(message);
                return;
            case State.CreatingEvent:
                await myEventsHandler.ActionInterrupted(message, state);
                return;
        }
        
        if (Matches.myEventsMatches.Contains(text.ToLower()))
        {
            await myEventsHandler.HandleMyEvents(message);
            return;
        }
        if (Matches.newEventMatches.Contains(text.ToLower()))
        {
            await myEventsHandler.HandleNewEvent(message);
            return;
        }
        if (Matches.myPeopleMatches.Contains(text.ToLower()))
        {
            await myListsHandler.HandleMyPeopleCommand(message);
            return;
        }
        
        await HandleNonCommandMessage(fromChatId);
    }

    private async Task BotOnCallbackQuery(CallbackQuery callbackQuery)
    {
        var callbackData = callbackQuery.Data;
        if (callbackData == null) return;
        var action = callbackData.Split('_')[0];
        switch (action)
        {
            case "willGo":
                await myEventsHandler.HandleWillGoAction(callbackQuery);
                break;
            case "changePageEvents":
                await myEventsHandler.HandleNextPageAction(callbackQuery);
                break;
            case "changePagePeople":
                await myListsHandler.HandleNextPageAction(callbackQuery);
                break;
            case "showEvent":
                await myEventsHandler.HandleViewEventAction(callbackQuery);
                break;
            case "edit":
                await HandleEditAction(callbackQuery);
                break;
            case "showPersonList":
                await myListsHandler.HandleViewPersonListAction(callbackQuery);
                break;
            case "editPersonList":
                await myListsHandler.HandleEditPersonListAction(callbackQuery);
                break;
            case "createPersonList":
                await myListsHandler.HandleCreatePersonListAction(callbackQuery);
                break;
            case "createEvent":
                await myEventsHandler.HandleCreateEventAction(callbackQuery);
                break;
            case "backMyEvents":
                await myEventsHandler.HandleBackAction(callbackQuery);
                break;
            case "backMyPeople":
                await myListsHandler.HandleBackAction(callbackQuery);
                break;
            case "editEvent":
                await myEventsHandler.HandleEditEventAction(callbackQuery);
                break;
            case "cancelEvent":
                await myEventsHandler.HandleCancelEventAction(callbackQuery);
                break;
            case "saveEvent":
                await myEventsHandler.HandleSaveEventAction(callbackQuery);
                break;
            case "addToCalendar":
                await myEventsHandler.HandleAddToCalendarAction(callbackQuery);
                break;
            case "savePersonList":
                await myListsHandler.HandleSavePersonList(callbackQuery);
                break;
            default:
                await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
                break;
        }
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
                                                  "чтобы добавить людей из  списка, добавьте '!' вначале" +
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
                   await messageView.SayWithKeyboard("Отправьте место (поставьте '!' в начале, чтобы мы не искали это место на карте) или нажмите кнопку назад для отмены",
                       chatId, key);
                   break;
               case "date":
                   await messageView.SayWithKeyboard("Отправьте дату в формате  dd.MM.yyyy HH:mm:ss, dd.MM.yyyy, dd.MM.yyyy HH:mm" + 
                                                     " или нажмите кнопку назад для отмены",
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
                   await messageView.SayWithKeyboard("Отправьте username участников через запятую, пробел или новую строку," +
                                                     "чтобы добавить людей из  списка, добавьте '!' вначале" +
                                                     " или нажмите кнопку назад для отмены", 
                       chatId, key);
                   break;
               default:
                   await messageView.Say("Неизвестное поле", chatId);
                   break; 
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }
    
    private static Task UnknownUpdateHandlerAsync() => Task.CompletedTask;

    private async Task HandleErrorAsync(Update incomingUpdate, Exception exception)
    {
        try
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            }; 
            await messageView.ShowErrorToDevops(incomingUpdate, errorMessage);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.ToString());
        }
    }
}