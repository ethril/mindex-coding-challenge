using System;
using System.Linq;
using CodeChallenge.Data;
using CodeChallenge.Models;
using CodeChallenge.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeChallenge.Tests.Integration
{
    [TestClass]
    public class EmployeeRepositoryTests
    {
        // These tests run the repository against its own in-memory database rather than through the API, because the
        // API has no way to create a reporting loop.

        /// <summary>
        /// Verifies that a reporting loop between two employees is cut where it leads back into the hierarchy. A and B
        /// each manage the other, so starting from A, B is A's only report and B has none. The first version of the
        /// loader left A in B's DirectReports, and the cycle made the response impossible to serialize.
        /// </summary>
        [TestMethod]
        public void GetByIdWithReportingHierarchy_TwoEmployeesManageEachOther_CutsTheLoop()
        {
            // Arrange
            var options = NewDatabaseOptions();
            using (var context = new EmployeeContext(options))
            {
                var employeeA = new Employee { EmployeeId = "EmployeeA_Id" };
                var employeeB = new Employee { EmployeeId = "EmployeeB_Id" };
                context.Employees.AddRange(employeeA, employeeB);
                SetManager(context, employeeA, employeeB);
                SetManager(context, employeeB, employeeA);
                context.SaveChanges();
            }

            // Execute
            Employee employeeWithHierarchy;
            using (var context = new EmployeeContext(options))
            {
                employeeWithHierarchy = NewRepository(context).GetByIdWithReportingHierarchy("EmployeeA_Id");
            }

            // Assert
            var report = employeeWithHierarchy.DirectReports.Single();
            Assert.AreEqual("EmployeeB_Id", report.EmployeeId);
            Assert.AreEqual(0, report.DirectReports.Count, "The loop back to employee A should be cut.");
        }

        /// <summary>
        /// Verifies that an employee set as their own manager comes back with no reports, rather than as their own
        /// report.
        /// </summary>
        [TestMethod]
        public void GetByIdWithReportingHierarchy_EmployeeManagesThemselves_ReturnsNoReports()
        {
            // Arrange
            var options = NewDatabaseOptions();
            using (var context = new EmployeeContext(options))
            {
                var employee = new Employee { EmployeeId = "ManagesSelf_Id" };
                context.Employees.Add(employee);
                SetManager(context, employee, employee);
                context.SaveChanges();
            }

            // Execute
            Employee employeeWithHierarchy;
            using (var context = new EmployeeContext(options))
            {
                employeeWithHierarchy = NewRepository(context).GetByIdWithReportingHierarchy("ManagesSelf_Id");
            }

            // Assert
            Assert.AreEqual(0, employeeWithHierarchy.DirectReports.Count);
        }

        private static DbContextOptions<EmployeeContext> NewDatabaseOptions()
        {
            return new DbContextOptionsBuilder<EmployeeContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private static EmployeeRepository NewRepository(EmployeeContext context)
        {
            return new EmployeeRepository(NullLogger<IEmployeeRepository>.Instance, context);
        }

        private static void SetManager(EmployeeContext context, Employee employee, Employee manager)
        {
            context.Entry(employee).Property(EmployeeContext.ManagerIdProperty).CurrentValue = manager.EmployeeId;
        }
    }
}
