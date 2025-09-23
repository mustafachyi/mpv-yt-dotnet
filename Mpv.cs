using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MpvYt;

public static class Mpv
{
    public static void Launch(PlayerData playerData, VideoStream? videoStream)
    {
        string mpvExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mpv.exe" : "mpv";
        
        var processStartInfo = new ProcessStartInfo(mpvExecutable)
        {
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        };

        processStartInfo.ArgumentList.Add($"--title={playerData.Title}");
        processStartInfo.ArgumentList.Add("--force-media-title= ");

        if (videoStream is not null)
        {
            processStartInfo.ArgumentList.Add(videoStream.Url);
            processStartInfo.ArgumentList.Add($"--audio-file={playerData.Audio.Url}");
            Console.WriteLine($"Playing: {playerData.Title} [video]");
        }
        else
        {
            processStartInfo.ArgumentList.Add(playerData.Audio.Url);
            processStartInfo.ArgumentList.Add("--force-window");
            Console.WriteLine($"Playing: {playerData.Title} [audio only]");
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