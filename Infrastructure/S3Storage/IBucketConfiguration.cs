using AspNetCore.Yandex.ObjectStorage.Configuration;

namespace Infrastructure.S3Storage;

public interface IBucketConfiguration
{
    public YandexStorageOptions YandexStorageOptions { get; }
}