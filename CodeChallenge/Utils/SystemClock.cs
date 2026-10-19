using System;

namespace CodeChallenge.Utils
{
    public class SystemClock : IClock
    {
        // Today is the server's local calendar day. Stored effective dates are calendar days too, parsed from
        // yyyy-MM-dd text with no time zone, so the two compare as days and there is nothing to convert.

        /// <inheritdoc/>
        public DateTime Today => DateTime.Today;
    }
}
