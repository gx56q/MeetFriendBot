using System.Text.RegularExpressions;
using Bot.Services.S3Storage;
using Bot.Services.Telegram.Commands;
using Bot.Services.YDB;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bot.Services.Telegram;

public class HandleUpdateService
{
    private readonly IMessageView messageView;
    private readonly IChatCommandHandler[] commands;
    private readonly IBucket bucket;

    private readonly MyEventsHandler myEventsHandler;
    private readonly MyListsHandler myListsHandler;
    private readonly IMainHandler mainHandler;
    
    public HandleUpdateService(
        IMessageView messageView, 
        IChatCommandHandler[] commands,
        IBucket bucket,
        IBotDatabase botDatabase)
    {
        this.messageView = messageView;
        this.commands = commands;
        this.bucket = bucket;
        
        mainHandler = new MainHandler(messageView, bucket, botDatabase);
        myEventsHandler = new MyEventsHandler(messageView, bucket, botDatabase, mainHandler);
        myListsHandler = new MyListsHandler(messageView, bucket, botDatabase, mainHandler);
    }

    public async Task Handle(Update update)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
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
        var fromChatId = message.ForwardFromChat!.Id;
        await messageView.Say("Спасибо за пересылку!", fromChatId);
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
            await command.HandlePlainText(text, fromChatId, mainHandler.GetMainKeyboard());
            return;
        }
        
        var state = await bucket.GetUserState(userId);
        
        switch (state)
        {
            case State.EditingList:
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
                await myEventsHandler.ActionInterrupted(message);
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
                await myEventsHandler.ActionInterrupted(message);
                return;
        }
        
        if (myEventsMatches.Contains(text.ToLower()))
        {
            await myEventsHandler.HandleMyEvents(message);
            return;
        }
        if (newEventMatches.Contains(text.ToLower()))
        {
            await myEventsHandler.HandleNewEvent(message);
            return;
        }
        if (myPeopleMatches.Contains(text.ToLower()))
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
                await mainHandler.HandleEditAction(callbackQuery);
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

    private readonly HashSet<string> myEventsMatches = new()
    {
        "\ud83d\udcc5 мои встречи",
        "мои встречи",
        "встречи",
        "мои события",
        "список встреч",
        "список событий",
        "события",
        "мои"
    };
    
    private readonly HashSet<string> newEventMatches = new()
    {
        "\ud83c\udfd7 создать встречу",
        "создать встречу",
        "новая встреча",
        "новое событие",
        "встреча",
        "новая",
        "создать"
    };
    
    private readonly HashSet<string> myPeopleMatches = new()
    {
        "мои люди",
        "люди"
    };
    
    private static bool SmartContains(string value, string query)
    {
        return new Regex($@"\b{Regex.Escape(query)}\b", RegexOptions.IgnoreCase).IsMatch(value);
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