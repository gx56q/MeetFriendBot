using Bot.Services.S3Storage;
using Bot.Services.Telegram;
using Bot.Services.Telegram.Commands;
using Bot.Services.YDB;
using Grpc.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Yandex.Cloud.Functions;
using TestMultiple;
// ReSharper disable UnusedAutoPropertyAccessor.Global


namespace Bot;

public class Response
{
    public int StatusCode { get; set; }
    public string Body { get; set; }

    public Response(int statusCode, string body)
    {
        StatusCode = statusCode;
        Body = body;
    }
}

// ReSharper disable once UnusedType.Global
public class TelegramHandler : YcFunction<string, Response>
{
    private const string CodePath = "/function/code/";
    
    public Response FunctionHandler(string request, Context context)
    {
        var logger = new ConsoleLogger();
        var configuration = Configuration.FromJson(CodePath + "settings.json");
        var tgClient = new TelegramBotClient(configuration.TelegramToken);
        logger.ForType<string>().Info(request);
        try
        {
            var body = JObject.Parse(request).GetValue("body")!.Value<string>()!;
            var update = JsonConvert.DeserializeObject<Update>(body)!;
            var view = new HtmlMessageView(tgClient, configuration.DevopsChatId);
            var bucketRepo = new S3Bucket(configuration.CreateBotBucketService());
            var botDatabase = new BotDatabase(configuration);

            var commands = new IChatCommandHandler[]
            {
                new StartCommandHandler(view),
                new HelpCommandHandler(view),
                new LudkaCommandHandler(view)
            };

            var updateService = new HandleUpdateService(view, commands, bucketRepo, botDatabase);
            updateService.Handle(update).Wait();
            if (GetSender(update) != configuration.DevopsChatId)
                // tgClient.SendTextMessageAsync(settings.DevopsChatId, presenter.FormatIncomingUpdate(update), null, parseMode: ParseMode.Html);
                logger.Info(view.FormatIncomingUpdate(update));
            return new Response(200, "ok");
        }
        catch (Exception e)
        {
            logger.ForType<Exception>().Error(e, "Error");
            tgClient.SendTextMessageAsync(configuration.DevopsChatId, "Request:\n\n" + request + "\n\n" + e).Wait();
            return new Response(500, $"Error {e}");
        }
    }
    
    private static ChatId GetSender(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message!.From!.Id,
            UpdateType.InlineQuery => update.InlineQuery!.From.Id,
            UpdateType.EditedMessage => update.EditedMessage!.Chat.Id,
            UpdateType.CallbackQuery => update.CallbackQuery!.From.Id,
            _ => 0
        };
    }
}

