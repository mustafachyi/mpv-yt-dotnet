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

        bool isInteractive = string.IsNullOrWhiteSpace(qualityPref) && string.IsNullOrWhiteSpace(langPref);
        if (isInteractive)
        {
            Console.Clear();
            Console.WriteLine(data.Title);
            Console.WriteLine(new string('─', Math.Min(Console.WindowWidth - 1, data.Title.Length)));
            Console.WriteLine();
            Console.WriteLine($"Video Quality: {video.Quality}");
        }

        var selectedAudio = SelectAudio(data.Audios, langPref);
        return selectedAudio is not null ? new VideoSelection(video, selectedAudio) : null;
    }

    private static VideoStream? SelectVideo(List<VideoStream> videos, string? qualityPref)
    {
        if (!string.IsNullOrWhiteSpace(qualityPref))
        {
            if (qualityPref.Equals("highest", StringComparison.OrdinalIgnoreCase))
            {
                var stream = videos[0];
                Console.WriteLine($"Video: Selected 'highest' -> {stream.Quality}");
                return stream;
            }
            if (qualityPref.Equals("lowest", StringComparison.OrdinalIgnoreCase))
            {
                var stream = videos[^1];
                Console.WriteLine($"Video: Selected 'lowest' -> {stream.Quality}");
                return stream;
            }

            var match = videos.Find(v => v.Quality.Equals(qualityPref, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                Console.WriteLine($"Video: Matched quality -> {match.Quality}");
                return match;
            }

            if (!int.TryParse(string.Concat(qualityPref.Where(char.IsDigit)), out int requestedQualityNum))
            {
                var stream = videos[0];
                Console.WriteLine($"Video: Could not parse '{qualityPref}'. Using highest available: {stream.Quality}");
                return stream;
            }

            var closestStream = videos
                .OrderBy(v => Math.Abs(int.Parse(string.Concat(v.Quality.Where(char.IsDigit))) - requestedQualityNum))
                .First();
            
            Console.WriteLine($"Video: Quality '{qualityPref}' not found. Using closest: {closestStream.Quality}");
            return closestStream;
        }

        Console.WriteLine("Video Quality");
        for (int i = 0; i < videos.Count; i++)
        {
            Console.WriteLine($"  {i + 1}) {videos[i].Quality}");
        }
        Console.Write("> Select video [1]: ");
        string? line = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(line)) return videos[0];
        if (int.TryParse(line, out int choice) && choice >= 1 && choice <= videos.Count)
        {
            return videos[choice - 1];
        }
        Console.Error.WriteLine("Invalid selection.");
        return null;
    }

    private static AudioStream? SelectAudio(List<AudioStream> audios, string? langPref)
    {
        if (audios.Count == 1)
        {
            var stream = audios[0];
            Console.WriteLine($"Audio: Only one track available -> {stream.Name}");
            return stream;
        }

        int defaultIndex = audios.FindIndex(a => a.IsDefault);
        if (defaultIndex == -1)
        {
            int englishIndex = audios.FindIndex(a => a.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            defaultIndex = (englishIndex != -1) ? englishIndex : 0;
        }

        if (!string.IsNullOrWhiteSpace(langPref))
        {
            var match = audios.Find(a => a.Language.Equals(langPref, StringComparison.OrdinalIgnoreCase))
                        ?? audios.Find(a => a.Language.StartsWith(langPref, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                Console.WriteLine($"Audio: Matched language '{langPref}' -> {match.Name}");
                return match;
            }
            
            var defaultStream = audios[defaultIndex];
            Console.WriteLine($"Audio: Language '{langPref}' not found. Using default -> {defaultStream.Name}");
            return defaultStream;
        }

        Console.WriteLine("\nAudio Track");
        var languageGroups = audios.GroupBy(a => GetNormalizedLanguageName(a.Language)).ToDictionary(g => g.Key, g => g.Count());

        for (int i = 0; i < audios.Count; i++)
        {
            var a = audios[i];
            string indicator = (i == defaultIndex) ? " (default)" : "";
            string normalizedName = GetNormalizedLanguageName(a.Language);
            
            string displayName = languageGroups[normalizedName] > 1 ? $"{normalizedName} ({a.Name})" : normalizedName;

            Console.WriteLine($"  {i + 1}) {displayName}{indicator}");
        }
        Console.Write($"> Select audio [{defaultIndex + 1}]: ");
        string? line = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(line)) return audios[defaultIndex];
        if (int.TryParse(line, out int choice) && choice >= 1 && choice <= audios.Count)
        {
            return audios[choice - 1];
        }
        Console.Error.WriteLine("Invalid selection.");
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