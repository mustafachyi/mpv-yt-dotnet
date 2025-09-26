using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MpvYt;

public sealed class YouTubeApiError : Exception
{
    public YouTubeApiError(string message) : base(message) { }
}

public static partial class YouTube
{
    [GeneratedRegex(@"^[a-zA-Z0-9_-]{11}$")]
    private static partial Regex VideoIdFormatRegex();

    [GeneratedRegex(@"(?:v=|youtu\.be\/|\/shorts\/|\/embed\/|\/live\/|\/v\/)([a-zA-Z0-9_-]{11})")]
    private static partial Regex VideoUrlRegex();

    private static readonly HttpClient HttpClient;
    private const string ApiEndpoint = "https://www.youtube.com/youtubei/v1/player";
    private const string ThumbnailBaseUrl = "https://img.youtube.com/vi/";

    static YouTube()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(15),
            AutomaticDecompression = DecompressionMethods.All,
            RequestHeaderEncodingSelector = (_, _) => System.Text.Encoding.UTF8
        };

        HttpClient = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
    }

    public static string? ExtractVideoId(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return null;
        if (VideoIdFormatRegex().IsMatch(identifier))
        {
            return identifier;
        }
        var match = VideoUrlRegex().Match(identifier);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static async Task<PlayerData> GetPlayerDataAsync(string videoId)
    {
        var thumbnailTask = GetHighestQualityThumbnailUrlAsync(videoId);
        var extractionTask = AttemptExtractionAsync(videoId, Client.Android);

        await Task.WhenAll(thumbnailTask, extractionTask);

        var thumbnailUrl = await thumbnailTask;
        var (data, error) = await extractionTask;

        if (data is not null)
        {
            return data with { ThumbnailUrl = thumbnailUrl };
        }

        bool isLoginOrAgeError = error?.Contains("LOGIN_REQUIRED", StringComparison.OrdinalIgnoreCase) == true ||
                                 error?.Contains("age", StringComparison.OrdinalIgnoreCase) == true;

        if (isLoginOrAgeError)
        {
            (data, error) = await AttemptExtractionAsync(videoId, Client.Ios);
            if (data is not null)
            {
                return data with { ThumbnailUrl = thumbnailUrl };
            }
        }

        throw new YouTubeApiError(error ?? "An unknown error occurred while fetching video data.");
    }

    private static async Task<string> GetHighestQualityThumbnailUrlAsync(string videoId)
    {
        string maxResUrl = $"{ThumbnailBaseUrl}{videoId}/maxresdefault.jpg";
        
        using var request = new HttpRequestMessage(HttpMethod.Head, maxResUrl);
        try
        {
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode)
            {
                return maxResUrl;
            }
        }
        catch (HttpRequestException)
        {
            // Network error, fallback to the guaranteed URL.
        }

        return $"{ThumbnailBaseUrl}{videoId}/hqdefault.jpg";
    }

    private static async Task<(PlayerData? Data, string? Error)> AttemptExtractionAsync(string videoId, Client client)
    {
        try
        {
            using var request = client.CreateRequest(videoId);
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return (null, $"API request failed with status code: {response.StatusCode}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync(YouTubeJsonContext.Default.ApiResponse);

            var playabilityStatus = apiResponse?.PlayabilityStatus;
            if (playabilityStatus?.Status != "OK")
            {
                return (null, playabilityStatus?.Reason ?? playabilityStatus?.Status ?? "Video is unplayable.");
            }

            var videoDetails = apiResponse?.VideoDetails;
            var streamingData = apiResponse?.StreamingData;

            if (streamingData is null || string.IsNullOrWhiteSpace(videoDetails?.Title))
            {
                return (null, "Incomplete video data received from API.");
            }
            if (videoDetails.IsLiveContent)
            {
                return (null, "Live streams are not supported.");
            }

            var (videos, audios) = Parser.ParseStreams(streamingData.Value);
            if (audios.Count == 0)
            {
                return (null, "No audio streams available for this video.");
            }

            return (new PlayerData(videoDetails.Title.Trim(), null, videos, audios), null);
        }
        catch (Exception e)
        {
            return (null, e.Message);
        }
    }

    private sealed class Client
    {
        public static readonly Client Android = new("ANDROID", "19.50.42", "3");
        public static readonly Client Ios = new("IOS", "17.13.3", "5", "iPhone14,3");

        private readonly string _name;
        private readonly string _version;
        private readonly string _id;
        private readonly string? _deviceModel;

        private Client(string name, string version, string id, string? deviceModel = null)
        {
            _name = name;
            _version = version;
            _id = id;
            _deviceModel = deviceModel;
        }

        public HttpRequestMessage CreateRequest(string videoId)
        {
            var payload = new ApiRequest(
                Context: new RequestContext(
                    Client: new ClientContext(_name, _version, _deviceModel),
                    User: new UserContext()
                ),
                VideoId: videoId
            );

            var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint)
            {
                Content = JsonContent.Create(payload, YouTubeJsonContext.Default.ApiRequest)
            };
            request.Headers.Add("X-Youtube-Client-Name", _id);
            request.Headers.Add("X-Youtube-Client-Version", _version);
            return request;
        }
    }
}

internal record ApiResponse(PlayabilityStatus? PlayabilityStatus, VideoDetails? VideoDetails, JsonElement? StreamingData);
internal record PlayabilityStatus(string? Status, string? Reason);
internal record VideoDetails(string? Title, bool IsLiveContent);

internal record ApiRequest(RequestContext Context, string VideoId, bool ContentCheckOk = true, bool RacyCheckOk = true);
internal record RequestContext(ClientContext Client, UserContext User);
internal record ClientContext(string ClientName, string ClientVersion, string? DeviceModel = null, string Hl = "en", string Gl = "US");
internal record UserContext(bool LockedSafetyMode = false);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = false)]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(ApiRequest))]
internal partial class YouTubeJsonContext : JsonSerializerContext { }