using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


namespace Bot.Services.Telegram;

public interface IMessageView
{
    Task<Message> Say(string text, long chatId);
    Task ShowHelp(long chatId, ReplyKeyboardMarkup? keyboard);
    Task ShowHelpWithKeyboard(long chatId, ReplyKeyboardMarkup keyboard);
    Task SendFile(long chatId, byte[] content, string filename, string caption);
    Task SendPicture(long chatId, byte[] picture, string caption);
    Task<Message> SayWithKeyboard(string text, long chatId, ReplyKeyboardMarkup keyboard);
    Task<Message> SayWithInlineKeyboard(string text, long chatId, InlineKeyboardMarkup keyboard);
    Task AnswerCallbackQuery(string callbackQueryId, string? text);
    Task EditInlineMessage(string text, long chatId, int messageId, InlineKeyboardMarkup? keyboard);
    Task EditInlineKeyboard(string text, long chatId, int messageId, InlineKeyboardMarkup keyboard);
    Task<byte[]> DownloadFile(string fileId);
    Task ForwardMessage(long chatId, long fromChatId, int messageId);
    Task DeleteMessage(long chatId, int messageId);
    Task EditInlineMessageWithPhoto(string text, long chatId, int messageId, InlineKeyboardMarkup? keyboard, string? mediaId);
    Task<Message> SayWithInlineKeyboardAndPhoto(string text, long chatId, InlineKeyboardMarkup keyboard, string? mediaId);
    Task<Message> SendDice(long chatId, Emoji emoji);
    Task ShowErrorToDevops(Update incomingUpdate, string errorMessage);
}

public class HtmlMessageView : IMessageView
{
    private readonly ITelegramBotClient botClient;
    private readonly ChatId devopsChatId;

    public HtmlMessageView(ITelegramBotClient client, ChatId devopsChatId)
    {
        botClient = client;
        this.devopsChatId = devopsChatId;
    }
    
    public async Task ShowReplyKeyboard(string text,long chatId, ReplyKeyboardMarkup keyboard)
    {
        var message = await botClient.SendTextMessageAsync(
            chatId,
            text,
            replyMarkup: keyboard
        );
        await botClient.DeleteMessageAsync(chatId, message.MessageId);
    }


    public async Task<Message> Say(string text, long chatId)
    {
        return await botClient.SendTextMessageAsync(
            chatId,
            text,
            parseMode: ParseMode.Html
        );
    }
    
    public async Task<Message> SayWithKeyboard(string text, long chatId, ReplyKeyboardMarkup? keyboard)
    {
        return await botClient.SendTextMessageAsync(
            chatId,
            text,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }
    
    public async Task<Message> SayWithInlineKeyboard(string text, long chatId, InlineKeyboardMarkup keyboard)
    {
        var result = await botClient.SendTextMessageAsync(
            chatId,
            text,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
        return result;
    }

    public async Task<Message> SayWithInlineKeyboardAndPhoto(string text, long chatId, InlineKeyboardMarkup keyboard,
        string? mediaId)
    {
        if (mediaId == null)
        {
            return await SayWithInlineKeyboard(text, chatId, keyboard);
        }

        var inputFile = new InputFileId(mediaId);
        var result = await botClient.SendPhotoAsync(
            chatId,
            inputFile,
            caption: text,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
        return result;
    }
    
    public async Task ForwardMessage(long chatId, long fromChatId, int messageId)
    {
        await botClient.ForwardMessageAsync(
            chatId,
            fromChatId,
            messageId
        );
    }
    
    public async Task DeleteMessage(long chatId, int messageId)
    {
        await botClient.DeleteMessageAsync(
            chatId,
            messageId
        );
    }
    
    public async Task EditInlineMessage(string text, long chatId, int messageId, InlineKeyboardMarkup? keyboard)
    {
        try
        {
            await botClient.EditMessageTextAsync(
                chatId,
                messageId,
                text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        catch (ApiRequestException e) when (e.Message.Contains("message is not modified"))
        {
            
        }
        catch (ApiRequestException e) when (e.Message.Contains("there is no text in the message to edit"))
        {
            await botClient.DeleteMessageAsync(chatId, messageId);
            await SayWithInlineKeyboard(text, chatId, keyboard!);
        }
    }
    
    public async Task EditInlineMessageWithPhoto(string text, long chatId, int messageId, 
        InlineKeyboardMarkup? keyboard, string? mediaId)
    {
        try
        {
            if (mediaId != null)
            {
                var input = new InputFileId(mediaId);
                await botClient.EditMessageMediaAsync(
                    chatId,
                    messageId,
                    new InputMediaPhoto(input),
                    replyMarkup: keyboard
                );
            }
            await botClient.EditMessageCaptionAsync(
                chatId,
                messageId,
                text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        catch (ApiRequestException e) when (e.Message.Contains("message is not modified"))
        {
            
        }
        catch (ApiRequestException e) when (e.Message.Contains("there is no text in the message to edit"))
        {
            
        }
        catch (ApiRequestException e) when (e.Message.Contains("there is no caption in the message to edit"))
        {
            await botClient.EditMessageTextAsync(
                chatId,
                messageId,
                text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }
    }
    
    public async Task EditInlineKeyboard(string text, long chatId, int messageId, InlineKeyboardMarkup keyboard)
    {
        try
        {
            await botClient.EditMessageReplyMarkupAsync(
                chatId,
                messageId,
                replyMarkup: keyboard
            );
        }
        catch (ApiRequestException e) when (e.Message.Contains("message is not modified"))
        {
            
        }
    }
    
    public async Task ShowHelp(long chatId, ReplyKeyboardMarkup? keyboard)
    {
        await SayWithKeyboard(
            "Помощи нет",
            chatId,
            keyboard
        );
    }
    
    public async Task ShowHelpWithKeyboard(long chatId, ReplyKeyboardMarkup keyboard)
    {
        await Say(
            "Помощи нет",
            chatId
        );
    }
    
    public async Task AnswerCallbackQuery(string callbackQueryId, string? text)
    {
        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId,
            text
        );
    }

    public async Task SendFile(long chatId, byte[] content, string filename, string caption)
    {
        await botClient.SendDocumentAsync(
            chatId,
            InputFile.FromStream(new MemoryStream(content), filename),
            caption: EscapeForHtml(caption)
        );
    }

    public async Task SendPicture(long chatId, byte[] picture, string caption)
    {
        await botClient.SendPhotoAsync(
            chatId,
            InputFile.FromStream(new MemoryStream(picture)),
            caption: EscapeForHtml(caption)
        );
    }
    
    public async Task<byte[]> DownloadFile(string fileId)
    {
        var file = await botClient.GetFileAsync(fileId);
        var stream = new MemoryStream();
        if (file.FilePath != null) await botClient.DownloadFileAsync(file.FilePath, stream);
        return stream.ToArray();
    }
    
    public async Task<Message> SendDice(long chatId, Emoji emoji)
    {
        return await botClient.SendDiceAsync(chatId, emoji: emoji);
    }
    
    private string FormatErrorHtml(Update incomingUpdate, string errorMessage)
    {
        var formattedUpdate = FormatIncomingUpdate(incomingUpdate);
        var formattedError = EscapeForHtml(errorMessage);
        return $"Error handling message: {formattedUpdate}\n\nError:\n<pre>{formattedError}</pre>";
    }

    public string FormatIncomingUpdate(Update incomingUpdate)
    {
        var incoming = incomingUpdate.Type switch
        {
            UpdateType.Message => $"From: {incomingUpdate.Message!.From} Message: {incomingUpdate.Message!.Text}",
            UpdateType.EditedMessage =>
                $"From: {incomingUpdate.EditedMessage!.From} Edit: {incomingUpdate.EditedMessage!.Text}",
            UpdateType.InlineQuery =>
                $"From: {incomingUpdate.InlineQuery!.From} Query: {incomingUpdate.InlineQuery!.Query}",
            UpdateType.CallbackQuery =>
                $"From: {incomingUpdate.CallbackQuery!.From} Query: {incomingUpdate.CallbackQuery.Data}",
            _ => $"Message with type {incomingUpdate.Type}"
        };

        return
            $"<pre>{EscapeForHtml(incoming)}</pre>";
    }
    
    public async Task ShowErrorToDevops(Update incomingUpdate, string errorMessage)
    {
        await botClient.SendTextMessageAsync(devopsChatId, FormatErrorHtml(incomingUpdate, errorMessage),
            parseMode: ParseMode.Html);
    }
    
    private static string EscapeForHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}