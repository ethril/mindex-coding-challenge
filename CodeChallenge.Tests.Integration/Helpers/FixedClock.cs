using System;

using CodeChallenge.Utils;

namespace CodeChallenge.Tests.Integration.Helpers
{
    public class FixedClock : IClock
    {
        public FixedClock(DateTime today)
        {
            Today = today.Date;
        }

        /// <inheritdoc/>
        public DateTime Today { get; }
    }
}
