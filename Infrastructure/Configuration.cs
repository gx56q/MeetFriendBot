using AspNetCore.Yandex.ObjectStorage.Configuration;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public class Configuration
{
    public YandexStorageOptions YandexStorageOptions { get; }
    public string TelegramToken => appSettings[nameof(TelegramToken)]!;
    public string YdbEndpoint => appSettings[nameof(YdbEndpoint)]!;
    public string YdbPath => appSettings[nameof(YdbPath)]!;
    public long DevopsChatId => long.Parse(appSettings[nameof(DevopsChatId)]!);
    public string? IamTokenPath => appSettings[nameof(IamTokenPath)];

    private readonly IConfigurationSection appSettings;

    private Configuration(IConfigurationSection appSettings, YandexStorageOptions cloudStorageOptions)
    {
        this.appSettings = appSettings;
        YandexStorageOptions = cloudStorageOptions;
    }

    public static Configuration FromJson(string path)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(path, optional: false, reloadOnChange: true)
            .Build();

        return new Configuration(
            configuration.GetSection("AppSettings"),
            configuration.GetYandexStorageOptions("CloudStorageOptions")
        );
    }
}