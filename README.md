# pomo
A simple CLI pomodoro timer with polished desktop notifications and a detached background mode for daily use.

## Prerequisites

This app uses Linux desktop notifications via `notify-send`, so your machine needs the notification system installed.

On most Debian/Ubuntu-based systems:

```bash
sudo apt install libnotify-bin
```

On Fedora/RHEL:

```bash
sudo dnf install libnotify
```

On Arch:

```bash
sudo pacman -S libnotify
```

If `notify-send` is not present, the app will not be able to show desktop notifications.

## Install from source

```bash
cd /home/sinch69/dev/pomo
sudo ./scripts/install.sh
```

Or manually:

```bash
dotnet publish -c Release
sudo install -m 755 ./bin/Release/net10.0/publish/pomo /usr/local/bin/pomo
```

Once installed, you can run:

```bash
pomo reg
pomo maxx
pomo status
pomo close
```

`pomo reg` and `pomo maxx` start the timer in detached mode immediately and return to your shell. `pomo status` tells you whether the timer is currently running, and `pomo close` stops it cleanly. This keeps the process running in the background without using systemd.

## Developer note

The timer runs as a background process when launched through the CLI, but the actual long-running loop is still the same app code under `Program.cs`. It listens on a named pipe so `pomo close` can stop the active session cleanly.
