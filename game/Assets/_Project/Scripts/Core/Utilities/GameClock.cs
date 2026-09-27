using System;

namespace NinjaVillage.Core.Utilities
{
    /// <summary>
    /// The game's notion of "now" for everything time-gated: daily login, daily/weekly
    /// quests, the Market's daily stock, battle pass seasons, limited-time offers.
    ///
    /// Uses device UTC by default; the backend calls <see cref="SetServerTime"/> once it
    /// knows the server time, which blunts clock-change cheating. Daily content resets
    /// at <see cref="DailyResetHourUtc"/>.
    /// </summary>
    public static class GameClock
    {
        public const int DailyResetHourUtc = 0;

        private static readonly DateTime Epoch = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc); // a Monday
        private static TimeSpan _serverOffset = TimeSpan.Zero;

        /// <summary>Tests can pin the clock; null = real time.</summary>
        public static Func<DateTime> OverrideUtcNow;

        public static DateTime UtcNow => OverrideUtcNow?.Invoke() ?? DateTime.UtcNow + _serverOffset;

        public static bool HasServerTime { get; private set; }

        public static void SetServerTime(DateTime serverUtcNow)
        {
            _serverOffset = serverUtcNow.ToUniversalTime() - DateTime.UtcNow;
            HasServerTime = true;
        }

        /// <summary>Days since the epoch, rolling over at the daily reset hour.</summary>
        public static int DayIndex(DateTime utc) =>
            (int)Math.Floor((utc - Epoch - TimeSpan.FromHours(DailyResetHourUtc)).TotalDays);

        /// <summary>Weeks since the epoch; weeks start Monday at the reset hour.</summary>
        public static int WeekIndex(DateTime utc) => (int)Math.Floor(DayIndex(utc) / 7.0);

        public static int Today => DayIndex(UtcNow);
        public static int ThisWeek => WeekIndex(UtcNow);

        public static DateTime StartOfDay(int dayIndex) => Epoch.AddDays(dayIndex).AddHours(DailyResetHourUtc);

        public static TimeSpan UntilNextDailyReset => StartOfDay(Today + 1) - UtcNow;
        public static TimeSpan UntilNextWeeklyReset => StartOfDay((ThisWeek + 1) * 7) - UtcNow;

        /// <summary>"3h 12m" / "2d 4h" style countdown for UI.</summary>
        public static string FormatCountdown(TimeSpan span)
        {
            if (span < TimeSpan.Zero) span = TimeSpan.Zero;
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d {span.Hours}h";
            if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h {span.Minutes}m";
            return $"{span.Minutes}m {span.Seconds}s";
        }
    }
}
