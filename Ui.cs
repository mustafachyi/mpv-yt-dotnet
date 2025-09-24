using System.Globalization;

namespace MpvYt;

public static class Ui
{
    public static string GetIdentifierFromInput()
    {
        Console.Write("Enter YouTube URL or Video ID: ");
        return Console.ReadLine()?.Trim() ?? "";
    }

    public static StreamSelection? GetStreamSelection(PlayerData data, string? qualityPref, string? langPref, bool audioOnly)
    {
        if (audioOnly)
        {
            Console.Clear();
            var audio = SelectAudio(data.Audios, langPref);
            return audio is not null ? new AudioSelection(audio) : null;
        }

        if (data.Videos.Count == 0)
        {
            Console.WriteLine("No video streams available, attempting audio only.");
            var audio = SelectAudio(data.Audios, langPref);
            return audio is not null ? new AudioSelection(audio) : null;
        }

        var video = SelectVideo(data.Videos, qualityPref);
        if (video is null) return null;

        Console.Clear();
        var selectedAudio = SelectAudio(data.Audios, langPref);
        return selectedAudio is not null ? new VideoSelection(video, selectedAudio) : null;
    }

    private static VideoStream? SelectVideo(List<VideoStream> videos, string? qualityPref)
    {
        if (string.IsNullOrWhiteSpace(qualityPref))
        {
            Console.WriteLine("Available video qualities:");
            for (int i = 0; i < videos.Count; i++)
            {
                var v = videos[i];
                string indicator = (i == 0) ? " (highest)" : "";
                Console.WriteLine($"{i + 1}) {v.Quality}{indicator} ({v.Bitrate / 1000} kbps)");
            }
            Console.Write($"Select quality [1-{videos.Count}, default: 1]: ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) return videos[0];
            if (int.TryParse(line, out int choice) && choice >= 1 && choice <= videos.Count)
            {
                return videos[choice - 1];
            }
            return null;
        }

        if (qualityPref.Equals("highest", StringComparison.OrdinalIgnoreCase)) return videos[0];
        if (qualityPref.Equals("lowest", StringComparison.OrdinalIgnoreCase)) return videos[^1];

        var match = videos.Find(v => v.Quality.Equals(qualityPref, StringComparison.OrdinalIgnoreCase));
        if (match is not null) return match;

        if (!int.TryParse(string.Concat(qualityPref.Where(char.IsDigit)), out int requestedQualityNum))
        {
            Console.WriteLine($"Warning: Could not parse quality '{qualityPref}'. Playing highest available quality ('{videos[0].Quality}') instead.");
            return videos[0];
        }

        var closestStream = videos
            .OrderBy(v => Math.Abs(int.Parse(string.Concat(v.Quality.Where(char.IsDigit))) - requestedQualityNum))
            .First();
        
        Console.WriteLine($"Warning: Quality '{qualityPref}' not found. Playing closest available quality ('{closestStream.Quality}') instead.");
        return closestStream;
    }

    private static AudioStream? SelectAudio(List<AudioStream> audios, string? langPref)
    {
        if (audios.Count == 1)
        {
            return audios[0];
        }

        if (!string.IsNullOrWhiteSpace(langPref))
        {
            var match = audios.Find(a => a.Language.Equals(langPref, StringComparison.OrdinalIgnoreCase))
                        ?? audios.Find(a => a.Language.StartsWith(langPref, StringComparison.OrdinalIgnoreCase));

            if (match is not null) return match;

            Console.WriteLine($"Warning: Language '{langPref}' not found. Falling back to default selection.");
        }

        int defaultIndex = audios.FindIndex(a => a.IsDefault);
        if (defaultIndex == -1)
        {
            int englishIndex = audios.FindIndex(a => a.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            defaultIndex = (englishIndex != -1) ? englishIndex : 0;
        }

        var languageGroups = audios.GroupBy(a => GetNormalizedLanguageName(a.Language)).ToDictionary(g => g.Key, g => g.Count());

        Console.WriteLine("Available audio languages:");
        for (int i = 0; i < audios.Count; i++)
        {
            var a = audios[i];
            string indicator = (i == defaultIndex) ? " (default)" : "";
            string normalizedName = GetNormalizedLanguageName(a.Language);
            
            string displayName = languageGroups[normalizedName] > 1 ? $"{normalizedName} ({a.Name})" : normalizedName;

            Console.WriteLine($"{i + 1}) {displayName} ({a.Bitrate / 1000} kbps){indicator}");
        }
        Console.Write($"Select language [1-{audios.Count}, default: {defaultIndex + 1}]: ");
        string? line = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(line)) return audios[defaultIndex];
        if (int.TryParse(line, out int choice) && choice >= 1 && choice <= audios.Count)
        {
            return audios[choice - 1];
        }
        return null;
    }

    private static string GetNormalizedLanguageName(string langCode)
    {
        if (string.IsNullOrWhiteSpace(langCode) || langCode.Equals("und", StringComparison.OrdinalIgnoreCase))
        {
            return "Original";
        }
        try
        {
            var culture = new CultureInfo(langCode);
            return culture.EnglishName;
        }
        catch (CultureNotFoundException)
        {
            return langCode;
        }
    }
}