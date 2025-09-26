using System.CommandLine;
using System.CommandLine.Parsing;
using MpvYt;

var identifierArgument = new Argument<string?>("identifier", "YouTube URL or Video ID.");
identifierArgument.AddValidator(result =>
{
    var identifier = result.GetValueForArgument(identifierArgument);
    if (identifier is not null && YouTube.ExtractVideoId(identifier) is null)
    {
        result.ErrorMessage = $"Invalid YouTube URL or Video ID: '{identifier}'";
    }
});

var qualityOption = new Option<string?>(["-q", "--quality"], "Stream quality (e.g., 720p, highest, lowest).");
var languageOption = new Option<string?>(["-l", "--language"], "Audio language (e.g., en, ja, es).");
var audioOnlyOption = new Option<bool>(["-a", "--audio"], "Play audio only.");

var rootCommand = new RootCommand("Play YouTube videos in mpv.")
{
    identifierArgument,
    qualityOption,
    languageOption,
    audioOnlyOption
};

rootCommand.SetHandler(async (identifier, quality, language, audioOnly) =>
{
    if (audioOnly && !string.IsNullOrWhiteSpace(quality))
    {
        Console.WriteLine("Info: --audio flag is present, --quality flag will be ignored.");
    }
    await MainLogic(identifier, quality, language, audioOnly);
}, identifierArgument, qualityOption, languageOption, audioOnlyOption);

return await rootCommand.InvokeAsync(args);

static async Task MainLogic(string? identifier, string? quality, string? language, bool audioOnly)
{
    if (!Mpv.IsAvailable())
    {
        Console.Error.WriteLine("Error: 'mpv' executable not found in your system's PATH.");
        Console.Error.WriteLine("Please install mpv and ensure its location is included in the PATH environment variable.");
        Environment.Exit(1);
        return;
    }
    
    try
    {
        string id = identifier ?? Ui.GetIdentifierFromInput();
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("Error: No identifier provided.");
            Environment.Exit(1);
            return;
        }

        string? videoId = YouTube.ExtractVideoId(id);
        if (videoId is null)
        {
            Console.Error.WriteLine($"Error: Invalid YouTube URL or Video ID: '{id}'");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine($"Fetching video data for '{videoId}'...");
        var playerData = await YouTube.GetPlayerDataAsync(videoId);

        Console.Clear();
        Console.WriteLine(playerData.Title);
        Console.WriteLine(new string('─', Math.Min(Console.WindowWidth - 1, playerData.Title.Length)));
        Console.WriteLine();

        var selectedStream = Ui.GetStreamSelection(playerData, quality, language, audioOnly);

        if (selectedStream is null)
        {
            Environment.Exit(0);
            return;
        }

        switch (selectedStream)
        {
            case VideoSelection vs:
                Mpv.Launch(playerData.Title, playerData.ThumbnailUrl, vs.Video, vs.Audio);
                break;
            case AudioSelection aud:
                Mpv.Launch(playerData.Title, playerData.ThumbnailUrl, null, aud.Audio);
                break;
        }
    }
    catch (YouTubeApiError e)
    {
        Console.Error.WriteLine(e.Message);
        Environment.Exit(1);
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Error: {e.Message}");
        Environment.Exit(1);
    }
}