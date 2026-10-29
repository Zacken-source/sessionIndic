using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    // Draws Asia / London / New York sessions with their high and low.
    // Hours are in UTC (winter time), London and NY are shifted 1h in summer.
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SessionIndic : Indicator
    {
        [Parameter("Days to show", DefaultValue = 5, MinValue = 1, MaxValue = 30)]
        public int DaysToShow { get; set; }

        [Parameter("Auto summer time", DefaultValue = true)]
        public bool AutoDst { get; set; }

        [Parameter("Extend last highs/lows", DefaultValue = true)]
        public bool ExtendLevels { get; set; }

        [Parameter("Opacity", DefaultValue = 40, MinValue = 0, MaxValue = 255)]
        public int Opacity { get; set; }

        [Parameter("Start hour", DefaultValue = 0, MinValue = 0, MaxValue = 23, Group = "Asia")]
        public int AsiaStart { get; set; }
        [Parameter("End hour", DefaultValue = 8, MinValue = 0, MaxValue = 24, Group = "Asia")]
        public int AsiaEnd { get; set; }
        [Parameter("Color", DefaultValue = "Magenta", Group = "Asia")]
        public Color AsiaColor { get; set; }

        [Parameter("Start hour", DefaultValue = 8, MinValue = 0, MaxValue = 23, Group = "London")]
        public int LondonStart { get; set; }
        [Parameter("End hour", DefaultValue = 17, MinValue = 0, MaxValue = 24, Group = "London")]
        public int LondonEnd { get; set; }
        [Parameter("Color", DefaultValue = "Gold", Group = "London")]
        public Color LondonColor { get; set; }

        [Parameter("Start hour", DefaultValue = 13, MinValue = 0, MaxValue = 23, Group = "New York")]
        public int NyStart { get; set; }
        [Parameter("End hour", DefaultValue = 22, MinValue = 0, MaxValue = 24, Group = "New York")]
        public int NyEnd { get; set; }
        [Parameter("Color", DefaultValue = "DodgerBlue", Group = "New York")]
        public Color NyColor { get; set; }

        private List<string> objects = new List<string>();
        private int lastIndex = -1;
        private int counter = 0;

        protected override void Initialize()
        {
        }

        public override void Calculate(int index)
        {
            // redraw once per new bar, not on every tick
            if (!IsLastBar || index == lastIndex)
                return;
            lastIndex = index;

            if (TimeFrame == TimeFrame.Daily || TimeFrame == TimeFrame.Weekly || TimeFrame == TimeFrame.Monthly)
                return;

            foreach (var name in objects)
                Chart.RemoveObject(name);
            objects.Clear();

            DateTime day = Bars.OpenTimes.LastValue.Date;
            int daysDrawn = 0;

            while (daysDrawn < DaysToShow && day >= Bars.OpenTimes[0].Date)
            {
                if (day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday)
                {
                    bool recent = daysDrawn < 2;
                    DrawSession("Asia", day, AsiaStart, AsiaEnd, 0, AsiaColor, recent);
                    DrawSession("London", day, LondonStart, LondonEnd, EuropeDst(day) ? 1 : 0, LondonColor, recent);
                    DrawSession("New York", day, NyStart, NyEnd, UsDst(day) ? 1 : 0, NyColor, recent);
                    daysDrawn++;
                }
                day = day.AddDays(-1);
            }
        }

        private void DrawSession(string name, DateTime day, int startHour, int endHour, int shift, Color color, bool extend)
        {
            if (!AutoDst)
                shift = 0;

            DateTime start = day.AddHours(startHour - shift);
            DateTime end = day.AddHours(endHour - shift);
            if (end <= start)
                end = end.AddDays(1);

            if (start > Bars.OpenTimes.LastValue)
                return;

            int i = Bars.OpenTimes.GetIndexByTime(start);
            if (i < 0)
                return;
            while (i < Bars.Count && Bars.OpenTimes[i] < start)
                i++;

            double high = double.MinValue;
            double low = double.MaxValue;
            int lastBar = -1;
            for (; i < Bars.Count && Bars.OpenTimes[i] < end; i++)
            {
                high = Math.Max(high, Bars.HighPrices[i]);
                low = Math.Min(low, Bars.LowPrices[i]);
                lastBar = i;
            }
            if (lastBar < 0)
                return;

            var rect = Chart.DrawRectangle(NewName(), start, high, end, low, Color.FromArgb(Opacity, color));
            rect.IsFilled = true;

            double pips = (high - low) / Symbol.PipSize;
            var text = Chart.DrawText(NewName(), name + " " + pips.ToString("F1"), start, high, color);
            text.FontSize = 8;
            text.VerticalAlignment = VerticalAlignment.Top;

            // session finished -> show its high/low until price closes through it
            bool finished = end <= Bars.OpenTimes.LastValue;
            if (ExtendLevels && extend && finished)
            {
                ExtendLevel(high, lastBar, end, true, color);
                ExtendLevel(low, lastBar, end, false, color);
            }
        }

        private void ExtendLevel(double price, int fromBar, DateTime from, bool isHigh, Color color)
        {
            DateTime to = Bars.OpenTimes.LastValue;
            for (int i = fromBar + 1; i < Bars.Count - 1; i++)
            {
                double close = Bars.ClosePrices[i];
                if ((isHigh && close > price) || (!isHigh && close < price))
                {
                    to = Bars.OpenTimes[i];
                    break;
                }
            }
            if (to > from)
                Chart.DrawTrendLine(NewName(), from, price, to, price, color, 1, LineStyle.Dots);
        }

        // Europe: last sunday of march -> last sunday of october
        private bool EuropeDst(DateTime day)
        {
            return day >= LastSunday(day.Year, 3) && day < LastSunday(day.Year, 10);
        }

        // USA: second sunday of march -> first sunday of november
        private bool UsDst(DateTime day)
        {
            return day >= FirstSunday(day.Year, 3).AddDays(7) && day < FirstSunday(day.Year, 11);
        }

        private DateTime LastSunday(int year, int month)
        {
            var d = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            while (d.DayOfWeek != DayOfWeek.Sunday)
                d = d.AddDays(-1);
            return d;
        }

        private DateTime FirstSunday(int year, int month)
        {
            var d = new DateTime(year, month, 1);
            while (d.DayOfWeek != DayOfWeek.Sunday)
                d = d.AddDays(1);
            return d;
        }

        private string NewName()
        {
            string name = "session_" + counter++;
            objects.Add(name);
            return name;
        }
    }
}
