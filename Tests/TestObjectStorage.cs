using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Yandex.ObjectStorage;
using Bot;
using Bot.Services.S3Storage;
using Bot.Services.Telegram;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace Tests;

public static class ObjectStorage
{
    public static readonly S3Bucket Bucket;

    static ObjectStorage()
    {
        var config = Configuration.FromJson("testsettings.json");
        
        Bucket = new S3Bucket(
            new YandexStorageService(config.YandexStorageOptions)
        );
    }
}


public class TestObjectStorage
{
    private static S3Bucket S3Bucket => ObjectStorage.Bucket;
    
    [Test]
    public async Task TestGetUserState()
    {
        var state = await S3Bucket.GetUserState(-1);
        Assert.AreEqual(State.Start, state);
    }
    
    [Test]
    public async Task TestWriteUserState()
    {
        const int userId = -1;
        const State state = State.Start;
        await S3Bucket.WriteUserState(userId, state);
        var newState = await S3Bucket.GetUserState(userId);
        Assert.AreEqual(state, newState);
    }


    
}