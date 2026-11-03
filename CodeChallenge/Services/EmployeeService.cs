using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeChallenge.Models;
using Microsoft.Extensions.Logging;
using CodeChallenge.Repositories;

namespace CodeChallenge.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(ILogger<EmployeeService> logger, IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public Employee Create(Employee employee)
        {
            if(employee != null)
            {
                _employeeRepository.Add(employee);
                _employeeRepository.SaveAsync().Wait();
            }

            return employee;
        }

        public Employee GetById(string id)
        {
            if(!string.IsNullOrEmpty(id))
            {
                return _employeeRepository.GetById(id);
            }

            return null;
        }

        public Employee Replace(Employee originalEmployee, Employee newEmployee)
        {
            // The starter replaced an employee by deleting the stored row and inserting the request body under the
            // same ID. The manager link is the shadow property ManagerId, which the body can't carry, so the new row
            // was inserted with no manager and the employee dropped out of their manager's reporting structure. I
            // changed it to update the tracked row in place instead. GetById loads the original in this request's
            // context, so copying the fields onto it and saving once sends a single UPDATE of the changed columns and
            // never touches ManagerId, the employee's own reports, or their compensations. The body's EmployeeId and
            // DirectReports are ignored, because the URL names the employee and moving people between managers isn't
            // something this endpoint supports.

            if(originalEmployee != null && newEmployee != null)
            {
                originalEmployee.FirstName = newEmployee.FirstName;
                originalEmployee.LastName = newEmployee.LastName;
                originalEmployee.Position = newEmployee.Position;
                originalEmployee.Department = newEmployee.Department;
                _employeeRepository.SaveAsync().Wait();
            }

            return originalEmployee;
        }
    }
}
