using System;
using System.Text.Json.Serialization;

namespace CodeChallenge.Models
{
    public class Compensation
    {
        // I've added the JsonIgnore attribute to EmployeeId, so the client never sees it in the output. In the response
        // object, Employee.EmployeeId remains visible. EF needs scalar key properties, and this one is half of the key
        // and the foreign key to the Employee row.

        /// <summary>
        /// The ID of the employee the compensation belongs to.
        /// </summary>
        [JsonIgnore]
        public string EmployeeId { get; set; }

        /// <summary>
        /// The employee the compensation belongs to.
        /// </summary>
        public Employee Employee { get; set; }

        /// <summary>
        /// The salary.
        /// </summary>
        public decimal Salary { get; set; }

        /// <summary>
        /// The day the salary takes effect, stored as midnight with no kind.
        /// </summary>
        public DateTime EffectiveDate { get; set; }
    }
}
