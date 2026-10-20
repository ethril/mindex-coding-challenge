using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeChallenge.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CodeChallenge.Data;

namespace CodeChallenge.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly EmployeeContext _employeeContext;
        private readonly ILogger<IEmployeeRepository> _logger;

        public EmployeeRepository(ILogger<IEmployeeRepository> logger, EmployeeContext employeeContext)
        {
            _employeeContext = employeeContext;
            _logger = logger;
        }

        public Employee Add(Employee employee)
        {
            employee.EmployeeId = Guid.NewGuid().ToString();
            _employeeContext.Employees.Add(employee);
            return employee;
        }

        public Employee GetById(string id)
        {
            return _employeeContext.Employees.SingleOrDefault(e => e.EmployeeId == id);
        }

        // The first version of the hierarchy loader, kept for comparison and replaced by the per-level version below.
        /*
        /// <inheritdoc/>
        /// <remarks>Issues one query for the employee and one more for each employee in the subtree.</remarks>
        public Employee GetByIdWithReportingHierarchy(string employeeId)
        {
            // I started with Include(e => e.DirectReports), but that loads only the first level of DirectReports. For
            // John Lennon, it brings in Paul and Ringo but not Ringo's reports, Pete and George, so the count would be
            // 2 instead of 4. Using ThenInclude also only extends that to a fixed depth, and the hierarchy's depth
            // isn't known in advance. So instead, I've opted to load each employee's reports explicitly and
            // recursively, performing one query per employee in the subtree.

            var employee = GetById(employeeId);
            if (employee != null)
                LoadDirectReports(employee, new HashSet<string>());
            return employee;
        }

        private void LoadDirectReports(Employee employee, HashSet<string> loadedEmployeeIds)
        {
            // Each employee has at most one parent node, so seeing an ID twice can only mean a reporting loop. This
            // doesn't happen in the provided data, but it could in a real system if someone misconfigures the reporting
            // structure. So, I added a guard against that by keeping track of the employee IDs that are already loaded
            // in this subtree. Without it, a loop would continue until the stack overflowed, and the warning message
            // makes the loop visible.

            if (!loadedEmployeeIds.Add(employee.EmployeeId))
            {
                _logger.LogWarning("Reporting loop detected at employee '{EmployeeId}'.", employee.EmployeeId);
                return;
            }

            _employeeContext.Entry(employee).Collection(e => e.DirectReports).Load();

            foreach (var directReport in employee.DirectReports)
                LoadDirectReports(directReport, loadedEmployeeIds);
        }
        */

        /// <inheritdoc/>
        /// <remarks>
        /// Issues one query for the employee and one more for each level of the subtree. A reporting loop is cut where
        /// it leads back into the hierarchy, so the returned graph is always a tree.
        /// </remarks>
        public Employee GetByIdWithReportingHierarchy(string employeeId)
        {
            // The first version of this method, commented out above, loaded DirectReports with one query per employee
            // in the subtree. The in-memory provider has no indexes and scans the whole Employees table for every
            // query, so that cost grew with the size of the subtree times the size of the table. This version loads the
            // subtree one level at a time instead. Each query fetches every employee whose manager is on the current
            // level, so the number of queries follows the depth of the hierarchy.

            // I used AsNoTracking on both queries because I put each report in its manager's DirectReports below.
            // Without it, EF would also add every report it loads to that list, so each would show up twice.

            var employee = _employeeContext.Employees
                .AsNoTracking()
                .SingleOrDefault(e => e.EmployeeId == employeeId);
            if (employee == null)
                return null;

            employee.DirectReports = new List<Employee>();
            var employeesById = new Dictionary<string, Employee> { [employee.EmployeeId] = employee };
            var levelIds = new HashSet<string> { employee.EmployeeId };

            while (levelIds.Count > 0)
            {
                // I used a HashSet for levelIds because the in-memory provider runs the query below by checking every
                // employee's ManagerId against it. A HashSet finds an ID in one step, but a List searches the whole
                // level for each check.

                var rows = _employeeContext.Employees
                    .AsNoTracking()
                    .Where(e => Enumerable.Contains(
                        levelIds, EF.Property<string>(e, EmployeeContext.ManagerIdProperty)))
                    .Select(e => new
                    {
                        Employee = e,
                        ManagerId = EF.Property<string>(e, EmployeeContext.ManagerIdProperty),
                    })
                    .ToList();

                levelIds = new HashSet<string>();
                foreach (var row in rows)
                {
                    // Each employee has one manager, so an employee who is already in the tree can only have
                    // been reached again through a reporting loop. Skipping them cuts the loop there.

                    var report = row.Employee;
                    if (!employeesById.TryAdd(report.EmployeeId, report))
                    {
                        _logger.LogWarning("Reporting loop detected at employee '{EmployeeId}'.", report.EmployeeId);
                        continue;
                    }

                    report.DirectReports = new List<Employee>();
                    employeesById[row.ManagerId].DirectReports.Add(report);
                    levelIds.Add(report.EmployeeId);
                }
            }

            return employee;
        }

        public Task SaveAsync()
        {
            return _employeeContext.SaveChangesAsync();
        }

        public Employee Remove(Employee employee)
        {
            return _employeeContext.Remove(employee).Entity;
        }
    }
}
