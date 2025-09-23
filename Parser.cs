namespace MpvYt;

public static class Parser
{
    private static readonly IReadOnlyDictionary<int, string> ItagQualityMap = new Dictionary<int, string>
    {
        { 160, "144p" }, { 278, "144p" }, { 330, "144p" }, { 394, "144p" }, { 694, "144p" },
        { 133, "240p" }, { 242, "240p" }, { 331, "240p" }, { 395, "240p" }, { 695, "240p" },
        { 134, "360p" }, { 243, "360p" }, { 332, "360p" }, { 396, "360p" }, { 696, "360p" },
        { 135, "480p" }, { 244, "480p" }, { 333, "480p" }, { 397, "480p" }, { 697, "480p" },
        { 136, "720p" }, { 247, "720p" }, { 298, "720p" }, { 302, "720p" }, { 334, "720p" }, { 398, "720p" }, { 698, "720p" },
        { 137, "1080p" }, { 299, "1080p" }, { 248, "1080p" }, { 303, "1080p" }, { 335, "1080p" }, { 399, "1080p" }, { 699, "1080p" },
        { 264, "1440p" }, { 271, "1440p" }, { 304, "1440p" }, { 308, "1440p" }, { 336, "1440p" }, { 400, "1440p" }, { 700, "1440p" },
        { 266, "2160p" }, { 305, "2160p" }, { 313, "2160p" }, { 315, "2160p" }, { 337, "2160p" }, { 401, "2160p" }, { 701, "2160p" },
        { 138, "4320p" }, { 272, "4320p" }, { 402, "4320p" }, { 571, "4320p" },
    };

    public static (List<VideoStream> Videos, Stream? Audio) ParseStreams(object streamingDataObject)
    {
        var streamingData = (System.Text.Json.JsonElement)streamingDataObject;
        if (!streamingData.TryGetProperty("adaptiveFormats", out var formatsElement) || formatsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return ([], null);
        }

        var videoStreams = new Dictionary<string, VideoStream>();
        Stream? bestAudio = null;

        foreach (var format in formatsElement.EnumerateArray())
        {
            if (!format.TryGetProperty("url", out var urlElement) || urlElement.GetString() is not { } url) continue;
            if (!format.TryGetProperty("bitrate", out var bitrateElement) || !bitrateElement.TryGetInt64(out var bitrate)) continue;
            
            var mimeType = format.TryGetProperty("mimeType", out var mimeElement) ? mimeElement.GetString() : "";
            bool isVideo = mimeType?.Contains("video/") == true;
            bool isAudio = mimeType?.Contains("audio/") == true;

            if (isVideo)
            {
                if (!format.TryGetProperty("itag", out var itagElement) || !itagElement.TryGetInt32(out var itag)) continue;
                if (!ItagQualityMap.TryGetValue(itag, out var quality)) continue;

                if (!videoStreams.TryGetValue(quality, out var existing) || bitrate > existing.Bitrate)
                {
                    videoStreams[quality] = new VideoStream(url, bitrate, quality);
                }
            }
            else if (isAudio)
            {
                if (bestAudio is null || bitrate > bestAudio.Bitrate)
                {
                    bestAudio = new Stream(url, bitrate);
                }
            }
        }

        var sortedVideos = videoStreams.Values.OrderByDescending(v => v.Bitrate).ToList();
        return (sortedVideos, bestAudio);
    }
}