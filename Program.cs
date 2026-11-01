
using System.Diagnostics;
using System.IO.Pipes;
using pomo;

const string PipeName = "pomo";

if (args.Length == 0)
{
    Console.WriteLine("Usage: pomo <reg|maxx|close|status> [--service]");
    return;
}

var isServiceMode = args.Contains("--service", StringComparer.OrdinalIgnoreCase);
var foregroundMode = args.Contains("--foreground", StringComparer.OrdinalIgnoreCase);
string arg = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal))?.ToLowerInvariant() ?? string.Empty;

if (string.IsNullOrWhiteSpace(arg))
{
    Console.WriteLine("Usage: pomo <reg|maxx|close|status> [--service]");
    return;
}

if (arg == "status")
{
    await CheckStatusAsync();
    return;
}

if (arg == "close")
{
    try
    {
        // Close Client
        using var client = new NamedPipeClientStream(
            ".", // localhost
            PipeName,
            PipeDirection.Out);

        await client.ConnectAsync(2000);

        using var writer = new StreamWriter(client)
        {
            AutoFlush = true
        };

        await writer.WriteLineAsync("close");

        Console.WriteLine("Stopping Pomodoro timer...");
    }
    catch (TimeoutException)
    {
        Console.WriteLine("No running Pomodoro timer found.");
    }
    catch (IOException)
    {
        Console.WriteLine("No running Pomodoro timer found.");
    }

    return;
}

if (!isServiceMode && !foregroundMode && (arg == "reg" || arg == "maxx"))
{
    StartDetached(arg);
    return;
}

TimeSpan workDuration;
TimeSpan breakDuration;

switch (arg)
{
    case "reg":
        workDuration = TimeSpan.FromMinutes(25);
        breakDuration = TimeSpan.FromMinutes(5);
        break;

    case "maxx":
        workDuration = TimeSpan.FromMinutes(45);
        breakDuration = TimeSpan.FromMinutes(15);
        break;

    default:
        Console.WriteLine($"Unknown command: {arg}");
        Console.WriteLine("Available commands: reg, maxx, close, status");
        return;
}

var notificationService = new NotificationService();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"Pomo {arg} started.");
Console.WriteLine("Press Ctrl+C or run 'pomo close' to stop.");

Task timerTask = RunPomodoroAsync(
    notificationService,
    workDuration,
    breakDuration,
    cts.Token);

Task closeTask = ListenForCloseAsync(
    PipeName,
    cts);

try
{
    await Task.WhenAny(timerTask, closeTask);
}
finally
{
    cts.Cancel();

    try
    {
        await Task.WhenAll(timerTask, closeTask);
    }
    catch (OperationCanceledException)
    {
    }

    Console.WriteLine("Pomo stopped.");
}

static async Task CheckStatusAsync()
{
    try
    {
        // Status Client
        using var client = new NamedPipeClientStream(
            ".",
            PipeName,
            PipeDirection.Out,
            PipeOptions.Asynchronous);

        await client.ConnectAsync(2000);

        Console.WriteLine("Pomo is running.");
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Pomo is not running.");
    }
    catch (IOException)
    {
        Console.WriteLine("Pomo is not running.");
    }
}

static void StartDetached(string command)
{
    var executablePath = Environment.ProcessPath;

    if (string.IsNullOrWhiteSpace(executablePath))
    {
        Console.WriteLine("Could not determine the installed executable path.");
        return;
    }

    var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
    var nopty = OperatingSystem.IsLinux() ? "nohup " : string.Empty;

    var startCommand = $"{nopty}\"{executablePath}\" --service {command} >/dev/null 2>&1 &";

    try
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = $"-lc \"{startCommand}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        Console.WriteLine($"Pomo {command} started in detached mode.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to start detached mode: {ex.Message}");
    }
}

static async Task RunPomodoroAsync(
    NotificationService notificationService,
    TimeSpan workDuration,
    TimeSpan breakDuration,
    CancellationToken cancellationToken)
{
    int session = 1;

    while (!cancellationToken.IsCancellationRequested)
    {
        await notificationService.SendAsync(
            "🍅 Focus mode",
            $"Session {session} is live — deep work for {workDuration.TotalMinutes} minutes.",
            appName: "Pomo",
            urgency: "normal",
            expireTime: 6000,
            icon: "pomo-focus");

        Console.WriteLine($"Session {session}: Work started.");

        await Task.Delay(workDuration, cancellationToken);

        await notificationService.SendAsync(
            "☕ Break time",
            "Excellent work. Stretch, hydrate, and recharge before the next sprint.",
            appName: "Pomo",
            urgency: "normal",
            expireTime: 7000,
            icon: "pomo-break");

        Console.WriteLine("Break started.");

        await Task.Delay(breakDuration, cancellationToken);

        await notificationService.SendAsync(
            "✨ Back to focus",
            "Break complete — time to lock in and crush the next session.",
            appName: "Pomo",
            urgency: "critical",
            expireTime: 8000,
            icon: "pomo-ready");

        Console.WriteLine("Break finished.");

        session++;
    }
}

// Actual Listner from the process to recive commands from Clients
static async Task ListenForCloseAsync(
    string pipeName,
    CancellationTokenSource cts)
{
    while (!cts.IsCancellationRequested)
    {
        using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.In,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        await server.WaitForConnectionAsync(cts.Token);

        using var reader = new StreamReader(server);

        string? command = await reader.ReadLineAsync(cts.Token);

        if (command == "close")
        {
            cts.Cancel();
            return;
        }
    }
}
