using System.ComponentModel.DataAnnotations;

namespace CodeChallenge.Models.Requests
{
    /// <summary>
    /// Names an existing employee by ID in a request body, in the same nested shape that responses use for an Employee.
    /// </summary>
    public class EmployeeReference
    {
        /// <summary>
        /// The employee's unique identifier.
        /// </summary>
        [Required(ErrorMessage = "employee.employeeId is required.")]
        public string EmployeeId { get; set; }
    }
}
