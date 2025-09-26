namespace MpvYt;

public record Stream(string Url, long Bitrate);

public record VideoStream(string Url, long Bitrate, string Quality) : Stream(Url, Bitrate);

public record AudioStream(string Url, long Bitrate, string Language, string Name, bool IsDefault) : Stream(Url, Bitrate);

public record PlayerData(string Title, string? ThumbnailUrl, List<VideoStream> Videos, List<AudioStream> Audios);

public abstract record StreamSelection;
public record VideoSelection(VideoStream Video, AudioStream Audio) : StreamSelection;
public record AudioSelection(AudioStream Audio) : StreamSelection;