using CodeChallenge.Models;
using System.Threading.Tasks;

namespace CodeChallenge.Repositories
{
    public interface IEmployeeRepository
    {
        Employee GetById(string id);

        /// <summary>
        /// Gets an employee by their unique identifier, with their entire reporting hierarchy loaded.
        /// </summary>
        /// <remarks>
        /// Each employee is loaded at most once, so a reporting loop cannot cause infinite recursion while loading the
        /// hierarchy. But the loop remains intact in the returned graph. Any callers that serialize it should treat
        /// looping data as invalid.
        /// </remarks>
        /// <param name="employeeId">The employee's unique identifier.</param>
        /// <returns>The employee, or null if no employee has that ID.</returns>
        Employee GetByIdWithReportingHierarchy(string employeeId);

        Employee Add(Employee employee);
        Employee Remove(Employee employee);
        Task SaveAsync();
    }
}