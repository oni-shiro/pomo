using System.Diagnostics;

namespace pomo;

public sealed class NotificationService
{
    private static readonly string[] PreferredIcons =
    [
        "/usr/share/icons/hicolor/scalable/apps/gnome-clocks.svg",
        "/usr/share/icons/hicolor/scalable/apps/clock.svg",
        "/usr/share/icons/hicolor/256x256/apps/gnome-clocks.png",
        "/usr/share/icons/hicolor/256x256/apps/clock.png",
        "/usr/share/icons/hicolor/256x256/apps/org.gnome.clocks.png"
    ];

    public async Task SendAsync(
        string title,
        string body,
        string appName = "Pomo",
        string urgency = "normal",
        int expireTime = 5000,
        string? icon = null,
        bool transient = false,
        CancellationToken cancellationToken = default)
    {
        var resolvedIcon = ResolveIcon(icon);

        var startInfo = new ProcessStartInfo
        {
            FileName = "notify-send",
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("--app-name");
        startInfo.ArgumentList.Add(appName);

        startInfo.ArgumentList.Add("--urgency");
        startInfo.ArgumentList.Add(urgency);

        startInfo.ArgumentList.Add("--expire-time");
        startInfo.ArgumentList.Add(expireTime.ToString());

        if (!string.IsNullOrWhiteSpace(resolvedIcon))
        {
            startInfo.ArgumentList.Add("--icon");
            startInfo.ArgumentList.Add(resolvedIcon);
        }

        if (transient)
        {
            startInfo.ArgumentList.Add("--transient");
        }

        startInfo.ArgumentList.Add(title);
        startInfo.ArgumentList.Add(body);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Failed to start notify-send.");

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"notify-send exited with code {process.ExitCode}");
        }
    }

    private static string ResolveIcon(string? requestedIcon)
    {
        if (!string.IsNullOrWhiteSpace(requestedIcon))
        {
            var bundled = ResolveBundledIcon(requestedIcon);
            if (bundled is not null)
            {
                return bundled;
            }

            if (File.Exists(requestedIcon))
            {
                return requestedIcon;
            }

            return requestedIcon;
        }

        var defaultBundled = ResolveBundledIcon("pomo-focus");
        if (defaultBundled is not null)
        {
            return defaultBundled;
        }

        foreach (var iconPath in PreferredIcons)
        {
            if (File.Exists(iconPath))
            {
                return iconPath;
            }
        }

        return "timer";
    }

    private static string? ResolveBundledIcon(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        var assetDirectory = Path.Combine(AppContext.BaseDirectory, "assets");
        var candidates = new[]
        {
            Path.Combine(assetDirectory, $"{iconName}.svg"),
            Path.Combine(assetDirectory, $"{iconName}.png"),
            Path.Combine(assetDirectory, $"{iconName}.xpm")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
