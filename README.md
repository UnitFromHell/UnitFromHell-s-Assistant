# UnitFromHellBot

A Telegram bot written in C# using .NET.

UnitFromHellBot provides a simple reminder system with an interactive calendar, date and time selection, and scheduled Telegram notifications.

## Features

- 📅 Interactive calendar
- ⏰ Reminder scheduling
- 🔔 Automatic Telegram notifications
- 🗓️ Date and time selection
- 🇷🇺 Moscow time (UTC+3) for reminders
- 🖼️ Calendar image rendering
- 💾 SQLite database
- 🔤 Bundled DejaVu Sans fonts for calendar rendering
- ⚡ Runs as a systemd service on Linux

## Requirements

- .NET 9.0 SDK or later
- A Telegram Bot Token
- A Telegram user ID
- SQLite
- Linux is recommended for deployment

## Configuration

The repository does **not** contain the real `appsettings.json` file because it may contain private credentials.

### 1. Create the configuration file

Copy:

```text
appsettings_example.json
```

to:

```text
appsettings.json
```

You can simply copy the file and rename the copy to `appsettings.json`.

### 2. Add your credentials

Open `appsettings.json` and replace the placeholder values:

```json
{
  "TelegramBotToken": "YOUR_BOT_TOKEN_HERE",
  "OwnerTelegramId": "YOUR_TELEGRAM_ID_HERE"
}
```

with your actual Telegram bot token and Telegram user ID.

**Never commit your real `appsettings.json` to GitHub.**

The real `appsettings.json` is excluded by `.gitignore`.

## Running locally

Clone the repository:

```bash
git clone https://github.com/YOUR_USERNAME/UnitFromHellBot.git
cd UnitFromHellBot
```

Create your local configuration:

```text
appsettings_example.json → appsettings.json
```

Restore dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run the bot:

```bash
dotnet run
```

## Publishing

To create a Linux deployment:

```bash
dotnet publish -c Release -r linux-x64 --self-contained false -o publish
```

The `publish/` directory is intentionally excluded from Git.

## Linux Deployment

The bot can be run as a `systemd` service on a Linux server.

Example:

```ini
[Unit]
Description=UnitFromHellBot
After=network.target

[Service]
WorkingDirectory=/opt/UnitFromHellBot/publish
ExecStart=/usr/bin/dotnet /opt/UnitFromHellBot/publish/UnitFromHellBot.dll
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

For production deployments, keep private credentials outside the Git repository.

## Project Structure

```text
UnitFromHellBot/
├── Fonts/
│   ├── DejaVuSans.ttf
│   └── ...
├── Handlers/
│   └── UpdateHandler.cs
├── Models/
│   ├── ReminderEvent.cs
│   └── UserSession.cs
├── Repositories/
│   ├── IReminderRepository.cs
│   └── SqliteReminderRepository.cs
├── Services/
│   ├── CalendarImageRenderer.cs
│   ├── CalendarService.cs
│   ├── ImageRenderService.cs
│   └── NotificationService.cs
├── Program.cs
├── UnitFromHellBot.csproj
├── .gitignore
└── appsettings_example.json
```

## Time Zone

Reminder scheduling uses Moscow time:

```text
UTC+3
```

The server's system time zone does not need to be changed.

## Security

Do not upload or commit:

- `appsettings.json`
- Telegram bot tokens
- Personal Telegram IDs
- SQLite database files
- `publish/`
- Other private configuration or credentials

Use `appsettings_example.json` as the public configuration template.

## License

This project is provided for personal and educational use.
