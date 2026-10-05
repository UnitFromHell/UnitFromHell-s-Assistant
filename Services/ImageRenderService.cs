using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UnitFromHellBot.Models;

namespace UnitFromHellBot.Services
{
    public static class ImageRenderService
    {
        public static Stream GenerateCalendarImage(int year, int month, List<ReminderEvent> events)
        {
            int width = 450;
            int height = 400;

            var image = new Image<Rgba32>(width, height);

            var bgColor = Color.ParseHex("#1e1e2e");
            var headerBgColor = Color.ParseHex("#313244");
            var textColor = Color.ParseHex("#cdd6f4");
            var weekendColor = Color.ParseHex("#f38ba8");
            var hasEventColor = Color.ParseHex("#a6e3a1");

            image.Mutate(ctx =>
            {
                ctx.Clear(bgColor);

                ctx.Fill(headerBgColor, new RectangleF(0, 0, width, 50));

                FontFamily family;
                if (!SystemFonts.Collection.TryGet("Arial", out family))
                {
                    family = SystemFonts.Collection.Families.FirstOrDefault();
                }

                Font font = family.CreateFont(18, FontStyle.Bold);
                Font dayFont = family.CreateFont(14, FontStyle.Regular);

                string monthName = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetMonthName(month);
                string title = $"{char.ToUpper(monthName[0])}{monthName.Substring(1)} {year}";

                ctx.DrawText(title, font, textColor, new PointF(20, 12));


                string[] weekdays = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
                int cellWidth = width / 7;

                for (int i = 0; i < 7; i++)
                {
                    var color = (i >= 5) ? weekendColor : textColor;
                    ctx.DrawText(weekdays[i], dayFont, color, new PointF(i * cellWidth + 20, 65));
                }

                DateTime firstDay = new DateTime(year, month, 1);
                int daysInMonth = DateTime.DaysInMonth(year, month);
                int startOffset = ((int)firstDay.DayOfWeek == 0) ? 6 : (int)firstDay.DayOfWeek - 1;

                int currentCell = startOffset;
                int row = 0;

                var eventDays = events
                    .Where(e => e.TargetTime.Year == year && e.TargetTime.Month == month)
                    .Select(e => e.TargetTime.Day)
                    .ToHashSet();

                for (int day = 1; day <= daysInMonth; day++)
                {
                    int x = (currentCell % 7) * cellWidth;
                    int y = 100 + (row * 50);

                    if (eventDays.Contains(day))
                    {
                        ctx.Fill(hasEventColor, new EllipsePolygon(new PointF(x + 32, y + 15), 18));
                        ctx.DrawText(day.ToString(), dayFont, bgColor, new PointF(x + 22, y + 5));
                    }
                    else
                    {
                        var color = ((currentCell % 7) >= 5) ? weekendColor : textColor;
                        ctx.DrawText(day.ToString(), dayFont, color, new PointF(x + 22, y + 5));
                    }

                    currentCell++;
                    if (currentCell % 7 == 0 && day < daysInMonth)
                    {
                        row++;
                    }
                }
            });

            var memoryStream = new MemoryStream();
            image.SaveAsPng(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
