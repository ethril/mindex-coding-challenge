using System;

namespace CodeChallenge.Utils
{
    /// <summary>
    /// Supplies the current calendar day.
    /// </summary>
    public interface IClock
    {
        // Added so tests can pin today to a fixed date.

        /// <summary>
        /// Today's date on the server, at midnight.
        /// </summary>
        DateTime Today { get; }
    }
}
