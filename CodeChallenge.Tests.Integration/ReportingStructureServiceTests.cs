using CodeChallenge.Models;
using CodeChallenge.Repositories;
using CodeChallenge.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeChallenge.Tests.Integration
{
    [TestClass]
    public class ReportingStructureServiceTests
    {
        /// <summary>
        /// Verifies that if an employee is set as their own direct report, they are not counted and the process does
        /// not enter an infinite loop.
        /// </summary>
        [TestMethod]
        public void GetById_EmployeeReportsToThemselves_Returns_Zero()
        {
            // Arrange
            var employee = new Employee { EmployeeId = "ReportsToSelf_Id" };
            employee.DirectReports = new List<Employee> { employee };
            var service = new ReportingStructureService(
                NullLogger<ReportingStructureService>.Instance,
                new FakeEmployeeRepository(employee));
            var expectedCount = 0;

            // Execute
            var reportingStructure = service.GetById(employee.EmployeeId);

            // Assert
            Assert.AreEqual(expectedCount, reportingStructure.NumberOfReports);
        }

        /// <summary>
        /// Verifies that a reporting loop between two employees is counted correctly. A and B each list the other as a
        /// direct report, so starting from A, B is A's only report. When the loop leads back to employee A, A is not
        /// counted again.
        /// </summary>
        [TestMethod]
        public void GetById_TwoEmployeesReportToEachOther_Returns_One()
        {
            // Arrange
            var employeeA = new Employee { EmployeeId = "EmployeeA_Id" };
            var employeeB = new Employee { EmployeeId = "EmployeeB_Id" };
            employeeA.DirectReports = new List<Employee> { employeeB };
            employeeB.DirectReports = new List<Employee> { employeeA };

            var service = new ReportingStructureService(
                NullLogger<ReportingStructureService>.Instance,
                new FakeEmployeeRepository(employeeA));
            var expectedCount = 1;

            // Execute
            var reportingStructure = service.GetById(employeeA.EmployeeId);

            // Assert
            Assert.AreEqual(expectedCount, reportingStructure.NumberOfReports);
        }

        /// <summary>
        /// Verifies that an unknown ID yields null rather than an empty structure.
        /// </summary>
        [TestMethod]
        public void GetById_UnknownId_Returns_Null()
        {
            // Arrange
            var employee = new Employee { EmployeeId = "Known_Id" };
            var service = new ReportingStructureService(
                NullLogger<ReportingStructureService>.Instance,
                new FakeEmployeeRepository(employee));

            // Execute
            var reportingStructure = service.GetById("Unknown_Id");

            // Assert
            Assert.IsNull(reportingStructure);
        }

        private class FakeEmployeeRepository : IEmployeeRepository
        {
            private readonly Employee _employee;

            public FakeEmployeeRepository(Employee employee)
            {
                _employee = employee;
            }

            public Employee GetByIdWithReportingHierarchy(string id)
            {
                return id == _employee.EmployeeId ? _employee : null;
            }

            public Employee GetById(string id)
            {
                throw new NotImplementedException();
            }

            public Employee Add(Employee employee)
            {
                throw new NotImplementedException();
            }

            public Employee Remove(Employee employee)
            {
                throw new NotImplementedException();
            }

            public Task SaveAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
