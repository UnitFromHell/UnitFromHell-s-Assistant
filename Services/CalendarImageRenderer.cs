using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SkiaSharp;
using UnitFromHellBot.Models;

namespace UnitFromHellBot.Services
{
    public class CalendarImageRenderer
    {
        private const int Width = 1000;
        private const int Height = 850;

        private readonly SKTypeface _font;

        public CalendarImageRenderer()
        {
            string fontPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Fonts",
                    "DejaVuSans.ttf");

            if (!File.Exists(fontPath))
            {
                throw new FileNotFoundException(
                    $"Шрифт не найден: {fontPath}");
            }

            _font =
                SKTypeface.FromFile(fontPath);

            if (_font == null)
            {
                throw new Exception(
                    $"Не удалось загрузить шрифт: {fontPath}");
            }
        }

        public MemoryStream Render(
            int year,
            int month,
            IEnumerable<ReminderEvent> events)
        {
            var eventsList =
                events.ToList();

            using var bitmap =
                new SKBitmap(
                    Width,
                    Height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Premul);

            using var canvas =
                new SKCanvas(bitmap);

            canvas.Clear(
                new SKColor(
                    15,
                    23,
                    32));

            DrawHeader(
                canvas,
                year,
                month);

            DrawWeekDays(canvas);

            DrawCalendar(
                canvas,
                year,
                month,
                eventsList);

            using var image =
                SKImage.FromBitmap(bitmap);

            using var data =
                image.Encode(
                    SKEncodedImageFormat.Png,
                    100);

            var stream =
                new MemoryStream();

            data.SaveTo(stream);

            stream.Position = 0;

            return stream;
        }

        private void DrawHeader(
            SKCanvas canvas,
            int year,
            int month)
        {
            string monthName =
                new DateTime(
                    year,
                    month,
                    1)
                .ToString(
                    "MMMM yyyy",
                    new CultureInfo("ru-RU"));

            monthName =
                char.ToUpper(
                    monthName[0]) +
                monthName.Substring(1);

            using var paint =
                new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = 48,
                    IsAntialias = true,
                    Typeface = _font,
                    TextAlign = SKTextAlign.Center
                };

            canvas.DrawText(
                monthName,
                Width / 2,
                65,
                paint);

            using var linePaint =
                new SKPaint
                {
                    Color =
                        new SKColor(
                            45,
                            58,
                            72),
                    StrokeWidth = 2
                };

            canvas.DrawLine(
                40,
                95,
                Width - 40,
                95,
                linePaint);
        }

        private void DrawWeekDays(
            SKCanvas canvas)
        {
            string[] days =
            {
                "ПН",
                "ВТ",
                "СР",
                "ЧТ",
                "ПТ",
                "СБ",
                "ВС"
            };

            float cellWidth =
                (Width - 80) / 7f;

            using var paint =
                new SKPaint
                {
                    Color =
                        new SKColor(
                            150,
                            165,
                            180),
                    TextSize = 24,
                    IsAntialias = true,
                    Typeface = _font,
                    TextAlign = SKTextAlign.Center
                };

            for (int i = 0; i < 7; i++)
            {
                float x =
                    40 +
                    cellWidth * i +
                    cellWidth / 2;

                canvas.DrawText(
                    days[i],
                    x,
                    135,
                    paint);
            }
        }

        private void DrawCalendar(
            SKCanvas canvas,
            int year,
            int month,
            List<ReminderEvent> events)
        {
            DateTime firstDay =
                new DateTime(
                    year,
                    month,
                    1);

            int daysInMonth =
                DateTime.DaysInMonth(
                    year,
                    month);

            int firstDayOffset =
                ((int)firstDay.DayOfWeek + 6) % 7;

            float cellWidth =
                (Width - 80) / 7f;

            float cellHeight = 105;

            float startY = 155;

            DateTime today =
     DateTime.UtcNow.AddHours(3).Date;

            for (int day = 1;
                 day <= daysInMonth;
                 day++)
            {
                int index =
                    firstDayOffset +
                    day -
                    1;

                int row =
                    index / 7;

                int column =
                    index % 7;

                float x =
                    40 +
                    column * cellWidth;

                float y =
                    startY +
                    row * cellHeight;

                DateTime currentDate =
                    new DateTime(
                        year,
                        month,
                        day);

                int eventCount =
                    events.Count(e =>
                        e.TargetTime.Date ==
                        currentDate.Date);

                bool isToday =
                    currentDate.Date ==
                    today;

                DrawDayCell(
                    canvas,
                    x,
                    y,
                    cellWidth,
                    cellHeight,
                    day,
                    eventCount,
                    isToday);
            }
        }

        private void DrawDayCell(
            SKCanvas canvas,
            float x,
            float y,
            float width,
            float height,
            int day,
            int eventCount,
            bool isToday)
        {
            var rect =
                new SKRect(
                    x + 5,
                    y + 5,
                    x + width - 5,
                    y + height - 5);

            using var backgroundPaint =
                new SKPaint
                {
                    Color =
                        isToday
                            ? new SKColor(
                                35,
                                72,
                                105)
                            : new SKColor(
                                25,
                                36,
                                48),
                    IsAntialias = true
                };

            canvas.DrawRoundRect(
                rect,
                14,
                14,
                backgroundPaint);

            using var borderPaint =
                new SKPaint
                {
                    Color =
                        isToday
                            ? new SKColor(
                                75,
                                170,
                                240)
                            : new SKColor(
                                42,
                                55,
                                68),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    IsAntialias = true
                };

            canvas.DrawRoundRect(
                rect,
                14,
                14,
                borderPaint);



            using var numberPaint =
                new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = 27,
                    IsAntialias = true,
                    Typeface = _font
                };

            canvas.DrawText(
                day.ToString(),
                x + 18,
                y + 34,
                numberPaint);



            if (eventCount == 0)
                return;

            string dots;

            if (eventCount >= 4)
                dots = "••••";
            else
                dots =
                    new string(
                        '•',
                        eventCount);

            using var eventPaint =
                new SKPaint
                {
                    Color =
                        new SKColor(
                            80,
                            190,
                            255),
                    TextSize = 25,
                    IsAntialias = true,
                    Typeface = _font,
                    TextAlign = SKTextAlign.Center
                };

            canvas.DrawText(
                dots,
                x + width / 2,
                y + 75,
                eventPaint);
        }
    }
}