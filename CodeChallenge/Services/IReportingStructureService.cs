using CodeChallenge.Models;

namespace CodeChallenge.Services
{
    public interface IReportingStructureService
    {
        /// <summary>
        /// Builds the entire reporting structure for an employee, including both direct and indirect reports.
        /// </summary>
        /// <param name="employeeId">The employee's unique identifier.</param>
        /// <returns>The reporting structure, or null if the ID is empty or no employee has that ID.</returns>
        ReportingStructure GetById(string employeeId);
    }
}
