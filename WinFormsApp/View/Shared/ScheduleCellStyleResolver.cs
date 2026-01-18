using DataAccessLayer.Models;
using System;
using System.Drawing;

namespace WinFormsApp.View.Shared
{
    public sealed class ScheduleCellStyleResolver
    {
        public Color WeekdayBackground { get; }
        public Color WeekendBackground { get; }
        public Color DefaultForeground { get; }

        public ScheduleCellStyleResolver(
            Color? weekdayBackground = null,
            Color? weekendBackground = null,
            Color? defaultForeground = null)
        {
            WeekdayBackground = weekdayBackground ?? Color.White;
            WeekendBackground = weekendBackground ?? Color.Gainsboro;
            DefaultForeground = defaultForeground ?? Color.Black;
        }

        public Color ResolveBackground(ScheduleCellStyleModel? overrideStyle, bool isWeekend)
        {
            var color = ColorHexConverter.FromHex(overrideStyle?.BackgroundHex);
            return color ?? (isWeekend ? WeekendBackground : WeekdayBackground);
        }

        public Color ResolveForeground(ScheduleCellStyleModel? overrideStyle)
            => ColorHexConverter.FromHex(overrideStyle?.ForegroundHex) ?? DefaultForeground;

        public static bool IsWeekend(int year, int month, int day)
        {
            var date = new DateTime(year, month, day);
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }
    }
}
