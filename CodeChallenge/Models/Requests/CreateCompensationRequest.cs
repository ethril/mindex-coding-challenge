using System.ComponentModel.DataAnnotations;

namespace CodeChallenge.Models.Requests
{
    // When the body was bound straight to the Compensation entity, its employee field deserialized into a fresh
    // Employee object carrying an existing ID. After the controller looked the employee up, that object was a second
    // copy of a row EF was already tracking, and passing it to Add threw a "another instance with the same key value is
    // already being tracked" exception. Without the lookup, Add tried to insert a duplicate employee instead. The
    // service had to swap the copy out for the stored instance before saving. So I added this request model that names
    // the employee by ID only, leaving no copy to swap. The service builds the entity pointing to the stored employee,
    // so the already-tracked/duplicate problem cannot occur.

    /// <summary>
    /// The body of a POST to /api/compensation.
    /// </summary>
    public class CreateCompensationRequest
    {
        /// <summary>
        /// The employee the compensation is for.
        /// </summary>
        [Required(ErrorMessage = "employee is required.")]
        public EmployeeReference Employee { get; set; }

        /// <summary>
        /// The salary.
        /// </summary>
        /// <remarks>
        /// Nullable so that a missing value in the request arrives as null instead of 0, and required so that null is
        /// rejected with a 400 status code.
        /// </remarks>
        [Required(ErrorMessage = "salary is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "salary cannot be negative.")]
        public decimal? Salary { get; set; }

        /// <summary>
        /// The day the salary takes effect, as YYYY-MM-DD.
        /// </summary>
        /// <remarks>
        /// A string rather than a DateTime, so that only the YYYY-MM-DD form is accepted and a missing value arrives
        /// as null instead of 0001-01-01. Required rejects null and CalendarDate rejects anything that is not a plain
        /// date, each with a 400 status code.
        /// </remarks>
        [Required(ErrorMessage = "effectiveDate is required.")]
        [CalendarDate(ErrorMessage = "effectiveDate must be a date in the form YYYY-MM-DD.")]
        public string EffectiveDate { get; set; }
    }
}
