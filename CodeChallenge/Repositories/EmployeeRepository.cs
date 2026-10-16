using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeChallenge.Models;
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
