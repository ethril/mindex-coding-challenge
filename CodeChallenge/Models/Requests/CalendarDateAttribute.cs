using System;
using System.ComponentModel.DataAnnotations;
using CodeChallenge.Utils;

namespace CodeChallenge.Models.Requests
{
    /// <summary>
    /// Accepts a string only when it is a calendar day in the form YYYY-MM-DD.
    /// </summary>
    /// <remarks>
    /// A null value passes, so that RequiredAttribute alone decides whether the value may be missing.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class CalendarDateAttribute : ValidationAttribute
    {
        // I wrote this rather than using RegularExpression because a pattern would let 2026-02-30 through, and the
        // parse that catches it would then have to raise the 400 from the service. With the check here, every rule
        // for the body stays in the attributes, and [ApiController] turns a bad date into the usual 400 before the
        // action runs. The same attribute guards the asOf query parameter.

        public CalendarDateAttribute()
            : base("The {0} field must be a date in the form YYYY-MM-DD.")
        {
        }

        /// <inheritdoc/>
        public override bool IsValid(object value)
        {
            return value == null || (value is string text && DateUtils.TryParseCalendarDate(text, out _));
        }
    }
}
