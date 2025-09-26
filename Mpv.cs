using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MpvYt;

public static class Mpv
{
    public static bool IsAvailable()
    {
        string mpvExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mpv.exe" : "mpv";
        string? pathVar = Environment.GetEnvironmentVariable("PATH");

        if (pathVar is null)
        {
            return false;
        }

        foreach (string path in pathVar.Split(Path.PathSeparator))
        {
            string fullPath = Path.Combine(path, mpvExecutable);
            if (File.Exists(fullPath))
            {
                return true;
            }
        }
        
        return false;
    }

    public static void Launch(string title, string? thumbnailUrl, VideoStream? video, AudioStream audio)
    {
        Console.Clear();
        
        string mpvExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mpv.exe" : "mpv";
        
        var processStartInfo = new ProcessStartInfo(mpvExecutable)
        {
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        };

        processStartInfo.ArgumentList.Add($"--title={title}");
        processStartInfo.ArgumentList.Add("--force-media-title= ");
        processStartInfo.ArgumentList.Add("--keep-open=yes");

        if (video is not null)
        {
            processStartInfo.ArgumentList.Add(video.Url);
            processStartInfo.ArgumentList.Add($"--audio-file={audio.Url}");
            Console.WriteLine($"\nPlaying: {title} [{video.Quality} / {audio.Name}]");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                processStartInfo.ArgumentList.Add(thumbnailUrl);
                processStartInfo.ArgumentList.Add($"--audio-file={audio.Url}");
                processStartInfo.ArgumentList.Add("--image-display-duration=inf");
                processStartInfo.ArgumentList.Add("--force-window=immediate");
                processStartInfo.ArgumentList.Add("--video-unscaled=yes");
                processStartInfo.ArgumentList.Add("--terminal=no");
            }
            else
            {
                processStartInfo.ArgumentList.Add(audio.Url);
                processStartInfo.ArgumentList.Add("--force-window");
            }
            
            Console.WriteLine($"\nPlaying: {title} [Audio only / {audio.Name}]");
        }

        try
        {
            Process.Start(processStartInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine($"Error: '{mpvExecutable}' not found in your system's PATH.");
            Environment.Exit(1);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error launching mpv: {e.Message}");
            Environment.Exit(1);
        }
    }
}