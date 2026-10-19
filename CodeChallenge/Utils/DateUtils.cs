using System;
using System.Globalization;

namespace CodeChallenge.Utils
{
    public static class DateUtils
    {
        // Effective dates and the asOf query are calendar days with no time zone. DateOnly would say that exactly,
        // but System.Text.Json on .NET 6 cannot serialize it, so a date travels as yyyy-MM-dd text and I parse it
        // here with an exact format. That way a value carrying a time, a trailing Z or an offset is rejected
        // instead of being converted to the server's zone and quietly landing on the neighbouring day, which is
        // what happened while the request model used a DateTime. The parsed value is midnight with an unspecified
        // kind, so every stored date goes out the same way, without a trailing Z. This is the one place that names
        // the format.

        /// <summary>
        /// The only form a date takes on the wire, such as 2026-01-01.
        /// </summary>
        public const string CalendarDateFormat = "yyyy-MM-dd";

        /// <summary>
        /// Tries to read a calendar day written as <see cref="CalendarDateFormat"/>.
        /// </summary>
        /// <param name="text">The text to read.</param>
        /// <param name="date">Midnight on that day with no kind, or the default if the text is not a date.</param>
        /// <returns>True if the text is a date in that exact form, and false otherwise, including for null.</returns>
        public static bool TryParseCalendarDate(string text, out DateTime date)
        {
            return DateTime.TryParseExact(
                text, CalendarDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        /// <summary>
        /// Reads a calendar day written as <see cref="CalendarDateFormat"/>.
        /// </summary>
        /// <param name="text">The text to read.</param>
        /// <returns>Midnight on that day with no kind.</returns>
        /// <exception cref="FormatException">The text is not a date in that exact form.</exception>
        /// <exception cref="ArgumentNullException">The text is null.</exception>
        public static DateTime ParseCalendarDate(string text)
        {
            return DateTime.ParseExact(text, CalendarDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        /// <summary>
        /// Writes a day as <see cref="CalendarDateFormat"/>.
        /// </summary>
        /// <param name="date">The day to write. The time of day is ignored.</param>
        /// <returns>The day, such as 2026-01-01.</returns>
        public static string FormatCalendarDate(DateTime date)
        {
            return date.ToString(CalendarDateFormat, CultureInfo.InvariantCulture);
        }
    }
}
