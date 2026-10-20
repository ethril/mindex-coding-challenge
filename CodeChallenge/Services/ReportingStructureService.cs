using System.Collections.Generic;
using CodeChallenge.Models;
using Microsoft.Extensions.Logging;
using CodeChallenge.Repositories;

namespace CodeChallenge.Services
{
    public class ReportingStructureService : IReportingStructureService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<ReportingStructureService> _logger;

        public ReportingStructureService(
            ILogger<ReportingStructureService> logger,
            IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public ReportingStructure GetById(string employeeId)
        {
            // I return null here for an unknown or empty ID and let the controller turn that into a generic 404 error,
            // mirroring how the existing EmployeeService.GetById and EmployeeController behave. The potential downside
            // of this is ambiguity. The controller wouldn't know whether the ID is unknown or empty. However, an empty
            // ID cannot reach this method over HTTP, because the route requires the segment, so in practice the 404
            // always means that no employee exists with that ID.

            if (!string.IsNullOrEmpty(employeeId))
            {
                // GetByIdWithReportingHierarchy provides the employee with the whole hierarchy loaded, so the count
                // below works entirely on objects already in memory and runs no additional queries. The queries occur
                // in the repository, one per employee in the subtree. The in-memory provider isn't relational and
                // cannot run SQL, so a recursive CTE isn't an option here. If it were SQL Server, I would consider one
                // to load the whole subtree in a single round trip instead of one per employee, which also gives one
                // consistent snapshot of the hierarchy.
                //
                // UPDATE: The repository now loads the hierarchy one level at a time, so it runs one query for the
                // employee and one for each level of the subtree rather than one per employee, which is 4 queries
                // instead of 6 for John Lennon.
                var employee = _employeeRepository.GetByIdWithReportingHierarchy(employeeId);
                if (employee != null)
                {
                    return new ReportingStructure
                    {
                        Employee = employee,
                        NumberOfReports = CountReports(employee, new HashSet<string> { employee.EmployeeId })
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Counts the employees below <paramref name="employee"/> at every depth.
        /// </summary>
        /// <param name="employee">The employee whose reports are counted.</param>
        /// <param name="countedEmployeeIds">The IDs that have been counted so far.</param>
        /// <returns>The number of distinct employees under <paramref name="employee"/>.</returns>
        private static int CountReports(Employee employee, HashSet<string> countedEmployeeIds)
        {
            // I used a HashSet so each employee under the root is counted exactly once when they're first reached, and
            // recursion continues only for reports not already in the collection. The collection starts with the root's
            // own ID, so an employee listed as their own report wouldn't be counted. This stops reporting loops and
            // self-references from recursing forever. I kept the null checks because the IEmployeeRepository contract
            // doesn't promise a non-null DirectReports. The EF implementation returns an empty list for a leaf, but
            // other implementations, like the fake repository in the unit tests I added, may return null.

            if (employee.DirectReports == null)
                return 0;

            var count = 0;

            foreach (var directReport in employee.DirectReports)
            {
                if (directReport != null && countedEmployeeIds.Add(directReport.EmployeeId))
                    count += 1 + CountReports(directReport, countedEmployeeIds);
            }

            return count;
        }
    }
}
