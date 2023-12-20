using System.Text;
using Domain;
using Infrastructure.S3Storage;
using Infrastructure.YDB;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace UI.Telegram.AppLogic;

public class MyListsHandler
{
    private readonly IMessageView messageView;
    private readonly IBucket bucket;
    private readonly IMainHandler mainHandler;
    private readonly EventRepo eventRepo;

    public MyListsHandler(
        IMessageView messageView,
        IBucket bucket,
        EventRepo eventRepo,
        IMainHandler mainHandler)
    {
        this.messageView = messageView;
        this.bucket = bucket;
        this.eventRepo = eventRepo;
        this.mainHandler = mainHandler;
    }


    public async Task EditListParticipants(Message message)
    {
        var text = message.Text;
        if (text == "Назад")
        {
            await CancelAction(message);
            return;
        }
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;
        var draft = await bucket.GetPersonListDraft(userId);
        var participantsRaw = text!.Split(new[] { ',', ' ', '\n' },
                StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().Replace("@", "")).ToList();
        var participants = await eventRepo.FilterValidUsersByUsernames(participantsRaw);
        var inactiveUsers = participantsRaw.Except(participants.Select(p => p.Username)).ToList();
        if (inactiveUsers.Any())
        {
            await messageView.Say($"Пользователи: {string.Join(", @", inactiveUsers)} не найдены\n" +
                                  $"Отправьте им этого бота, чтобы добавить их в список", fromChatId);
        }
        if (!participants.Any())
        {
            return;
        }
        draft.Participants = participants.Select(p => new PersonListParticipant(p.Id, p.Username, p.FirstName)).ToList();
        await bucket.WriteDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingList : State.CreatingList);
        await messageView.SayWithKeyboard("Участники изменены!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdatePersonListMessage(draft, fromChatId, draft.InlinedMessageId!.Value);
    }

    public async Task EditListName(Message message)
    {
        var text = message.Text;
        if (text == "Назад")
        {
            await CancelAction(message);
            return;
        }
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;
        
        var draft = await bucket.GetPersonListDraft(userId);
        draft.Name = text;
        await bucket.WriteDraft(userId, draft);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingList : State.CreatingList);
        await messageView.SayWithKeyboard("Название изменено!", fromChatId, mainHandler.GetMainKeyboard());
        await UpdatePersonListMessage(draft, fromChatId, draft.InlinedMessageId!.Value);
    }
    
    private async Task UpdatePersonListMessage(PersonList draft, long fromChatId, int messageId)
    {
        string messageText;
        InlineKeyboardMarkup keyboard;
        if (draft.Status == EventStatus.Active)
        {
            messageText = "Редактирование списка:\n\n" + GetPersonListMessageText(draft);
            keyboard = GetEditPersonListInlineKeyboard(draft);
        }
        else
        {
            messageText = "Новая список:\n\n" + GetPersonListMessageText(draft);
            keyboard = GetCreatePersonListInlineKeyboard(draft);
        }
        await messageView.EditInlineMessage(messageText, fromChatId, messageId, keyboard);
    }
    
    private async Task CancelAction(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        var draft = await bucket.GetPersonListDraft(userId);
        await bucket.WriteUserState(userId, draft.Status == EventStatus.Active ? State.EditingList : State.CreatingList);
        await messageView.SayWithKeyboard("Ввод отменен", fromChatId, mainHandler.GetMainKeyboard());
    }
    
    public async Task ActionInterrupted(Message message)
    {
        var fromChatId = message.Chat.Id;
        await messageView.Say("Сначала закончите редактирование списка", fromChatId);
    }
    
    public async Task HandleMyPeopleCommand(Message message)
    {
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;

        var peopleLists = (await eventRepo.GetPersonListsByUserId(userId)).ToList();
        var inlineKeyboard = GetPeopleInlineKeyboard(peopleLists);
        
        await messageView.SayWithInlineKeyboard(peopleLists.Any() ? "Ваши списки людей:" : "У вас нет списков людей",
            fromChatId, inlineKeyboard);
    }

    private static InlineKeyboardMarkup GetPeopleInlineKeyboard(IEnumerable<ISimple> people,
        int currentPage = 1, int peoplePerPage = 6)
    {
        const int columnCount = 2; // Number of columns for buttons
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        var startIndex = (currentPage - 1) * peoplePerPage;
        var simplePersonLists = people.ToList();
        var endIndex = Math.Min(startIndex + peoplePerPage, simplePersonLists.Count);

        for (var i = startIndex; i < endIndex; i++)
        {
            var personButton = new InlineKeyboardButton(simplePersonLists[i].Name)
            {
                CallbackData = $"showPersonList_{simplePersonLists[i].Id}"
            };

            var columnIndex = (i - startIndex) % columnCount;

            if (columnIndex == 0)
            {
                inlineKeyboard.Add(new List<InlineKeyboardButton>());
            }

            inlineKeyboard.Last().Add(personButton);
        }

        if (currentPage > 1)
        {
            var prevButton = new InlineKeyboardButton("⬅️ Назад")
                { CallbackData = $"changePagePeople_{currentPage - 1}" };

            inlineKeyboard.Add(new List<InlineKeyboardButton> { prevButton });
        }

        if (endIndex < simplePersonLists.Count)
        {
            var nextButton = new InlineKeyboardButton("Вперед ➡️")
                { CallbackData = $"changePagePeople_{currentPage + 1}" };

            inlineKeyboard.Add(new List<InlineKeyboardButton> { nextButton });
        }

        inlineKeyboard.Add(new List<InlineKeyboardButton>
        { new("Новый список")
            { CallbackData = $"createPersonList" } });
        return new InlineKeyboardMarkup(inlineKeyboard);
    }

    public async Task HandleNextPageAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var data = callbackQuery.Data!.Split('_');
        var currentPage = int.Parse(data[1]);

        var myPeople = await eventRepo.GetPersonListsByUserId(userId);
        var peopleKeyboard = GetPeopleInlineKeyboard(myPeople, currentPage);
        await messageView.EditInlineKeyboard("Ваши люди:", chatId, messageId, peopleKeyboard);
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }

    public async Task HandleViewPersonListAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var data = callbackQuery.Data!.Split('_');
        var listId = data[1];

        await bucket.WriteUserState(userId, State.Start);
        var personList = await eventRepo.GetPersonListById(listId);

        if (personList != null)
        {
            var messageText = GetPersonListMessageText(personList);
            var inlineKeyboard = GetPersonListInlineKeyboard(personList);
            await messageView.EditInlineMessage(messageText, chatId, messageId, inlineKeyboard);
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }

    private static string GetPersonListMessageText(PersonList myPersonList)
    {
        var messageText = new StringBuilder();
        messageText.AppendLine($"Имя списка: {myPersonList.Name ?? "🚫"}");
        messageText.AppendLine("Участники:");
        if (myPersonList.Participants is null)
        {
            messageText.AppendLine("Список пуст");
            return messageText.ToString();
        }
        foreach (var participant in myPersonList.Participants)
        {
            if (participant.ParticipantUsername is not null)
                messageText.AppendLine($" - <a href=\"@{participant.ParticipantUsername}\"> @{participant.ParticipantUsername}</a>");
            else
                messageText.AppendLine($" - <a href=\"tg://user?id={participant.Id}\">{participant.ParticipantFirstName}</a>");
        }
        return messageText.ToString();
    }

    private static InlineKeyboardMarkup GetPersonListInlineKeyboard(PersonList myPersonList)
    {
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        var editButton = new InlineKeyboardButton($"Редактировать {myPersonList.Name}")
        {
            CallbackData = $"editPersonList_{myPersonList.Id}"
        };
        inlineKeyboard.Add(new List<InlineKeyboardButton> { editButton });
        var backButton = new InlineKeyboardButton("Назад")
        {
            CallbackData = $"backMyPeople"
        };
        inlineKeyboard.Add(new List<InlineKeyboardButton> { backButton });
        var keyboard = new InlineKeyboardMarkup(inlineKeyboard);
        return keyboard;
    }

    public async Task HandleEditPersonListAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var data = callbackQuery.Data!.Split('_');
        var personListId = data[1];

        var personList = await eventRepo.GetPersonListById(personListId);

        if (personList != null)
        {
            await bucket.WriteUserState(userId, State.EditingList);
            var inlineKeyboard = GetEditPersonListInlineKeyboard(personList);
            var messageText = "Редактирование списка:\n\n" + GetPersonListMessageText(personList);
            personList.InlinedMessageId = messageId;
            await bucket.WriteDraft(userId, personList);

            await messageView.EditInlineMessage(messageText, chatId, messageId, inlineKeyboard);
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }
    
    private static List<List<InlineKeyboardButton>> GetBaseEditInlineKeyboard(PersonList myPersonList)
    {
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        var editNameButton = new InlineKeyboardButton("Изменить имя")
        {
            CallbackData = $"edit_ListName_{myPersonList.Id}"
        };

        var editParticipantsButton = new InlineKeyboardButton("Изменить участников")
        {
            CallbackData = $"edit_ListParticipants_{myPersonList.Id}"
        };
        
        inlineKeyboard.Add(new List<InlineKeyboardButton> { editNameButton });
        inlineKeyboard.Add(new List<InlineKeyboardButton> { editParticipantsButton });
        return inlineKeyboard;
    }

    private static InlineKeyboardMarkup GetEditPersonListInlineKeyboard(PersonList myPersonList)
    {
        var inlineKeyboard = GetBaseEditInlineKeyboard(myPersonList);

        var backButton = new InlineKeyboardButton("Назад")
        {
            CallbackData = $"showPersonList_{myPersonList.Id}"
        };

        var saveButton = new InlineKeyboardButton("Сохранить")
        {
            CallbackData = $"savePersonList_{myPersonList.Id}"
        };

        inlineKeyboard.Add(new List<InlineKeyboardButton> { backButton, saveButton });

        return new InlineKeyboardMarkup(inlineKeyboard);
    }
    
    private static InlineKeyboardMarkup GetCreatePersonListInlineKeyboard(PersonList myPersonList)
    {
        var inlineKeyboard = GetBaseEditInlineKeyboard(myPersonList);

        var backButton = new InlineKeyboardButton("Назад")
        {
            CallbackData = $"backMyPeople"
        };

        var saveButton = new InlineKeyboardButton("Сохранить")
        {
            CallbackData = $"savePersonList_{myPersonList.Id}"
        };

        inlineKeyboard.Add(new List<InlineKeyboardButton> { backButton, saveButton });

        return new InlineKeyboardMarkup(inlineKeyboard);
    }

    public async Task HandleCreatePersonListAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var draft = new PersonList(userId)
        {
            InlinedMessageId = messageId
        };
        await bucket.WriteUserState(userId, State.CreatingList);
        var messageText = "Новая встреча\n\n" + GetPersonListMessageText(draft);
        var inlineKeyboard = GetCreatePersonListInlineKeyboard(draft);
        await messageView.EditInlineMessage(messageText, chatId, messageId, inlineKeyboard);
        await bucket.WriteDraft(userId, draft);
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }

    public async Task HandleSavePersonList(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var callbackQueryId = callbackQuery.Id;
        var data = callbackQuery.Data!.Split('_');
        var personListId = data[1];
        var draft = await bucket.GetPersonListDraft(userId);
        if (draft.Participants is null || !draft.Participants.Any())
        {
            await messageView.AnswerCallbackQuery(callbackQueryId, "Список не может быть пустым");
            return;
        }
        if (draft.Name is null)
        {
            await messageView.AnswerCallbackQuery(callbackQueryId, "Укажите название списка");
            return;
        }
        draft.Status = EventStatus.Active;
        await eventRepo.PushPersonList(draft);
        await bucket.ClearDraft(userId);
        await bucket.WriteUserState(userId, State.Start);
        callbackQuery.Data = $"showPersonList_{personListId}";
        await HandleViewPersonListAction(callbackQuery);
        await messageView.AnswerCallbackQuery(callbackQueryId, "Список сохранен");
    }

    public async Task HandleBackAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var callbackQueryId = callbackQuery.Id;
        var myPeople = await eventRepo.GetPersonListsByUserId(userId);
        var peopleKeyboard = GetPeopleInlineKeyboard(myPeople);
        await bucket.ClearDraft(userId);
        await bucket.WriteUserState(userId, State.Start);
        await messageView.EditInlineMessage("Ваши люди:", chatId, messageId, peopleKeyboard);
        await messageView.AnswerCallbackQuery(callbackQueryId, null);
    }
}