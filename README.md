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
make sure `dotnet` is installed in system

## Install from source

A published .NET app is not just the `pomo` launcher. It also needs its companion files such as `pomo.dll`, `pomo.runtimeconfig.json`, and the bundled assets folder to live beside it.

For a cleaner install, keep the app in its own folder and expose a small launcher in `/usr/local/bin`:

```bash
cd /home/sinch69/dev/pomo
dotnet publish -c Release -o ./build/install
sudo mkdir -p /usr/local/lib/pomo
sudo cp -a ./build/install/. /usr/local/lib/pomo/
sudo ln -sf /usr/local/lib/pomo/pomo /usr/local/bin/pomo
```

This keeps the published app and its assets together in `/usr/local/lib/pomo`, while still making the `pomo` command available from your shell. Then you can run:

```bash
pomo reg
pomo maxx
pomo status
pomo close
```

`pomo reg` and `pomo maxx` start the timer in detached mode immediately and return to your shell. `pomo status` tells you whether the timer is currently running, and `pomo close` stops it cleanly. This keeps the process running in the background without using systemd.

## Developer note

The timer runs as a background process when launched through the CLI, but the actual long-running loop is still the same app code under `Program.cs`. It listens on a named pipe so `pomo close` can stop the active session cleanly.
