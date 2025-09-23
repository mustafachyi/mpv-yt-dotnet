namespace MpvYt;

public static class Ui
{
    public static Task<string> GetIdentifierFromInputAsync()
    {
        Console.Write("Enter YouTube URL or Video ID: ");
        return Task.FromResult(Console.ReadLine()?.Trim() ?? "");
    }

    public static async Task<StreamSelection?> GetStreamSelectionAsync(PlayerData data, string? qualityPref, bool audioOnly)
    {
        if (audioOnly) return new AudioSelection();

        var videos = data.Videos;
        if (videos.Count == 0)
        {
            Console.WriteLine("No video streams available, defaulting to audio only.");
            return new AudioSelection();
        }

        if (!string.IsNullOrWhiteSpace(qualityPref))
        {
            return SelectStreamFromPreference(videos, qualityPref);
        }

        Console.WriteLine($"Available streams for: {data.Title}");
        return await SelectStreamInteractiveAsync(videos);
    }

    private static StreamSelection SelectStreamFromPreference(List<VideoStream> videos, string qualityPref)
    {
        if (qualityPref.Equals("highest", StringComparison.OrdinalIgnoreCase)) return new VideoSelection(videos[0]);
        if (qualityPref.Equals("lowest", StringComparison.OrdinalIgnoreCase)) return new VideoSelection(videos[^1]);

        var match = videos.Find(v => v.Quality.Equals(qualityPref, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return new VideoSelection(match);
        }

        if (!int.TryParse(string.Concat(qualityPref.Where(char.IsDigit)), out int requestedQualityNum))
        {
            Console.WriteLine($"Warning: Could not parse quality '{qualityPref}'. Playing highest available quality ('{videos[0].Quality}') instead.");
            return new VideoSelection(videos[0]);
        }

        VideoStream closestStream = videos
            .OrderBy(v => Math.Abs(int.Parse(string.Concat(v.Quality.Where(char.IsDigit))) - requestedQualityNum))
            .First();
        
        Console.WriteLine($"Warning: Quality '{qualityPref}' not found. Playing closest available quality ('{closestStream.Quality}') instead.");
        return new VideoSelection(closestStream);
    }

    private static async Task<StreamSelection?> SelectStreamInteractiveAsync(List<VideoStream> videos)
    {
        for (int i = 0; i < videos.Count; i++)
        {
            var video = videos[i];
            long bitrateKbps = video.Bitrate / 1000;
            string indicator = (i == 0) ? " (highest)" : "";
            Console.WriteLine($"{i + 1}) {video.Quality}{indicator} ({bitrateKbps} kbps)");
        }

        int audioIndex = videos.Count + 1;
        Console.WriteLine($"{audioIndex}) Audio Only");
        Console.Write($"Select quality [1-{audioIndex}, default: 1]: ");

        string? line = await Task.FromResult(Console.ReadLine());

        if (string.IsNullOrWhiteSpace(line))
        {
            return new VideoSelection(videos[0]);
        }

        if (int.TryParse(line, out int choice))
        {
            if (choice >= 1 && choice <= videos.Count)
            {
                return new VideoSelection(videos[choice - 1]);
            }
            if (choice == audioIndex)
            {
                return new AudioSelection();
            }
        }
        return null;
    }
}