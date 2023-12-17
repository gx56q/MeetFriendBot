using AspNetCore.Yandex.ObjectStorage;

namespace Infrastructure.S3Storage;

public static class BotBucketService
{
    public static YandexStorageService CreateBotBucketService(this Configuration configuration)
    {
        return new YandexStorageService(configuration.YandexStorageOptions);
    }
}