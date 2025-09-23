namespace MpvYt;

public record Stream(string Url, long Bitrate);

public record VideoStream(string Url, long Bitrate, string Quality) : Stream(Url, Bitrate);

public record PlayerData(string Title, List<VideoStream> Videos, Stream Audio);

public abstract record StreamSelection;
public record VideoSelection(VideoStream Stream) : StreamSelection;
public record AudioSelection : StreamSelection;