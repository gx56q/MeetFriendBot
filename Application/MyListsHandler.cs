using System.Text;
using Domain;
using Infrastructure.S3Storage;
using Infrastructure.YDB;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application;

public class MyListsHandler
{
    private static List<PersonList> RetrievePeople(long userId)
    {
        return new List<PersonList>()
        {
            PersonList.FromJson(@"{""id"":""1"",""name"":""Семья"",""participants"":[""gx56q"",""dudefromtheInternet"",""yellooot"", ""xoposhiy""]}"),
            PersonList.FromJson(@"{""id"":""2"",""name"":""Коллеги"",""participants"":[""gx56q"",""dudefromtheInternet"",""yellooot"", ""xoposhiy""]}"),
            PersonList.FromJson(@"{""id"":""3"",""name"":""Друзья"",""participants"":[""gx56q"",""dudefromtheInternet"",""yellooot"", ""xoposhiy""]}"),
            PersonList.FromJson(@"{""id"":""4"",""name"":""Семья2"",""participants"":[""gx56q"",""dudefromtheInternet"",""yellooot"", ""xoposhiy""]}"),
            PersonList.FromJson(@"{""id"":""5"",""name"":""Коллеги2"",""participants"":[""gx56q"",""dudefromtheInternet"",""yellooot"", ""xoposhiy""]}"),
            PersonList.FromJson(@"{""id"":""6"",""name"":""Друзья2"",""participants"":[""gx56q"",""dudefromtheInternet"",""yellooot"", ""xoposhiy""]}"),
            PersonList.FromJson(@"{""id"":""7"",""name"":""Семья3"",""participants"":[""gx56q"",""dudefromtheInternet"",""yellooot"", ""xoposhiy""]}")
        };
    }
    
    
    private readonly IMessageView messageView;
    private readonly IBucket bucket;
    private readonly IBotDatabase botDatabase;
    private readonly IMainHandler mainHandler;

    public MyListsHandler(
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
        
        
    public async Task EditListParticipants(Message message)
    {
        var text = message.Text;
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;
        if (text == "Отмена")
        {
            await CancelAction(new Message());
            return;
        }
        await bucket.WriteUserState(userId, State.EditingList);
        await messageView.SayWithKeyboard("Участники изменены!", fromChatId, mainHandler.GetMainKeyboard());
    }

    public async Task EditListName(Message message)
    {
        var text = message.Text;
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;
        if (text == "Отмена")
        {
            await CancelAction(new Message());
            return;
        }
        await bucket.WriteUserState(userId, State.EditingList);
        await messageView.SayWithKeyboard("Название изменено!", fromChatId, mainHandler.GetMainKeyboard());
    }
    
    public async Task ActionInterrupted(Message message)
    {
        var fromChatId = message.Chat.Id;
        await messageView.Say("Сначала закончите редактирование списка", fromChatId);
    }

    private async Task CancelAction(Message message)
    {
        var userId = message.From!.Id;
        var fromChatId = message.Chat.Id;
        await bucket.WriteUserState(userId, State.EditingList);
        await messageView.SayWithKeyboard("Ввод отменен", fromChatId, mainHandler.GetMainKeyboard());
    }
    
    public async Task HandleMyPeopleCommand(Message message)
    {
        var fromChatId = message.Chat.Id;
        var userId = message.From!.Id;
        
        var peopleLists = RetrievePeople(userId);
        if (peopleLists.Any())
        {
            var inlineKeyboard = GetPeopleInlineKeyboard(peopleLists);
            await messageView.SayWithInlineKeyboard("Ваши люди:", fromChatId, inlineKeyboard);
        }
        else
        {
            await messageView.SayWithKeyboard("У вас нет людей", fromChatId, mainHandler.GetMainKeyboard());
        }
    }
    
    private static InlineKeyboardMarkup GetPeopleInlineKeyboard(IReadOnlyList<PersonList> people, 
        int currentPage = 1, int peoplePerPage = 6)
    {
        const int columnCount = 2; // Number of columns for buttons
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();

        var startIndex = (currentPage - 1) * peoplePerPage;
        var endIndex = Math.Min(startIndex + peoplePerPage, people.Count);

        for (var i = startIndex; i < endIndex; i++)
        {
            var personButton = new InlineKeyboardButton(people[i].Name)
            {
                CallbackData = $"showPersonList_{people[i].Id}"
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

        if (endIndex < people.Count)
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

        var myPeople = RetrievePeople(userId);
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
        var personLists = RetrievePeople(userId);
        var myPersonList = personLists.FirstOrDefault(p => p.Id == listId);

        if (myPersonList != null)
        {
            var messageText = GetPersonListMessageText(myPersonList);
            var inlineKeyboard = GetPersonListInlineKeyboard(myPersonList);
            await messageView.EditInlineMessage(messageText, chatId, messageId, inlineKeyboard);
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }
    
    private static string GetPersonListMessageText(PersonList myPersonList)
    {
        var messageText = new StringBuilder();
        messageText.AppendLine($"Имя списка: {myPersonList.Name}");
        messageText.AppendLine("Участники:");
        foreach (var participant in myPersonList.Participants)
            messageText.AppendLine($"<a href=\"@{participant}\"> @{participant}</a>");
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
        
        var personLists = RetrievePeople(userId);
        var myPersonList = personLists.FirstOrDefault(pl => pl.Id == personListId);

        if (myPersonList != null)
        {
            await bucket.WriteUserState(userId, State.EditingList);
            var inlineKeyboard = GetEditPersonListInlineKeyboard(myPersonList);
            var messageText = GetPersonListMessageText(myPersonList);

            await messageView.EditInlineMessage(messageText, chatId, messageId, inlineKeyboard);
        }
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }
    
    private static InlineKeyboardMarkup GetEditPersonListInlineKeyboard(PersonList myPersonList)
    {
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();
        
        // TODO: обработка
        var editNameButton = new InlineKeyboardButton("Изменить имя")
        {
            CallbackData = $"edit_ListName_{myPersonList.Id}"
        };
        
        // TODO: обработка
        var editParticipantsButton = new InlineKeyboardButton("Изменить участников")
        {
            CallbackData = $"edit_ListParticipants_{myPersonList.Id}"
        };

        var backButton = new InlineKeyboardButton("Назад")
        {
            CallbackData = $"showPersonList_{myPersonList.Id}"
        };
        
        var saveButton = new InlineKeyboardButton("Сохранить")
        {
            CallbackData = $"savePersonList_{myPersonList.Id}"
        };

        inlineKeyboard.Add(new List<InlineKeyboardButton> { editNameButton });
        inlineKeyboard.Add(new List<InlineKeyboardButton> { editParticipantsButton });
        inlineKeyboard.Add(new List<InlineKeyboardButton> { backButton, saveButton });

        return new InlineKeyboardMarkup(inlineKeyboard);
    }

    public async Task HandleCreatePersonListAction(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        // TODO: add db
        await messageView.EditInlineMessage("Лучше пива выпей", chatId, messageId, null);
        await messageView.AnswerCallbackQuery(callbackQuery.Id, null);
    }

    public async Task HandleSavePersonList(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var callbackQueryId = callbackQuery.Id;
        var data = callbackQuery.Data!.Split('_');
        var personListId = data[1];
        // TODO: save to db
        // await bucket.ClearPersonListDraft(userId);
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
        var myPeople = RetrievePeople(userId);
        var peopleKeyboard = GetPeopleInlineKeyboard(myPeople);
        await messageView.EditInlineMessage("Ваши люди:", chatId, messageId, peopleKeyboard);
        await messageView.AnswerCallbackQuery(callbackQueryId, null);
    }
}