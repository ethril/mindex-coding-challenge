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
        /// Each employee is added to the hierarchy at most once. When a reporting loop leads back to an employee who
        /// is already in it, that employee isn't added again and a warning is logged, so loading always finishes and
        /// the returned graph never contains the loop.
        /// </remarks>
        /// <param name="employeeId">The employee's unique identifier.</param>
        /// <returns>The employee, or null if no employee has that ID.</returns>
        Employee GetByIdWithReportingHierarchy(string employeeId);

        Employee Add(Employee employee);
        Employee Remove(Employee employee);
        Task SaveAsync();
    }
}