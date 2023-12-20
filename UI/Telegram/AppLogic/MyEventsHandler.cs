using System.Text;
using Domain;
using Infrastructure.Api.Geocode;
using Infrastructure.Api.Maps;
using Infrastructure.Api.Taxi;
using Infrastructure.Calendar;
using Infrastructure.S3Storage;
using Infrastructure.YDB;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using EventStatus = Domain.EventStatus;
using Location = Domain.Location;
using User = Domain.User;

namespace UI.Telegram.AppLogic;

public class MyEventsHandler
{
    private readonly IMessageView messageView;
    private readonly IBucket bucket;
    private readonly EventRepo eventRepo;
    private readonly IMainHandler mainHandler;
    private readonly IGeocodeApi geocodeApi;
    private readonly ITaxiApi taxiApi;
    private readonly IMapsApi mapsApi;

    public MyEventsHandler(
        IMessageView messageView,
        IBucket bucket,
        EventRepo eventRepo,
        IGeocodeApi geocodeApi,
        ITaxiApi taxiApi,
        IMapsApi mapsApi,
        IMainHandler mainHandler)
    {
        this.messageView = messageView;
        this.bucket = bucket;
        this.geocodeApi = geocodeApi;
        this.eventRepo = eventRepo;
        this.taxiApi = taxiApi;
        this.mapsApi = mapsApi;
        this.mainHandler = mainHandler;
    }
    
    public async Task EditPicture(Message message)
    {
        switch (message.Text)
        {
            case "Отмена":
                await CancelAction(message);
                return;
            case "Назад":
                await CancelAction(message);
                return;
            case "Удалить фото":
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
        draft.Picture = fileId;
        await bucket.WriteDraft(message.From.Id, draft);
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
            await bucket.WriteDraft(userId, draft);
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

    // ReSharper disable once CognitiveComplexity
    private static string GetEventMessageText(Event newEvent, Dictionary<string, string> fields)
    {
        var messageText = new StringBuilder();
        foreach (var field in fields)
        {
            if (field.Key == "CreatorId") continue;
            var propertyValue = GetPropertyValue(newEvent, field.Key);
            switch (propertyValue)
            {
                case Location location:
                    propertyValue = location.Loc;
                    break;
                case DateTime dateTime:
                    propertyValue = dateTime.ToString("dd-MM-yyyy HH:mm");
                    break;
                case List<EventParticipant> participants:
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

        var organizerField = fields.FirstOrDefault(f => f.Key == "CreatorId");
        if (organizerField.Key != null)
        {
            if (GetPropertyValue(newEvent, organizerField.Key) is long organizerValue)
            {
                messageText.AppendLine(
                    $"<a href=\"tg://user?id={organizerValue}\">Организатор встречи</a>");
            }
        }

        var participantsField = fields.FirstOrDefault(f => f.Key == "Participants");
        if (participantsField.Key == null) return messageText.ToString();
        var participantsValue = GetPropertyValue(newEvent, participantsField.Key) as List<EventParticipant>;
        if (participantsValue == null || !participantsValue.Any()) return messageText.ToString();
        messageText.AppendLine();
        messageText.AppendLine($"{participantsField.Value}:");
        foreach (var participant in participantsValue)
        {
            if (participant.ParticipantUsername is not "")
                messageText.AppendLine($"- <a href=\"@{participant.ParticipantUsername}\"> @{participant.ParticipantUsername}</a>" +
                                       $" ({participant.GetEmojiForStatus()})");
            else
                messageText.AppendLine($"- <a href=\"tg://user?id={participant.Id}\">{participant.ParticipantFirstName}</a>" +
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
            CallbackData = "cancelEvent"
        };
        var createButton = new InlineKeyboardButton("Создать")
        {
            CallbackData = "createEvent"
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
            if (pair.Key == "CreatorId") continue;

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

    public async Task ActionInterrupted(Message message, State userState)
    {
        var fromChatId = message.Chat.Id;
        var text = message.Text!.ToLower();
        if (Matches.newEventMatches.Contains(text.ToLower()) && userState == State.CreatingEvent)
        {
            await HandleNewEvent(message);
            return;
        }
        // if (Matches.myEventsMatches.Contains(text.ToLower()) && userState == State.EditingEvent)
        // {
        //     await HandleMyEvents(message);
        //     return;
        // }

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
        await bucket.WriteDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Фото удалено!", chatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, chatId, true);
    }

    public async Task EditParticipants(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;
        if (text == "Назад")
        {
            await CancelAction(message);
            return;
        }
        
        List<User> participants;
        var draft = await bucket.GetEventDraft(userId);
        if (text != null && text.Trim().StartsWith('!'))
        {
            var personListName = text.Trim()[1..];
            participants = await eventRepo.GetUsersFromPersonList(personListName, userId);
            if (participants.Count == 0)
            {
                await messageView.Say($"Список {personListName} не найден", fromChatId);
                return;
            }
        }
        else
        {
            var participantsRaw = text!.Split(new[] { ',', ' ', '\n' },
                StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().Replace("@", "")).ToList();
            participants = await eventRepo.FilterValidUsersByUsernames(participantsRaw);
            var inactiveUsers = participantsRaw.Except(participants.Select(p => p.Username)).ToList();
            if (inactiveUsers.Any())
            {
                await messageView.Say($"Пользователи: {string.Join(", @", inactiveUsers)} не найдены\n" +
                                      $"Отправьте им этого бота, чтобы добавить их в список участников", fromChatId);
            }

            if (!participants.Any())
            {
                return;
            }
        }
        draft.Participants = participants.Select(p => new EventParticipant(p.Id, UserStatus.Maybe, p.Username ?? "", 
            p.FirstName?? "")).ToList();
        await bucket.WriteDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Участники изменены!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task EditDate(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;
        if (text == "Назад")
        {
            await CancelAction(message);
            return;
        }

        var draft = await bucket.GetEventDraft(userId);
        // TODO: make smart check, because this only accepts dd.MM.yyyy
        string[] dateFormats = { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy", "dd.MM.yyyy HH:mm" };
        var isValidDate = DateTime.TryParseExact(text, dateFormats, null, System.Globalization.DateTimeStyles.None, out var date);
        if (!isValidDate)
        {
            await messageView.Say("Неверный формат даты", fromChatId);
            return;
        }
        draft.Date = date;
        await bucket.WriteDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Дата изменена!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task EditLocation(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;
        if (text is "Назад" or null)
        {
            await CancelAction(message);
            return;
        }
        var draft = await bucket.GetEventDraft(userId);
        var cleanText = text.Trim();
        var location = cleanText.StartsWith('!') ? new Location(text[1..].Trim()) :
            geocodeApi.ResolveLocation(cleanText);
        draft.Location = location;
        await bucket.WriteDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Место изменено!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task EditDescription(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;
        if (text == "Назад")
        {
            await CancelAction(message);
            return;
        }

        var draft = await bucket.GetEventDraft(userId);
        draft.Description = text;
        await bucket.WriteDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Описание изменено!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task EditName(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var text = message.Text;
        if (text == "Назад")
        {
            await CancelAction(message);
            return;
        }
        var draft = await bucket.GetEventDraft(userId);
        draft.Name = text;
        await bucket.WriteDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingEvent : State.CreatingEvent);
        await messageView.SayWithKeyboard("Название изменено!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdateEventMessage(draft, userId, fromChatId);
    }

    public async Task HandleMyEvents(Message message)
    {
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;
        var myEvents = (await eventRepo.GetEventsByUserId(userId)).ToList();
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
        await bucket.WriteDraft(userId, draft);
    }

    private static InlineKeyboardMarkup GetEventsInlineKeyboard(IEnumerable<ISimple> myEvents,
        int currentPage = 1, int eventsPerPage = 6)
    {
        const int columnCount = 2;
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        var startIndex = (currentPage - 1) * eventsPerPage;
        var enumerable = myEvents as ISimple[] ?? myEvents.ToArray();
        var endIndex = Math.Min(startIndex + eventsPerPage, enumerable.Length);

        for (var i = startIndex; i < endIndex; i++)
        {
            var eventButton = new InlineKeyboardButton(enumerable[i].Name)
            {
                CallbackData = $"showEvent_{enumerable[i].Id}"
            };

            var columnIndex = (i - startIndex) % columnCount;

            if (columnIndex == 0)
            {
                inlineKeyboard.Add(new List<InlineKeyboardButton>());
            }

            inlineKeyboard.Last().Add(eventButton);
        }

        if (enumerable.Length > endIndex)
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
        var myEvent = await eventRepo.GetEventById(eventId);
        if (myEvent != null)
        {
            var participant = myEvent.Participants!.FirstOrDefault(p => p.Id == userId);
            var currentStatus = participant!.ParticipantUserStatus;
            switch (currentStatus)
            {
                case UserStatus.Maybe:
                    participant.ParticipantUserStatus = UserStatus.WillGo;
                    await messageView.AnswerCallbackQuery(callbackQueryId, "Вы идете");
                    break;
                case UserStatus.WillGo:
                    participant.ParticipantUserStatus = UserStatus.WontGo;
                    await messageView.AnswerCallbackQuery(callbackQueryId, "Вы не идете");
                    break;
                case UserStatus.WontGo:
                    participant.ParticipantUserStatus = UserStatus.WillGo;
                    await messageView.AnswerCallbackQuery(callbackQueryId, "Вы идете");
                    break;
            }
            await eventRepo.PushEvent(myEvent);
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

        var myEvents = await eventRepo.GetEventsByUserId(userId);
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
        var myEvent = await eventRepo.GetEventById(eventId);

        if (myEvent != null)
        {
            var messageText = GetEventMessageText(myEvent, myEvent.GetFields());
            var inlineKeyboard = GetEventInlineKeyboard(myEvent, userId);
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
    
    private InlineKeyboardMarkup GetEventInlineKeyboard(Event myEvent, long userId)
    { 
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();
        List<InlineKeyboardButton> firstRow = new();
        var addToCalendarButton = new InlineKeyboardButton("\ud83d\udcc5 В календарь")
            {
                CallbackData = $"addToCalendar_{myEvent.Id}"
            };
        firstRow.Add(addToCalendarButton);
        if (myEvent.Participants != null && myEvent.Participants.Any(p => p.Id == userId))
        {
            var currentStatus = myEvent.Participants.FirstOrDefault(p => p.Id == userId)!.ParticipantUserStatus;
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
        if (myEvent.Location is not null && myEvent.Location.HasCoordinates)
        {
            var showOnMapButton = new InlineKeyboardButton("\ud83d\uddfa Карта")
            {
                Url = mapsApi.GetMapLink(myEvent.Location)
            };
            var orderTaxiButton = new InlineKeyboardButton("\ud83d\ude95 Такси")
            {
                Url = taxiApi.GetTaxiLink(myEvent.Location)
            };
            inlineKeyboard.Add(new List<InlineKeyboardButton> { showOnMapButton, orderTaxiButton });
        }
        
        if (myEvent.CreatorId == userId)
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
        var chatId = callbackQuery.Message!.Chat.Id;
        var eventId = callbackQuery.Data!.Split('_')[1];
        var myEvent = await eventRepo.GetEventById(eventId);
        if (myEvent != null)
        {
            var calendar = CalendarGenerator.GenerateEventCalendar(myEvent);
            const string helpText = "Чтобы добавить это событие в свой календарь, выполните следующие шаги:\n" +
                                    "1. Скачайте файл your_event.ics, нажав на кнопку ниже.\n" +
                                    "2. Откройте свой календарь (например, Google Календарь).\n" +
                                    "3. Импортируйте событие, выбрав опцию 'Импорт' или 'Добавить'.\n" +
                                    "4. Выберите скачанный файл your_event.ics.\n" +
                                    "5. Подтвердите импорт, и событие будет добавлено в ваш календарь.";
            await messageView.SendFile(chatId, calendar, "your_event.ics", helpText);
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }

    public async Task HandleSaveEventAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var eventId = callbackQuery.Data!.Split('_')[1];
        var callbackQueryId = callbackQuery.Id;
        var draft = await bucket.GetEventDraft(userId);
        await eventRepo.PushEvent(draft);
        await bucket.ClearDraft(userId);
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
        await bucket.ClearDraft(userId);
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
        if (draft.Location?.Loc is null)
        {
            await messageView.Say("Нужно заполнить место", chatId);
            return;
        }
        await eventRepo.PushEvent(draft);
        await bucket.ClearDraft(userId);
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
        
        var myEvents = await eventRepo.GetEventsByUserId(userId);
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
        
        var existingEvent = await eventRepo.GetEventById(eventId);
        if (existingEvent is null)
        {
            await messageView.AnswerCallbackQuery(callbackQueryId, "Событие не найдено");
            return;
        }
        if (existingEvent.CreatorId == userId)
        {
            await bucket.WriteUserState(userId, State.EditingEvent);
            existingEvent.InlinedMessageId = messageId;
            await bucket.WriteDraft(userId, existingEvent);
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