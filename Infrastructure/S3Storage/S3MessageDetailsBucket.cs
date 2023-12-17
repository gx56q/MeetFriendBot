using System.Net;
using AspNetCore.Yandex.ObjectStorage;
using AspNetCore.Yandex.ObjectStorage.Object;
using Bot.Domain;
using Domain;
using FluentResults;
using Newtonsoft.Json;

namespace Infrastructure.S3Storage;


public class S3ApiException : Exception
{
    private S3ApiException(string message)
        : base(message) { }
    
    public S3ApiException(string message, Exception innerException)
        : base(message, innerException: innerException) { }

    public static S3ApiException FromResult<T>(Result<T> result, HttpMethod method, string path)
    {
        return new S3ApiException(
            $"Error during {method.Method} {path}: " + 
            $"{string.Join('\n', result.Errors.Select(e => e.Message))}"
        );
    }
}


public class S3Bucket : IBucket
{
    private const string PicturesFolder = "pictures";
    private const string StateFolder = "states";
    private const string DraftFolder = "drafts";
    private readonly IObjectService objectService;
    public S3Bucket(IYandexStorageService storageService)
    {
        objectService = storageService.ObjectService;
    }

    public async Task<State> GetUserState(long userId)
    {
        var filename = GetFilename(StateFolder+"/"+userId+"_state");
        var response = await objectService.GetAsync(filename);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return State.Start;
        }
        if (!response.IsSuccessStatusCode)
        {
            throw S3ApiException.FromResult(
                response.ToResult(),
                HttpMethod.Get,
                filename
            );
        }
        var body = await response.ReadAsByteArrayAsync();
        var content = System.Text.Encoding.UTF8.GetString(body.Value);
        return JsonConvert.DeserializeObject<State>(content);
    }
    
    public async Task WriteUserState(long userId, State state)
    {
        var filename = GetFilename(StateFolder+"/"+userId+"_state");
        var response = await objectService.PutAsync(
            System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(state)),
            filename
        );
        if (!response.IsSuccessStatusCode)
        {
            throw S3ApiException.FromResult(
                response.ToResult(),
                HttpMethod.Put,
                filename
            );
        }
    }
    
    public async Task WriteEventDraft(long userId, Domain.Event draft)
    {
        var filename = GetFilename(DraftFolder+"/"+userId+"_draft");
        var response = await objectService.PutAsync(
            System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(draft)),
            filename
        );
        if (!response.IsSuccessStatusCode)
        {
            throw S3ApiException.FromResult(
                response.ToResult(),
                HttpMethod.Put,
                filename
            );
        }
    }
    
    public async Task<Event> GetEventDraft(long userId)
    {
        var filename = GetFilename(DraftFolder+"/"+userId+"_draft");
        var response = await objectService.GetAsync(filename);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new Domain.Event(userId);
        }
        if (!response.IsSuccessStatusCode)
        {
            throw S3ApiException.FromResult(
                response.ToResult(),
                HttpMethod.Get,
                filename
            );
        }
        var body = await response.ReadAsByteArrayAsync();
        var content = System.Text.Encoding.UTF8.GetString(body.Value);
        return JsonConvert.DeserializeObject<Domain.Event>(content)!;
    }
    
    public async Task ClearEventDraft(long userId)
    {
        var filename = GetFilename(DraftFolder+"/"+userId+"_draft");
        var response = await objectService.DeleteAsync(filename);

        if (!response.IsSuccessStatusCode)
        {
            throw S3ApiException.FromResult(
                response.ToResult(),
                HttpMethod.Delete,
                filename
            );
        }
    }
    
    public async Task WriteEventPicture(string eventId, string fileId)
    {
        var filename = GetFilename(PicturesFolder+"/"+eventId+"_picture");
        var response = await objectService.PutAsync(
            System.Text.Encoding.UTF8.GetBytes(fileId),
            filename
        );
        
        if (!response.IsSuccessStatusCode)
        {
            throw S3ApiException.FromResult(
                response.ToResult(),
                HttpMethod.Put,
                filename
            );
        }
    }
    
    public async Task<string?> GetEventPicture(string eventId)
    {
        var filename = GetFilename(PicturesFolder+"/"+eventId+"_picture");
        var response = await objectService.GetAsync(filename);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        if (!response.IsSuccessStatusCode)
        {
            throw S3ApiException.FromResult(
                response.ToResult(),
                HttpMethod.Get,
                filename
            );
        }
        var body = await response.ReadAsByteArrayAsync();
        return System.Text.Encoding.UTF8.GetString(body.Value);
    }
    
    public async Task DeleteEventPicture(string eventId)
    {
        var filename = GetFilename(PicturesFolder+"/"+eventId+"_picture");
        var response = await objectService.DeleteAsync(filename);

        if (!response.IsSuccessStatusCode)
        {
            throw S3ApiException.FromResult(
                response.ToResult(),
                HttpMethod.Delete,
                filename
            );
        }
    }
    
    private static string GetFilename(string filename) => $"{filename}.json";
}