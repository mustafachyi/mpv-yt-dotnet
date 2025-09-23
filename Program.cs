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
var audioOnlyOption = new Option<bool>(["-a", "--audio"], "Play audio only.");

var rootCommand = new RootCommand("Play YouTube videos in mpv.")
{
    identifierArgument,
    qualityOption,
    audioOnlyOption
};

rootCommand.SetHandler(async (identifier, quality, audioOnly) =>
{
    if (audioOnly && !string.IsNullOrWhiteSpace(quality))
    {
        Console.WriteLine("Info: --audio flag is present, --quality flag will be ignored.");
    }
    await MainLogic(identifier, quality, audioOnly);
}, identifierArgument, qualityOption, audioOnlyOption);

return await rootCommand.InvokeAsync(args);

static async Task MainLogic(string? identifier, string? quality, bool audioOnly)
{
    try
    {
        string id = identifier ?? await Ui.GetIdentifierFromInputAsync();
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
        var selectedStream = await Ui.GetStreamSelectionAsync(playerData, quality, audioOnly);

        if (selectedStream is null)
        {
            Console.WriteLine("No stream selected.");
            Environment.Exit(0);
            return;
        }

        switch (selectedStream)
        {
            case VideoSelection vs:
                Mpv.Launch(playerData, vs.Stream);
                break;
            case AudioSelection:
                Mpv.Launch(playerData, null);
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