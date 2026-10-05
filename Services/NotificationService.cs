using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UnitFromHellBot.Models;
using UnitFromHellBot.Repositories;

namespace UnitFromHellBot.Services
{
    public class NotificationService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IReminderRepository _repository;

        private static readonly TimeSpan MoscowUtcOffset =
            TimeSpan.FromHours(3);

        public NotificationService(
            ITelegramBotClient botClient,
            IReminderRepository repository)
        {
            _botClient = botClient;
            _repository = repository;
        }

        public async Task StartNotificationLoopAsync(
            CancellationToken cancellationToken)
        {
            using var timer =
                new PeriodicTimer(
                    TimeSpan.FromSeconds(30));

            try
            {
                while (await timer.WaitForNextTickAsync(
                    cancellationToken))
                {

                    var now =
                        DateTime.UtcNow.Add(MoscowUtcOffset);

                    var events =
                        _repository.GetAllEvents();

                    var toNotify =
                        events
                            .Where(e =>
                                e.TargetTime <= now &&
                                !e.IsNotified)
                            .ToList();

                    foreach (var ev in toNotify)
                    {
                        try
                        {
                            var inlineKeyboard =
                                new InlineKeyboardMarkup(
                                    new[]
                                    {
                                        new[]
                                        {
                                            InlineKeyboardButton
                                                .WithCallbackData(
                                                    "🔄 Перенести",
                                                    $"event_reschedule_{ev.Id}")
                                        }
                                    });

                            await _botClient.SendMessage(
                                chatId: ev.ChatId,
                                text:
                                    $"⏰ **ПОРА! Наступило " +
                                    $"запланированное событие!**\n\n" +
                                    $"📝 Событие: {ev.Description}",
                                parseMode: ParseMode.Markdown,
                                replyMarkup: inlineKeyboard,
                                cancellationToken:
                                    cancellationToken);

                            ev.IsNotified = true;

                            _repository.UpdateEvent(ev);

                            Console.WriteLine(
                                $"[Уведомление] Событие {ev.Id} " +
                                $"отправлено в {now:dd.MM.yyyy HH:mm:ss} МСК.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"Ошибка уведомления чата " +
                                $"{ev.ChatId}: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}