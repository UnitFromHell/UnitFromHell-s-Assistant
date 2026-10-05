using System;
using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace UnitFromHellBot.Services
{
    public static class CalendarService
    {
        private static readonly TimeSpan MoscowUtcOffset =
            TimeSpan.FromHours(3);

        private static DateTime MoscowNow =>
            DateTime.UtcNow.Add(MoscowUtcOffset);



        public static InlineKeyboardMarkup BuildCalendarKeyboard(
            int year,
            int month)
        {
            var rows =
                new List<List<InlineKeyboardButton>>();

            DateTime firstDay =
                new DateTime(year, month, 1);

            int daysInMonth =
                DateTime.DaysInMonth(year, month);

            int firstDayOffset =
                ((int)firstDay.DayOfWeek + 6) % 7;

            DateTime today =
                MoscowNow.Date;

            DateTime currentMonth =
                new DateTime(
                    today.Year,
                    today.Month,
                    1);

            var row =
                new List<InlineKeyboardButton>();


            for (int i = 0;
                 i < firstDayOffset;
                 i++)
            {
                row.Add(
                    InlineKeyboardButton.WithCallbackData(
                        " ",
                        "calendar_ignore"));
            }



            for (int day = 1;
                 day <= daysInMonth;
                 day++)
            {
                DateTime currentDate =
                    new DateTime(
                        year,
                        month,
                        day);

                if (currentDate.Date < today)
                {
                    row.Add(
                        InlineKeyboardButton.WithCallbackData(
                            "❌",
                            "calendar_ignore"));
                }
                else
                {
                    row.Add(
                        InlineKeyboardButton.WithCallbackData(
                            day.ToString(),
                            $"calendar_day_{year}_{month}_{day}"));
                }


                if (row.Count == 7)
                {
                    rows.Add(row);
                    row =
                        new List<InlineKeyboardButton>();
                }
            }


            if (row.Count > 0)
            {
                while (row.Count < 7)
                {
                    row.Add(
                        InlineKeyboardButton.WithCallbackData(
                            " ",
                            "calendar_ignore"));
                }

                rows.Add(row);
            }


            DateTime previousMonth =
                currentMonth.AddMonths(-1);

            DateTime nextMonth =
                currentMonth.AddMonths(1);


            var navigationRow =
                new List<InlineKeyboardButton>();

            if (previousMonth < currentMonth)
            {
                navigationRow.Add(
                    InlineKeyboardButton.WithCallbackData(
                        "❌",
                        "calendar_ignore"));
            }
            else
            {
                navigationRow.Add(
                    InlineKeyboardButton.WithCallbackData(
                        "◀",
                        $"calendar_{previousMonth.Year}_{previousMonth.Month}"));
            }


            navigationRow.Add(
                InlineKeyboardButton.WithCallbackData(
                    "📅 Сегодня",
                    "calendar_today"));


            navigationRow.Add(
                InlineKeyboardButton.WithCallbackData(
                    "▶",
                    $"calendar_{nextMonth.Year}_{nextMonth.Month}"));


            rows.Add(navigationRow);


            rows.Add(
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        "🏠 Главное меню",
                        "main_menu")
                });


            return new InlineKeyboardMarkup(rows);
        }


        public static InlineKeyboardMarkup BuildHoursKeyboard(
    int year,
    int month,
    int day)
        {
            var rows =
                new List<List<InlineKeyboardButton>>();

            var row =
                new List<InlineKeyboardButton>();

            DateTime now =
                MoscowNow;

            DateTime selectedDate =
                new DateTime(
                    year,
                    month,
                    day);


            for (int hour = 0;
                 hour < 24;
                 hour++)
            {
                DateTime hourStart =
                    new DateTime(
                        year,
                        month,
                        day,
                        hour,
                        0,
                        0);

                DateTime hourEnd =
                    new DateTime(
                        year,
                        month,
                        day,
                        hour,
                        59,
                        59);

                if (hourEnd <= now)
                {
                    row.Add(
                        InlineKeyboardButton.WithCallbackData(
                            "❌",
                            "time_ignore"));
                }
                else
                {

                    row.Add(
                        InlineKeyboardButton.WithCallbackData(
                            hour.ToString("00"),
                            $"time_hour_{hour}"));
                }


                if (row.Count == 4)
                {
                    rows.Add(row);

                    row =
                        new List<InlineKeyboardButton>();
                }
            }


            if (row.Count > 0)
            {
                rows.Add(row);
            }


            return new InlineKeyboardMarkup(rows);
        }



        public static InlineKeyboardMarkup BuildMinutesKeyboard(
            int year,
            int month,
            int day,
            int hour)
        {
            int[] minutes =
            {
                0,
                5,
                10,
                15,
                20,
                25,
                30,
                35,
                40,
                45,
                50,
                55
            };


            var rows =
                new List<List<InlineKeyboardButton>>();

            var row =
                new List<InlineKeyboardButton>();


            DateTime now =
                MoscowNow;


            foreach (int minute in minutes)
            {
                DateTime selectedTime =
                    new DateTime(
                        year,
                        month,
                        day,
                        hour,
                        minute,
                        0);


                if (selectedTime <= now)
                {
                    row.Add(
                        InlineKeyboardButton.WithCallbackData(
                            "❌",
                            "time_ignore"));
                }
                else
                {
                    row.Add(
                        InlineKeyboardButton.WithCallbackData(
                            minute.ToString("00"),
                            $"time_minute_{minute}"));
                }


                if (row.Count == 4)
                {
                    rows.Add(row);

                    row =
                        new List<InlineKeyboardButton>();
                }
            }


            if (row.Count > 0)
            {
                rows.Add(row);
            }


            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup BuildEventActions(
            Guid eventId,
            int year,
            int month,
            int day)
        {
            return new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "🔄 Перенести",
                            $"event_reschedule_{eventId}"),

                        InlineKeyboardButton.WithCallbackData(
                            "🗑 Удалить",
                            $"event_delete_{eventId}")
                    }
                });
        }


        public static InlineKeyboardMarkup BuildDayKeyboard(
            int year,
            int month,
            int day)
        {
            DateTime selectedDate =
                new DateTime(
                    year,
                    month,
                    day);

            DateTime today =
                MoscowNow.Date;

            if (selectedDate.Date < today)
            {
                return new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "❌ Добавить событие",
                                "calendar_ignore")
                        },

                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "📅 Назад к календарю",
                                $"calendar_{year}_{month}")
                        },

                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "🏠 Главное меню",
                                "main_menu")
                        }
                    });
            }


            return new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "➕ Добавить событие",
                            $"event_add_{year}_{month}_{day}")
                    },

                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "📅 Назад к календарю",
                            $"calendar_{year}_{month}")
                    },

                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "🏠 Главное меню",
                            "main_menu")
                    }
                });
        }



        public static InlineKeyboardMarkup BuildCreatedEventKeyboard(
            Guid eventId,
            int year,
            int month,
            int day)
        {
            return new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "🔄 Перенести",
                            $"event_reschedule_{eventId}"),

                        InlineKeyboardButton.WithCallbackData(
                            "🗑 Удалить",
                            $"event_delete_{eventId}")
                    },

                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "📅 Открыть день",
                            $"calendar_day_{year}_{month}_{day}")
                    },

                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "🏠 Главное меню",
                            "main_menu")
                    }
                });
        }
    }
}