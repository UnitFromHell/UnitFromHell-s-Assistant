using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using UnitFromHellBot.Handlers;
using UnitFromHellBot.Repositories;
using UnitFromHellBot.Services;

class Program
{
    static async Task Main(string[] args)
    {
        string baseDir =
            AppDomain.CurrentDomain.BaseDirectory;

        var config =
            new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();


        string? botToken =
            config["TelegramBotToken"];

        if (string.IsNullOrWhiteSpace(botToken))
        {
            Console.ForegroundColor =
                ConsoleColor.Red;

            Console.WriteLine(
                "Ошибка: TelegramBotToken не найден!");

            Console.ResetColor();

            return;
        }


        string? ownerIdString =
            config["OwnerTelegramId"];

        if (!long.TryParse(
                ownerIdString,
                out long ownerTelegramId))
        {
            Console.ForegroundColor =
                ConsoleColor.Red;

            Console.WriteLine(
                "Ошибка: OwnerTelegramId не найден " +
                "или имеет неправильный формат!");

            Console.ResetColor();

            return;
        }



        var botClient =
            new TelegramBotClient(botToken);

        using var cts =
            new CancellationTokenSource();



        IReminderRepository repository =
            new SqliteReminderRepository();



        var updateHandler =
            new UpdateHandler(
                repository,
                ownerTelegramId);



        var notificationService =
            new NotificationService(
                botClient,
                repository);

        _ = notificationService
            .StartNotificationLoopAsync(
                cts.Token);


        var receiverOptions =
            new ReceiverOptions
            {
                AllowedUpdates =
                    Array.Empty<UpdateType>()
            };

        botClient.StartReceiving(
            updateHandler.HandleUpdateAsync,
            updateHandler.HandlePollingErrorAsync,
            receiverOptions,
            cts.Token);


        var me =
            await botClient.GetMe(
                cancellationToken:
                    cts.Token);

        Console.ForegroundColor =
            ConsoleColor.Green;

        Console.WriteLine(
            $"Бот @{me.Username} успешно запущен.");

        Console.WriteLine(
            $"Владелец Telegram ID: {ownerTelegramId}");

        Console.ResetColor();


        await Task.Delay(Timeout.Infinite);

        cts.Cancel();
    }
}