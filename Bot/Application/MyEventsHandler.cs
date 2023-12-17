using System.Text;
using Bot.Infrastructure.S3Storage;
using Bot.Infrastructure.YDB;
using Bot.Domain;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using EventStatus = Bot.Domain.EventStatus;
using Location = Bot.Domain.Location;

namespace Bot.Application;

public class MyEventsHandler
{
    private static List<Event> RetrieveEvents(long userId)
    {
        return new List<Event>
        {
            Event.FromJson(
                @"{""id"":""1"",""name"":""Встреча с друзьями"",""date"":""2024-10-10T12:00:00"",""description"":""Встреча с друзьями в кафе"", ""location"":""Кафе"", ""participants"":[{""userId"":""gx56q"",""participantStatus"":""WillGo""},{""userId"":""dudefromtheInternet"",""participantStatus"":""WillGo""},{""userId"":""yellooot"",""participantStatus"":""WillGo""}, {""userId"":""xoposhiy"",""participantStatus"":""WillGo""}], ""creator"":""349173467"", ""status"":""active"", ""Picture"":""true""}"),
            Event.FromJson(
                @"{""id"":""2"",""name"":""Созвон"",""date"":""2024-10-10T12:00:00"",""description"":""Встреча с друзьями в кафе"", ""location"":""Кафе"", ""participants"":[{""userId"":""gx56q"",""participantStatus"":""WillGo""},{""userId"":""dudefromtheInternet"",""participantStatus"":""WillGo""},{""userId"":""yellooot"",""participantStatus"":""WillGo""}, {""userId"":""xoposhiy"",""participantStatus"":""WillGo""}], ""creator"":""349173467"", ""status"":""active"", ""Picture"":""false""}"),
            Event.FromJson(
                @"{""id"":""3"",""name"":""Экзамен тервер"",""date"":""2024-10-10T12:00:00"",""description"":""Встреча с друзьями в кафе"", ""location"":""Кафе"", ""participants"":[{""userId"":""gx56q"",""participantStatus"":""WillGo""},{""userId"":""dudefromtheInternet"",""participantStatus"":""WillGo""},{""userId"":""X_V_I_M"",""participantStatus"":""WillGo""}, {""userId"":""xoposhiy"",""participantStatus"":""WillGo""}], ""creator"":""1059949647"", ""status"":""active"", ""Picture"":""false""}"),
            Event.FromJson(
                @"{""id"":""4"",""name"":""Яндекс митап"",""date"":""2024-10-10T12:00:00"",""description"":""Встреча с друзьями в кафе"", ""location"":""Кафе"", ""participants"":[{""userId"":""gx56q"",""participantStatus"":""WillGo""},{""userId"":""dudefromtheInternet"",""participantStatus"":""WillGo""},{""userId"":""yellooot"",""participantStatus"":""WillGo""}, {""userId"":""xoposhiy"",""participantStatus"":""WillGo""}], ""creator"":""1059949647"", ""status"":""active"", ""Picture"":""false""}"),
            Event.FromJson(
                @"{""id"":""5"",""name"":""Шашлычок"",""date"":""2024-10-10T12:00:00"",""description"":""Встреча с друзьями в кафе"", ""location"":""Кафе"", ""participants"":[{""userId"":""gx56q"",""participantStatus"":""WillGo""},{""userId"":""dudefromtheInternet"",""participantStatus"":""WillGo""},{""userId"":""yellooot"",""participantStatus"":""WillGo""}, {""userId"":""xoposhiy"",""participantStatus"":""WillGo""}], ""creator"":""1059949647"", ""status"":""active"", ""Picture"":""false""}"),
            Event.FromJson(
                @"{""id"":""6"",""name"":""Футбольчик"",""date"":""2024-10-10T12:00:00"",""description"":""Мальчики походят на качков"", ""location"":""Кафе"", ""participants"":[{""userId"":""gx56q"",""participantStatus"":""WillGo""},{""userId"":""dudefromtheInternet"",""participantStatus"":""WillGo""},{""userId"":""yellooot"",""participantStatus"":""WillGo""}, {""userId"":""xoposhiy"",""participantStatus"":""WillGo""}], ""creator"":""1059949647"", ""status"":""active"", ""Picture"":""false""}"),
            Event.FromJson(
                @"{""id"":""7"",""name"":""Перекур"",""date"":""2024-10-10T12:00:00"",""description"":""Встреча с друзьями в кафе"", ""location"":""Кафе"", ""participants"":[{""userId"":""gx56q"",""participantStatus"":""WillGo""},{""userId"":""dudefromtheInternet"",""participantStatus"":""WillGo""},{""userId"":""yellooot"",""participantStatus"":""WillGo""}, {""userId"":""xoposhiy"",""participantStatus"":""WillGo""}], ""creator"":""1059949647"", ""status"":""active"", ""Picture"":""false""}"),
            Event.FromJson(
                @"{""id"":""8"",""name"":""Коньячок"",""date"":""2024-10-10T12:00:00"",""description"":""Встреча с друзьями в кафе"", ""location"":""Кафе"", ""participants"":[{""userId"":""gx56q"",""participantStatus"":""WillGo""},{""userId"":""dudefromtheInternet"",""participantStatus"":""WillGo""},{""userId"":""yellooot"",""participantStatus"":""WillGo""}, {""userId"":""xoposhiy"",""participantStatus"":""WillGo""}], ""creator"":""1059949647"", ""status"":""active"", ""Picture"":""false""}")
        };
    }
    
    private static string GetTaxiLink(string location)
    {
        return
            "https://3.redirect.appmetrica.yandex.com/route?&end-lat=56.841129&end-lon=60.614752&tariffClass=comfortplus&ref=mywebsiteru&appmetrica_tracking_id=1178268795219780156&lang=ru\n";
        
    }

    private static string GetMapLink(string location)
    {
        return "https://yandex.ru/maps/?rtext=~56.841129%2C60.614752&rtt=mt";
    }

    private static byte[] GetEventCalendar(Event data)
    {
        var calendar = new Calendar();

        var calendarEvent = new CalendarEvent()
        {
            Summary = data.Name,
            Description = data.Description,
            Location = data.Location?.ToString() ?? "",
            DtStart = new CalDateTime(Convert.ToDateTime(data.Date), "UTC")
        };
        
        calendar.Events.Add(calendarEvent);
        var serializer = new CalendarSerializer();
        var calendarString = serializer.SerializeToString(calendar);
        return Encoding.UTF8.GetBytes(calendarString);
    }


    private readonly IMessageView messageView;
    private readonly IBucket bucket;
    private readonly IBotDatabase botDatabase;
    private readonly IMainHandler mainHandler;

    public MyEventsHandler(
        IMessageView messageView,
        IBucket bucket,
        IBotDatabase botDatabase,
        IMainHandler mainHandler)
    {
        this.messageView = messageView;
        this.bucket = bucket;
        this.botDatabase = botDatabase;
        this.mainHandler = mainHandler;
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

    public async Task EditPicture(Message message)
    {
        if (message.Text == "Отмена")
        {
            await CancelAction(message);
            return;
        }

        if (message.Text == "Удалить фото")
        {
            await DeletePhoto(message);
            return;
        }

        if (message.Photo == null)
        {
            await messageView.Say("Пожалуйста, отправьте фото", message.Chat.Id);
        }

        var fromChatId = message.Chat.Id;
        var draft = await bucket.GetEventDraft(message.From!.Id);
        var fileId = message.Photo!.Last().FileId;
        await bucket.WriteEventPicture(draft.Id, fileId);
        draft.Picture = new Picture("0"); // TO-DO: 0 index?? 
        await bucket.WriteEventDraft(message.From.Id, draft);
        await bucket.WriteUserState(message.From.Id,
            draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Фото изменено!", fromChatId,
            mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, message.From.Id, fromChatId, true);
    }

    private async Task UpdateEventMessage(Event draft, long userId, long chatId, bool isPicture = false)
    {
        string messageText;
        InlineKeyboardMarkup? keyboard;
        var fields = draft.GetFields();
        if (draft.Status == EventStatus.Active)
        {
            messageText = "Редактирование встречи:\n\n" + GetEventMessageText(draft, fields);
            keyboard = GetEditEventInlineKeyboard(draft, fields);
        }
        else
        {
            messageText = "Новая встреча:\n\n" + GetEventMessageText(draft, fields);
            keyboard = GetNewEventInlineKeyboard(fields);
        }

        if (isPicture)
        {
            var messageId = draft.InlinedMessageId!.Value;
            await messageView.DeleteMessage(chatId, messageId);
            var pictureId = await bucket.GetEventPicture(draft.Id);
            var message = await messageView.SayWithInlineKeyboardAndPhoto(messageText,
                chatId, keyboard, pictureId);
            draft.InlinedMessageId = message.MessageId;
            await bucket.WriteEventDraft(userId, draft);
        }
        else if (draft.Picture is not null)
        {
            await messageView.EditInlineMessageWithPhoto(messageText, chatId, draft.InlinedMessageId!.Value, keyboard,
                null);
        }
        else
        {
            await messageView.EditInlineMessage(messageText, chatId, draft.InlinedMessageId!.Value, keyboard);
        }
    }

    private string GetEventMessageText(Event newEvent, Dictionary<string, string> fields)
    {
        var messageText = new StringBuilder();
        foreach (var field in fields)
        {
            if (field.Key == "Creator") continue;
            var propertyValue = GetPropertyValue(newEvent, field.Key);
            switch (propertyValue)
            {
                case DateTime dateTime:
                    propertyValue = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                case List<Participant> participants:
                    propertyValue = participants.Count.ToString();
                    break;
                case null:
                    propertyValue = "🚫";
                    break;
                case bool boolean:
                    propertyValue = boolean ? "✅" : "🚫";
                    break;
                case string:
                    propertyValue = $"{propertyValue}";
                    break;
            }

            messageText.AppendLine($"<b>{field.Value}:</b> {propertyValue}");
        }

        var organizerField = fields.FirstOrDefault(f => f.Key == "Creator");
        if (organizerField.Key != null)
        {
            if (GetPropertyValue(newEvent, organizerField.Key) is string organizerValue)
            {
                messageText.AppendLine(
                    $"<b>{organizerField.Value}:</b> <a href=\"tg://user?id={organizerValue}\">Имя организатора</a>");
            }
        }

        var participantsField = fields.FirstOrDefault(f => f.Key == "Participants");
        if (participantsField.Key == null) return messageText.ToString();
        var participantsValue = GetPropertyValue(newEvent, participantsField.Key) as List<Participant>;
        if (participantsValue == null || !participantsValue.Any()) return messageText.ToString();
        messageText.AppendLine();
        messageText.AppendLine($"{participantsField.Value}:");
        foreach (var participant in participantsValue)
        {
            messageText.AppendLine($"- <a href=\"@{participant.Id}\"> @{participant.Id}</a>" +
                                   $" ({participant.GetEmojiForStatus()})");
        }

        return messageText.ToString();
    }

    private static object GetPropertyValue(Event myEvent, string propertyName)
    {
        var propertyInfo = typeof(Event).GetProperty(propertyName);
        return propertyInfo?.GetValue(myEvent) ?? "🚫";
    }

    private static InlineKeyboardMarkup GetEditEventInlineKeyboard(Event existingEvent,
        Dictionary<string, string> fields)
    {
        var inlineKeyboard = GetBaseInlineKeyboard(fields);
        var backButton = new InlineKeyboardButton("\ud83d\udd19 Назад")
        {
            CallbackData = $"showEvent_{existingEvent.Id}"
        };
        var saveButton = new InlineKeyboardButton("Сохранить 💾")
        {
            CallbackData = $"saveEvent_{existingEvent.Id}"
        };
        inlineKeyboard.Add(new List<InlineKeyboardButton> { backButton, saveButton });
        return new InlineKeyboardMarkup(inlineKeyboard);
    }

    private static InlineKeyboardMarkup GetNewEventInlineKeyboard(Dictionary<string, string> fields)
    {
        var inlineKeyboard = GetBaseInlineKeyboard(fields);
        var cancelButton = new InlineKeyboardButton("Отмена")
        {
            CallbackData = $"cancelEvent"
        };
        var createButton = new InlineKeyboardButton("Создать")
        {
            CallbackData = $"createEvent"
        };
        inlineKeyboard.Add(new List<InlineKeyboardButton> { cancelButton, createButton });
        return new InlineKeyboardMarkup(inlineKeyboard);
    }

    private static List<List<InlineKeyboardButton>> GetBaseInlineKeyboard(Dictionary<string, string> fields)
    {
        const int columnCount = 2;
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();
        var textButton = new InlineKeyboardButton("Выберите поле для редактирования")
        {
            CallbackData = "noLoad"
        };
        inlineKeyboard.Add(new List<InlineKeyboardButton> { textButton });
        var i = 0;
        foreach (var pair in fields)
        {
            if (pair.Key == "Creator") continue;

            var fieldButton = new InlineKeyboardButton(pair.Value)
            {
                CallbackData = $"edit_{pair.Key}"
            };

            var columnIndex = i % columnCount;

            if (columnIndex == 0)
            {
                inlineKeyboard.Add(new List<InlineKeyboardButton>());
            }

            inlineKeyboard.Last().Add(fieldButton);
            i++;
        }

        return inlineKeyboard;
    }

    public async Task ActionInterrupted(Message message)
    {
        var fromChatId = message.Chat.Id;
        var text = message.Text!.ToLower();
        if (newEventMatches.Contains(text.ToLower()))
        {
            await HandleNewEvent(message);
            return;
        }
        if (myEventsMatches.Contains(text.ToLower()))
        {
            await HandleMyEvents(message);
            return;
        }

        await messageView.Say("Сначала закончите текущее действие", fromChatId);
    }

    private async Task CancelAction(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var draft = await bucket.GetEventDraft(userId);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Ввод отменен", fromChatId, mainHandler.GetMainKeyboard());
    }

    private async Task DeletePhoto(Message message)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var draft = await bucket.GetEventDraft(userId);
        await bucket.DeleteEventPicture(draft.Id);
        draft.Picture = null;
        await bucket.WriteEventDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Фото удалено!", chatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, chatId, true);
    }

    public async Task EditParticipants(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;

        var draft = await bucket.GetEventDraft(userId);
        var participants = text!.Split(' ');
        draft.Participants = participants.Select(p => new Participant(p.Trim(), UserStatus.Maybe)).ToList();
        await bucket.WriteEventDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Участники изменены!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task EditDate(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;

        var draft = await bucket.GetEventDraft(userId);
        draft.Date = new Date(text);
        await bucket.WriteEventDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Дата изменена!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task EditLocation(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;

        var draft = await bucket.GetEventDraft(userId);
        // TODO logic
        draft.Location = new Location(text);
        await bucket.WriteEventDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Место изменено!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task EditDescription(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;

        var draft = await bucket.GetEventDraft(userId);
        draft.Description = text;
        await bucket.WriteEventDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Описание изменено!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task EditName(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;

        var draft = await bucket.GetEventDraft(userId);
        draft.Name = text;
        await bucket.WriteEventDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Название изменено!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task HandleMyEvents(Message message)
    {
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;
        var myEvents = RetrieveEvents(userId);
        if (myEvents.Any())
        {
            var inlineKeyboard = GetEventsInlineKeyboard(myEvents);
            await messageView.SayWithInlineKeyboard("Ваши встречи:", fromChatId, inlineKeyboard);
        }
        else
        {
            await messageView.Say("У вас нет встреч", fromChatId);
        }
    }

    public async Task HandleNewEvent(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        Event draft;
        if (await bucket.GetUserState(userId) != State.Start)
        {
            await messageView.Say("Вы уже создаете событие", fromChatId);
            draft = await bucket.GetEventDraft(userId);
            var messageId = draft.InlinedMessageId;
            await messageView.DeleteMessage(fromChatId, messageId!.Value);
        }
        else
        {
            await bucket.WriteUserState(userId, State.CreatingEvent);
            draft = new Event(userId);
        }

        var fields = draft.GetFields();
        var messageText = "Новая встреча\n\n" + GetEventMessageText(draft, fields);
        var inlineKeyboard = GetNewEventInlineKeyboard(fields);
        Message? replyMessage;
        if (draft.Picture is not null)
        {
            var pictureId = await bucket.GetEventPicture(draft.Id);
            replyMessage =
                await messageView.SayWithInlineKeyboardAndPhoto(messageText, fromChatId, inlineKeyboard, pictureId);
        }
        else
        {
            replyMessage = await messageView.SayWithInlineKeyboard(messageText, fromChatId, inlineKeyboard);
        }

        draft.InlinedMessageId = replyMessage.MessageId;
        await bucket.WriteEventDraft(userId, draft);
    }

    private static InlineKeyboardMarkup GetEventsInlineKeyboard(IReadOnlyList<Event> myEvents,
        int currentPage = 1, int eventsPerPage = 6)
    {
        const int columnCount = 2;
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        var startIndex = (currentPage - 1) * eventsPerPage;
        var endIndex = Math.Min(startIndex + eventsPerPage, myEvents.Count);

        for (var i = startIndex; i < endIndex; i++)
        {
            var eventButton = new InlineKeyboardButton(myEvents[i].Name!)
            {
                CallbackData = $"showEvent_{myEvents[i].Id}"
            };

            var columnIndex = (i - startIndex) % columnCount;

            if (columnIndex == 0)
            {
                inlineKeyboard.Add(new List<InlineKeyboardButton>());
            }

            inlineKeyboard.Last().Add(eventButton);
        }

        if (myEvents.Count > endIndex)
        {
            var nextButton = new InlineKeyboardButton("Вперед ➡️")
                { CallbackData = $"changePageEvents_{currentPage + 1}" };

            inlineKeyboard.Add(new List<InlineKeyboardButton> { nextButton });
        }

        if (currentPage > 1)
        {
            var prevButton = new InlineKeyboardButton("⬅️ Назад")
                { CallbackData = $"changePageEvents_{currentPage - 1}" };

            inlineKeyboard.Add(new List<InlineKeyboardButton> { prevButton });
        }

        return new InlineKeyboardMarkup(inlineKeyboard);
    }

    public async Task HandleWillGoAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var eventId = callbackQuery.Data!.Split('_')[1];
        var callbackQueryId = callbackQuery.Id;
        var events = RetrieveEvents(userId);
        var myEvent = events.FirstOrDefault(e => e.Id == eventId);
        if (myEvent != null)
        {
            var currentStatus = myEvent.Participants!.FirstOrDefault(p => p.Id == "xoposhiy")?.ParticipantUserStatus;
            if (currentStatus == UserStatus.Maybe)
            {
                myEvent.Participants!.FirstOrDefault(p => p.Id == "xoposhiy")!.ParticipantUserStatus = UserStatus.WillGo;
                await messageView.AnswerCallbackQuery(callbackQueryId, "Вы идете");
            }
            else if (currentStatus == UserStatus.WillGo)
            {
                myEvent.Participants!.FirstOrDefault(p => p.Id == "xoposhiy")!.ParticipantUserStatus = UserStatus.WontGo;
                await messageView.AnswerCallbackQuery(callbackQueryId, "Вы не идете");
            }
            else if (currentStatus == UserStatus.WontGo)
            {
                myEvent.Participants!.FirstOrDefault(p => p.Id == "xoposhiy")!.ParticipantUserStatus = UserStatus.WillGo;
                await messageView.AnswerCallbackQuery(callbackQueryId, "Вы идете");
            }
            await HandleViewEventAction(callbackQuery, true);
        }
    }

    public async Task HandleNextPageAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var data = callbackQuery.Data!.Split('_');
        var currentPage = int.Parse(data[1]);

        var myEvents = RetrieveEvents(userId);
        var eventsKeyboard = GetEventsInlineKeyboard(myEvents, currentPage);
        await messageView.EditInlineKeyboard("\ud83e\udd1d Ваши встречи:", chatId, messageId, eventsKeyboard);
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }
    
    // TODO: OPTIMISE THIS
    public async Task HandleViewEventAction(CallbackQuery callbackQuery, bool toEdit = false)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var eventId = callbackQuery.Data!.Split('_')[1];


        await bucket.WriteUserState(userId, State.Start);
        var events = RetrieveEvents(userId);
        var myEvent = events.FirstOrDefault(e => e.Id == eventId);

        if (myEvent != null)
        {
            var messageText = GetEventMessageText(myEvent, myEvent.GetFields());
            var inlineKeyboard = GetEventInlineKeyboard(myEvent);
            if (toEdit)
            {
                await messageView.EditInlineMessageWithPhoto(messageText, chatId, messageId, inlineKeyboard,
                    null);
                return;
            }

            // TODO: somehow check if message has photo
            if (myEvent.Picture is null)
                await messageView.EditInlineMessage(messageText, chatId, messageId, inlineKeyboard);
            else
            {
                await messageView.DeleteMessage(chatId, messageId);
                await messageView.SayWithInlineKeyboardAndPhoto(messageText, chatId, inlineKeyboard,
                    await bucket.GetEventPicture(myEvent.Id));
            }
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }
    
    private InlineKeyboardMarkup GetEventInlineKeyboard(Event myEvent)
    { 
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();
        List<InlineKeyboardButton> firstRow = new();
        var addToCalendarButton = new InlineKeyboardButton("\ud83d\udcc5 В календарь")
            {
                CallbackData = $"addToCalendar_{myEvent.Id}"
            };
        firstRow.Add(addToCalendarButton);
        if (myEvent.Participants != null && myEvent.Participants.Any(p => p.Id == "xoposhiy"))
        {
            var currentStatus = myEvent.Participants.FirstOrDefault(p => p.Id == "xoposhiy")!.ParticipantUserStatus;
            switch (currentStatus)
            {
                case UserStatus.Maybe or UserStatus.WontGo:
                {
                    var willGoButton = new InlineKeyboardButton("\ud83d\udc4d Иду")
                    {
                        CallbackData = $"willGo_{myEvent.Id}"
                    };
                    firstRow.Add(willGoButton);
                    break;
                }
                case UserStatus.WillGo:
                {
                    var wontGoButton = new InlineKeyboardButton("\ud83d\udc4e Не иду")
                    {
                        CallbackData = $"willGo_{myEvent.Id}"
                    };
                    firstRow.Add(wontGoButton);
                    break;
                }
            }
        }
        inlineKeyboard.Add(firstRow);
        // TODO: show only if in coordinates
        var showOnMapButton = new InlineKeyboardButton("\ud83d\uddfa Карта")
        {
            Url = GetMapLink(myEvent.Location.Loc!)
        };
        var orderTaxiButton = new InlineKeyboardButton("\ud83d\ude95 Такси")
        {
            Url = GetTaxiLink(myEvent.Location.Loc!)
        };
        inlineKeyboard.Add(new List<InlineKeyboardButton> { showOnMapButton, orderTaxiButton });
            
        // if (myEvent.Creator == userId.ToString())
        if (myEvent.CreatorId == "349173467")
        {
            var editButton = new InlineKeyboardButton("Редактировать встречу")
            {
                CallbackData = $"editEvent_{myEvent.Id}"
            };
            inlineKeyboard.Add(new List<InlineKeyboardButton> { editButton });
        }
        var backButton = new InlineKeyboardButton("\ud83d\udd19 Назад")
        {
            CallbackData = "backMyEvents"
        };
        inlineKeyboard.Add(new List<InlineKeyboardButton> { backButton });

        return new InlineKeyboardMarkup(inlineKeyboard);
    }
    
    public async Task HandleAddToCalendarAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var eventId = callbackQuery.Data!.Split('_')[1];
        var events = RetrieveEvents(userId);
        var myEvent = events.FirstOrDefault(e => e.Id == eventId);
        if (myEvent != null)
        {
            var calendar = GetEventCalendar(myEvent);
            await messageView.SendFile(chatId, calendar, "your_event.ics", 
                "TODO: тут может будет справочка как добавить из файла к себе в календарь");
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }

    public async Task HandleSaveEventAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var eventId = callbackQuery.Data!.Split('_')[1];
        var callbackQueryId = callbackQuery.Id;
        // TODO: save draft to database
        await bucket.ClearEventDraft(userId);
        await bucket.WriteUserState(userId, State.Start);
        callbackQuery.Data = $"showEvent_{eventId}";
        await HandleViewEventAction(callbackQuery);
        await messageView.AnswerCallbackQuery(callbackQueryId, "Событие сохранено");
    }

    public async Task HandleCancelEventAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var callbackQueryId = callbackQuery.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        
        var draft = await bucket.GetEventDraft(userId);
        var eventId = draft.Id;
        await bucket.ClearEventDraft(userId);
        await bucket.DeleteEventPicture(eventId);
        await bucket.WriteUserState(userId, State.Start);
        await messageView.DeleteMessage(chatId, messageId);
        await messageView.SayWithKeyboard("Создание встречи отменено", chatId, mainHandler.GetMainKeyboard());
        await messageView.AnswerCallbackQuery(callbackQueryId, "Создание встречи отменено");
    }

    public async Task HandleCreateEventAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var callbackQueryId = callbackQuery.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        
        var draft = await bucket.GetEventDraft(userId);
        // TODO: make smart check
        if (draft.Name == null)
        {
            await messageView.Say("Нужно заполнить название", chatId);
            return;
        }
        if (draft.Date == null)
        {
            await messageView.Say("Нужно заполнить дату", chatId);
            return;
        }
        if (draft.Location == null)
        {
            await messageView.Say("Нужно заполнить место", chatId);
            return;
        }
        // TODO: push to database
        await bucket.ClearEventDraft(userId);
        await bucket.WriteUserState(userId, State.Start);
        await messageView.DeleteMessage(chatId, messageId);
        await messageView.SayWithKeyboard($"Встреча {draft.Name} создана!", chatId, mainHandler.GetMainKeyboard());
        await messageView.AnswerCallbackQuery(callbackQueryId, "Встреча создана");
    }

    public async Task HandleBackAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var callbackQueryId = callbackQuery.Id;
        
        var myEvents = RetrieveEvents(userId);
        var eventsKeyboard = GetEventsInlineKeyboard(myEvents);
        await messageView.EditInlineMessage("\ud83e\udd1d Ваши встречи:", chatId, messageId, 
            eventsKeyboard);
        await messageView.AnswerCallbackQuery(callbackQueryId, null);
    }

    public async Task HandleEditEventAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var callbackQueryId = callbackQuery.Id;
        var eventId = callbackQuery.Data!.Split('_')[1];
        
        
        var events = RetrieveEvents(userId);
        var existingEvent = events.FirstOrDefault(e => e.Id == eventId);
        // if (existingEvent != null && existingEvent.Creator == userId.ToString())
        if (existingEvent is { CreatorId: "349173467" })
        {
            await bucket.WriteUserState(userId, State.EditingEvent);
            existingEvent.InlinedMessageId = messageId;
            await bucket.WriteEventDraft(userId, existingEvent);
            var fields = existingEvent.GetFields();
            var messageText = "Редактирование события\n\n" + GetEventMessageText(existingEvent, fields);
            var inlineKeyboard = GetEditEventInlineKeyboard(existingEvent, fields);
            if (existingEvent.Picture is not null)
                await messageView.EditInlineMessageWithPhoto(messageText, chatId, messageId, inlineKeyboard, 
                    await bucket.GetEventPicture(existingEvent.Id));
            else
            {
                await messageView.EditInlineMessage(messageText, chatId, messageId, inlineKeyboard);
            }
        }
        await messageView.AnswerCallbackQuery(callbackQueryId, null);
    }
}