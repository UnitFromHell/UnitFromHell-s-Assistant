using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using UnitFromHellBot.Models;
using UnitFromHellBot.Repositories;
using UnitFromHellBot.Services;

namespace UnitFromHellBot.Handlers
{
    public class UpdateHandler
    {
        private readonly IReminderRepository _repository;

        private readonly CalendarImageRenderer _calendarRenderer;

        private readonly long _ownerTelegramId;

        private readonly ConcurrentDictionary<long, UserSession> _sessions =
            new ConcurrentDictionary<long, UserSession>();

        public UpdateHandler(
            IReminderRepository repository,
            long ownerTelegramId)
        {
            _repository = repository;

            _ownerTelegramId =
                ownerTelegramId;

            _calendarRenderer =
                new CalendarImageRenderer();
        }


        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            try
            {

                long? userId = null;

                if (update.Message != null)
                {
                    userId =
                        update.Message.From?.Id;
                }
                else if (update.CallbackQuery != null)
                {
                    userId =
                        update.CallbackQuery.From.Id;
                }


                if (!userId.HasValue ||
                    userId.Value != _ownerTelegramId)
                {
                    Console.WriteLine(
                        $"Заблокирован пользователь: " +
                        $"{userId?.ToString() ?? "unknown"}");

                    return;
                }


                if (update.Message is
                    { Text: { } text } message)
                {
                    await HandleTextMessageAsync(
                        botClient,
                        message,
                        text,
                        cancellationToken);

                    return;
                }


                if (update.CallbackQuery is
                    { Data: { } data } callbackQuery)
                {
                    await HandleCallbackQueryAsync(
                        botClient,
                        callbackQuery,
                        data,
                        cancellationToken);

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка обработки update:");

                Console.WriteLine(ex);
            }
        }


        private async Task HandleTextMessageAsync(
            ITelegramBotClient botClient,
            Message message,
            string text,
            CancellationToken cancellationToken)
        {
            long chatId =
                message.Chat.Id;

            UserSession session =
                GetSession(chatId);


            if (text.Trim() == "/start")
            {
                ResetSession(session);

                await SendMainMenuAsync(
                    botClient,
                    chatId,
                    cancellationToken);

                return;
            }


            if (session.State ==
                SessionState.AwaitingDescription)
            {
                await SaveEventFromDescriptionAsync(
                    botClient,
                    chatId,
                    session,
                    text,
                    cancellationToken);

                return;
            }


            await botClient.SendMessage(
                chatId: chatId,
                text:
                    "Выберите действие в меню 👇",
                replyMarkup:
                    BuildMainMenuKeyboard(),
                cancellationToken:
                    cancellationToken);
        }


        private async Task HandleCallbackQueryAsync(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            string data,
            CancellationToken cancellationToken)
        {
            if (callbackQuery.Message == null)
            {
                return;
            }

            long chatId =
                callbackQuery.Message.Chat.Id;

            int messageId =
                callbackQuery.Message.MessageId;

            UserSession session =
                GetSession(chatId);

            try
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    cancellationToken:
                        cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Не удалось подтвердить callback-запрос: {ex.Message}");
            }


            if (data == "calendar_ignore")
            {
                return;
            }

            if (data == "time_ignore")
            {
                return;
            }
            if (data == "main_menu")
            {
                ResetSession(session);

                await DeleteMessageSafeAsync(
                    botClient,
                    chatId,
                    messageId,
                    cancellationToken);

                await SendMainMenuAsync(
                    botClient,
                    chatId,
                    cancellationToken);

                return;
            }


            if (data == "action_calendar")
            {
                ResetSession(session);

                await DeleteMessageSafeAsync(
                    botClient,
                    chatId,
                    messageId,
                    cancellationToken);

                DateTime now =
                    DateTime.UtcNow.AddHours(3);

                await SendCalendarAsync(
                    botClient,
                    chatId,
                    now.Year,
                    now.Month,
                    cancellationToken);

                return;
            }


            if (data == "calendar_today")
            {
                ResetSession(session);

                await DeleteMessageSafeAsync(
                    botClient,
                    chatId,
                    messageId,
                    cancellationToken);

                DateTime today =
                    DateTime.UtcNow.AddHours(3).Date;

                await SendCalendarAsync(
                    botClient,
                    chatId,
                    today.Year,
                    today.Month,
                    cancellationToken);

                return;
            }


            if (data.StartsWith("calendar_"))
            {
                string[] parts =
                    data.Split('_');

                if (parts.Length == 3 &&
                    int.TryParse(
                        parts[1],
                        out int year) &&
                    int.TryParse(
                        parts[2],
                        out int month))
                {
                    ResetSession(session);

                    await DeleteMessageSafeAsync(
                        botClient,
                        chatId,
                        messageId,
                        cancellationToken);

                    await SendCalendarAsync(
                        botClient,
                        chatId,
                        year,
                        month,
                        cancellationToken);

                    return;
                }
            }


            if (data.StartsWith("calendar_day_"))
            {
                string[] parts =
                    data.Split('_');

                if (parts.Length == 5 &&
                    int.TryParse(
                        parts[2],
                        out int year) &&
                    int.TryParse(
                        parts[3],
                        out int month) &&
                    int.TryParse(
                        parts[4],
                        out int day))
                {

                    if (session.State ==
                            SessionState.ChoosingDate &&
                        session.EditingEventId.HasValue)
                    {
                        ReminderEvent? eventToMove =
                            _repository.GetEventById(
                                session.EditingEventId.Value);

                        if (eventToMove == null ||
                            eventToMove.ChatId != chatId)
                        {
                            ResetSession(session);

                            await botClient.SendMessage(
                                chatId,
                                "❌ Событие не найдено.",
                                cancellationToken:
                                    cancellationToken);

                            return;
                        }

                        session.TargetYear =
                            year;

                        session.TargetMonth =
                            month;

                        session.TargetDay =
                            day;

                        session.State =
                            SessionState.ChoosingHour;

                        await DeleteMessageSafeAsync(
                            botClient,
                            chatId,
                            messageId,
                            cancellationToken);

                        await botClient.SendMessage(
                            chatId: chatId,
                            text:
                                "🔄 **Перенос события**\n\n" +
                                $"📝 {EscapeMarkdown(eventToMove.Description)}\n\n" +
                                $"📅 Новая дата: " +
                                $"**{day:00}.{month:00}.{year}**\n\n" +
                                "⏰ Выберите новый час:",
                            parseMode:
                                ParseMode.Markdown,
                            replyMarkup:
    CalendarService.BuildHoursKeyboard(
        session.TargetYear,
        session.TargetMonth,
        session.TargetDay),
                            cancellationToken:
                                cancellationToken);

                        return;
                    }


                    ResetSession(session);

                    await DeleteMessageSafeAsync(
                        botClient,
                        chatId,
                        messageId,
                        cancellationToken);

                    await ShowDayAsync(
                        botClient,
                        chatId,
                        year,
                        month,
                        day,
                        cancellationToken);

                    return;
                }
            }


            if (data.StartsWith("event_add_"))
            {
                string[] parts =
                    data.Split('_');

                if (parts.Length == 5 &&
                    int.TryParse(
                        parts[2],
                        out int year) &&
                    int.TryParse(
                        parts[3],
                        out int month) &&
                    int.TryParse(
                        parts[4],
                        out int day))
                {
                    session.State =
                        SessionState.ChoosingHour;

                    session.EditingEventId =
                        null;

                    session.TargetYear =
                        year;

                    session.TargetMonth =
                        month;

                    session.TargetDay =
                        day;

                    await botClient.EditMessageText(
                        chatId: chatId,
                        messageId: messageId,
                        text:
                            $"📅 **{day:00}.{month:00}.{year}**\n\n" +
                            "⏰ Выберите час:",
                        parseMode:
                            ParseMode.Markdown,
                        replyMarkup:
    CalendarService.BuildHoursKeyboard(
        session.TargetYear,
        session.TargetMonth,
        session.TargetDay),
                        cancellationToken:
                            cancellationToken);

                    return;
                }
            }


            if (data.StartsWith("event_reschedule_"))
            {
                string idText =
                    data.Substring(
                        "event_reschedule_".Length);

                if (Guid.TryParse(
                    idText,
                    out Guid eventId))
                {
                    ReminderEvent? ev =
                        _repository.GetEventById(
                            eventId);

                    if (ev == null ||
                        ev.ChatId != chatId)
                    {
                        await botClient.SendMessage(
                            chatId,
                            "❌ Событие не найдено.",
                            cancellationToken:
                                cancellationToken);

                        return;
                    }


                    session.EditingEventId =
                        eventId;

                    session.State =
                        SessionState.ChoosingDate;

                    DateTime currentDate =
                        ev.TargetTime;

                    session.TargetYear =
                        currentDate.Year;

                    session.TargetMonth =
                        currentDate.Month;

                    session.TargetDay =
                        currentDate.Day;

                    await DeleteMessageSafeAsync(
                        botClient,
                        chatId,
                        messageId,
                        cancellationToken);

                    await SendCalendarAsync(
                        botClient,
                        chatId,
                        currentDate.Year,
                        currentDate.Month,
                        cancellationToken,
                        "🔄 Выберите новую дату:");

                    return;
                }
            }

            if (data.StartsWith("event_delete_"))
            {
                string idText =
                    data.Substring(
                        "event_delete_".Length);

                if (Guid.TryParse(
                    idText,
                    out Guid eventId))
                {
                    ReminderEvent? ev =
                        _repository.GetEventById(
                            eventId);

                    if (ev == null ||
                        ev.ChatId != chatId)
                    {
                        await botClient.SendMessage(
                            chatId,
                            "❌ Событие не найдено.",
                            cancellationToken:
                                cancellationToken);

                        return;
                    }

                    _repository.RemoveEvent(
                        eventId);

                    await botClient.SendMessage(
                        chatId,
                        "🗑 **Событие удалено.**",
                        parseMode:
                            ParseMode.Markdown,
                        cancellationToken:
                            cancellationToken);

                    DateTime date =
                        ev.TargetTime;

                    await ShowDayAsync(
                        botClient,
                        chatId,
                        date.Year,
                        date.Month,
                        date.Day,
                        cancellationToken);

                    return;
                }
            }


            if (data.StartsWith("time_hour_"))
            {
                string value =
                    data.Substring(
                        "time_hour_".Length);

                if (int.TryParse(
                    value,
                    out int hour))
                {
                    session.TargetHour =
                        hour;

                    session.State =
                        SessionState.ChoosingMinute;

                    await botClient.EditMessageText(
                        chatId: chatId,
                        messageId: messageId,
                        text:
                            $"📅 " +
                            $"{session.TargetDay:00}." +
                            $"{session.TargetMonth:00}." +
                            $"{session.TargetYear}\n\n" +
                            $"⏰ Время: **{hour:00}:__**\n\n" +
                            "Выберите минуты:",
                        parseMode:
                            ParseMode.Markdown,
                        replyMarkup:
    CalendarService.BuildMinutesKeyboard(
        session.TargetYear,
        session.TargetMonth,
        session.TargetDay,
        session.TargetHour),
                        cancellationToken:
                            cancellationToken);

                    return;
                }
            }


            if (data.StartsWith("time_minute_"))
            {
                string value =
                    data.Substring(
                        "time_minute_".Length);

                if (int.TryParse(
                    value,
                    out int minute))
                {
                    session.TargetMinute =
                        minute;

                    DateTime targetTime =
                        new DateTime(
                            session.TargetYear,
                            session.TargetMonth,
                            session.TargetDay,
                            session.TargetHour,
                            session.TargetMinute,
                            0);


                    if (session.EditingEventId.HasValue)
                    {
                        Guid eventId =
                            session.EditingEventId.Value;

                        ReminderEvent? ev =
                            _repository.GetEventById(
                                eventId);

                        if (ev == null ||
                            ev.ChatId != chatId)
                        {
                            ResetSession(session);

                            await botClient.EditMessageText(
                                chatId,
                                messageId,
                                "❌ Событие не найдено.",
                                cancellationToken:
                                    cancellationToken);

                            return;
                        }


                        ev.TargetTime =
                            targetTime;

                        ev.IsNotified =
                            false;

                        _repository.UpdateEvent(
                            ev);

                        string description =
                            ev.Description;

                        ResetSession(session);

                        await botClient.EditMessageText(
                            chatId: chatId,
                            messageId: messageId,
                            text:
                                "🔄 **Событие перенесено!**\n\n" +
                                $"📅 {targetTime:dd.MM.yyyy}\n" +
                                $"⏰ {targetTime:HH:mm}\n" +
                                $"📝 {EscapeMarkdown(description)}",
                            parseMode:
                                ParseMode.Markdown,
                            replyMarkup:
                                CalendarService.BuildCreatedEventKeyboard(
                                    ev.Id,
                                    targetTime.Year,
                                    targetTime.Month,
                                    targetTime.Day),
                            cancellationToken:
                                cancellationToken);

                        return;
                    }


                    session.State =
                        SessionState.AwaitingDescription;

                    await botClient.EditMessageText(
                        chatId: chatId,
                        messageId: messageId,
                        text:
                            $"📅 **{targetTime:dd.MM.yyyy}**\n" +
                            $"⏰ **{targetTime:HH:mm}**\n\n" +
                            "Теперь напишите, что запланировано:",
                        parseMode:
                            ParseMode.Markdown,
                        cancellationToken:
                            cancellationToken);

                    return;
                }
            }
        }


        private async Task SaveEventFromDescriptionAsync(
            ITelegramBotClient botClient,
            long chatId,
            UserSession session,
            string description,
            CancellationToken cancellationToken)
        {
            description =
                description.Trim();

            if (string.IsNullOrWhiteSpace(
                description))
            {
                await botClient.SendMessage(
                    chatId,
                    "❌ Описание не может быть пустым.\n\n" +
                    "Напишите, что запланировано.",
                    cancellationToken:
                        cancellationToken);

                return;
            }

            DateTime targetTime =
                new DateTime(
                    session.TargetYear,
                    session.TargetMonth,
                    session.TargetDay,
                    session.TargetHour,
                    session.TargetMinute,
                    0);
            DateTime moscowNow =
    DateTime.UtcNow.AddHours(3);
            if (targetTime <= moscowNow)
            {
                ResetSession(session);

                await botClient.SendMessage(
                    chatId,
                    "❌ Нельзя создать событие в прошлом.",
                    replyMarkup:
                        BuildMainMenuKeyboard(),
                    cancellationToken:
                        cancellationToken);

                return;
            }

            var reminder =
                new ReminderEvent
                {
                    Id =
                        Guid.NewGuid(),

                    ChatId =
                        chatId,

                    TargetTime =
                        targetTime,

                    Description =
                        description,

                    IsNotified =
                        false
                };

            _repository.AddEvent(
                reminder);

            ResetSession(session);

            await botClient.SendMessage(
                chatId: chatId,
                text:
                    "✅ **Событие создано!**\n\n" +
                    $"📅 {targetTime:dd.MM.yyyy}\n" +
                    $"⏰ {targetTime:HH:mm}\n" +
                    $"📝 {EscapeMarkdown(description)}",
                parseMode:
                    ParseMode.Markdown,
                replyMarkup:
                    CalendarService.BuildCreatedEventKeyboard(
                        reminder.Id,
                        targetTime.Year,
                        targetTime.Month,
                        targetTime.Day),
                cancellationToken:
                    cancellationToken);
        }


        private async Task SendCalendarAsync(
            ITelegramBotClient botClient,
            long chatId,
            int year,
            int month,
            CancellationToken cancellationToken,
            string caption = "📅 Выберите день:")
        {
            List<ReminderEvent> events =
                _repository
                    .GetAllEvents()
                    .Where(e =>
                        e.ChatId == chatId)
                    .ToList();

            using var stream =
                _calendarRenderer.Render(
                    year,
                    month,
                    events);

            await botClient.SendPhoto(
                chatId: chatId,
                photo:
                    Telegram.Bot.Types.InputFile
                        .FromStream(
                            stream,
                            "calendar.png"),
                caption:
                    $"{GetRussianMonthName(month)} {year}\n\n" +
                    caption,
                replyMarkup:
                    CalendarService.BuildCalendarKeyboard(
                        year,
                        month),
                cancellationToken:
                    cancellationToken);
        }


        private async Task ShowDayAsync(
            ITelegramBotClient botClient,
            long chatId,
            int year,
            int month,
            int day,
            CancellationToken cancellationToken)
        {
            DateTime date =
                new DateTime(
                    year,
                    month,
                    day);

            List<ReminderEvent> events =
                _repository
                    .GetAllEvents()
                    .Where(e =>
                        e.ChatId == chatId &&
                        e.TargetTime.Date == date.Date)
                    .OrderBy(e =>
                        e.TargetTime)
                    .ToList();

            string text =
                $"📅 **{date:dd.MM.yyyy}**\n\n";

            if (events.Count == 0)
            {
                text +=
                    "На этот день событий нет.\n\n" +
                    "Можно добавить новое событие.";

                await botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    parseMode:
                        ParseMode.Markdown,
                    replyMarkup:
                        CalendarService.BuildDayKeyboard(
                            year,
                            month,
                            day),
                    cancellationToken:
                        cancellationToken);

                return;
            }

            foreach (ReminderEvent ev in events)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text:
                        $"⏰ **{ev.TargetTime:HH:mm}**\n" +
                        $"📝 {EscapeMarkdown(ev.Description)}",
                    parseMode:
                        ParseMode.Markdown,
                    replyMarkup:
                        CalendarService.BuildEventActions(
                            ev.Id,
                            year,
                            month,
                            day),
                    cancellationToken:
                        cancellationToken);
            }

            await botClient.SendMessage(
                chatId: chatId,
                text:
                    $"📅 **{date:dd.MM.yyyy}**\n\n" +
                    "Что хотите сделать?",
                parseMode:
                    ParseMode.Markdown,
                replyMarkup:
                    CalendarService.BuildDayKeyboard(
                        year,
                        month,
                        day),
                cancellationToken:
                    cancellationToken);
        }


        private async Task SendMainMenuAsync(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text:
                    "🏠 **Главное меню**\n\n" +
                    "Выберите раздел:",
                parseMode:
                    ParseMode.Markdown,
                replyMarkup:
                    BuildMainMenuKeyboard(),
                cancellationToken:
                    cancellationToken);
        }

        private InlineKeyboardMarkup BuildMainMenuKeyboard()
        {
            return new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "📅 Мой календарь",
                            "action_calendar")
                    }
                });
        }

        private UserSession GetSession(
            long chatId)
        {
            return _sessions.GetOrAdd(
                chatId,
                _ => new UserSession());
        }

        private void ResetSession(
            UserSession session)
        {
            session.State =
                SessionState.Idle;

            session.EditingEventId =
                null;

            session.TargetYear =
                0;

            session.TargetMonth =
                0;

            session.TargetDay =
                0;

            session.TargetHour =
                0;

            session.TargetMinute =
                0;

            session.Description =
                string.Empty;
        }


        private async Task DeleteMessageSafeAsync(
            ITelegramBotClient botClient,
            long chatId,
            int messageId,
            CancellationToken cancellationToken)
        {
            try
            {
                await botClient.DeleteMessage(
                    chatId,
                    messageId,
                    cancellationToken);
            }
            catch
            {

            }
        }



        private string GetRussianMonthName(
            int month)
        {
            string[] months =
            {
                "",
                "Январь",
                "Февраль",
                "Март",
                "Апрель",
                "Май",
                "Июнь",
                "Июль",
                "Август",
                "Сентябрь",
                "Октябрь",
                "Ноябрь",
                "Декабрь"
            };

            if (month < 1 ||
                month > 12)
            {
                return "";
            }

            return months[month];
        }



        private string EscapeMarkdown(
            string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            return text
                .Replace("\\", "\\\\")
                .Replace("*", "\\*")
                .Replace("_", "\\_")
                .Replace("`", "\\`")
                .Replace("[", "\\[")
                .Replace("]", "\\]");
        }


        public Task HandlePollingErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(
                $"Ошибка Telegram API:");

            Console.WriteLine(
                exception);

            return Task.CompletedTask;
        }



        private class UserSession
        {
            public SessionState State { get; set; } =
                SessionState.Idle;

            public Guid? EditingEventId { get; set; }

            public int TargetYear { get; set; }

            public int TargetMonth { get; set; }

            public int TargetDay { get; set; }

            public int TargetHour { get; set; }

            public int TargetMinute { get; set; }

            public string Description { get; set; } =
                string.Empty;
        }



        private enum SessionState
        {
            Idle,

            ChoosingDate,

            ChoosingHour,

            ChoosingMinute,

            AwaitingDescription
        }
    }
}