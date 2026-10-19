using System;
using System.Threading.Tasks;
using CodeChallenge.Models;
using CodeChallenge.Models.Requests;
using CodeChallenge.Repositories;
using CodeChallenge.Services;
using CodeChallenge.Tests.Integration.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeChallenge.Tests.Integration
{
    [TestClass]
    public class CompensationServiceTests
    {
        private static readonly DateTime Today = new(2026, 12, 31);

        /// <summary>
        /// Verifies that Create builds the compensation around the employee the caller looked up, copies the request's
        /// salary onto it, and reads the effective date as a calendar day.
        /// </summary>
        [TestMethod]
        public void Create_BuildsCompensationFromStoredEmployeeAndRequest()
        {
            // Arrange
            var storedEmployee = new Employee { EmployeeId = "Stored_Id", FirstName = "Stored" };
            var request = new CreateCompensationRequest
            {
                Employee = new EmployeeReference { EmployeeId = storedEmployee.EmployeeId },
                Salary = 1000m,
                EffectiveDate = "2026-01-01",
            };
            var expectedEffectiveDate = new DateTime(2026, 1, 1);
            var repository = new FakeCompensationRepository();
            var service = NewService(repository);

            // Execute
            var created = service.Create(storedEmployee, request);

            // Assert
            Assert.AreEqual(storedEmployee.EmployeeId, created.EmployeeId);
            Assert.AreSame(storedEmployee, created.Employee);
            Assert.AreEqual(request.Salary, created.Salary);
            Assert.AreEqual(expectedEffectiveDate, created.EffectiveDate, "The text should be read as a calendar day.");
            Assert.AreEqual(DateTimeKind.Unspecified, created.EffectiveDate.Kind);
            Assert.AreSame(created, repository.Added,
                "The instance handed to the repository should be the one returned.");
            Assert.AreEqual(1, repository.AddCalls);
            Assert.AreEqual(1, repository.SaveCalls);
        }

        [TestMethod]
        public void Create_MissingOrMalformedInput_Returns_Null()
        {
            // Arrange
            var storedEmployee = new Employee { EmployeeId = "Stored_Id" };
            var request = new CreateCompensationRequest
            {
                Employee = new EmployeeReference { EmployeeId = storedEmployee.EmployeeId },
                Salary = 1000m,
                EffectiveDate = "2026-01-01",
            };
            var requestWithoutSalary = new CreateCompensationRequest
            {
                Employee = new EmployeeReference { EmployeeId = storedEmployee.EmployeeId },
                EffectiveDate = "2026-01-01",
            };
            var requestWithoutDate = new CreateCompensationRequest
            {
                Employee = new EmployeeReference { EmployeeId = storedEmployee.EmployeeId },
                Salary = 1000m,
            };
            var requestWithBadDate = new CreateCompensationRequest
            {
                Employee = new EmployeeReference { EmployeeId = storedEmployee.EmployeeId },
                Salary = 1000m,
                EffectiveDate = "2026-01-01T10:30:00Z",
            };
            var repository = new FakeCompensationRepository();
            var service = NewService(repository);

            // Execute
            var forNullEmployee = service.Create(null, request);
            var forNullRequest = service.Create(storedEmployee, null);
            var forMissingSalary = service.Create(storedEmployee, requestWithoutSalary);
            var forMissingDate = service.Create(storedEmployee, requestWithoutDate);
            var forBadDate = service.Create(storedEmployee, requestWithBadDate);

            // Assert
            Assert.IsNull(forNullEmployee);
            Assert.IsNull(forNullRequest);
            Assert.IsNull(forMissingSalary);
            Assert.IsNull(forMissingDate);
            Assert.IsNull(forBadDate);
            Assert.AreEqual(0, repository.AddCalls, "Nothing should reach the repository.");
            Assert.AreEqual(0, repository.SaveCalls);
        }

        [TestMethod]
        public void GetById_AsksTheRepositoryForTheClocksDay()
        {
            // Arrange
            var stored = new Compensation { EmployeeId = "Stored_Id", Salary = 1000m, EffectiveDate = Today };
            var repository = new FakeCompensationRepository { GetByIdResult = stored };
            var service = NewService(repository);

            // Execute
            var compensation = service.GetById("Stored_Id");

            // Assert
            Assert.AreSame(stored, compensation);
            Assert.AreEqual("Stored_Id", repository.GetByIdEmployeeId);
            Assert.AreEqual(Today, repository.GetByIdAsOf, "The default read asks about the clock's day.");
        }

        [TestMethod]
        public void GetById_AsOf_PassesTheCalendarDayThrough()
        {
            // Arrange
            var repository = new FakeCompensationRepository();
            var service = NewService(repository);
            var asOf = new DateTime(2026, 3, 15);

            // Execute
            service.GetById("Stored_Id", asOf);

            // Assert
            Assert.AreEqual(asOf, repository.GetByIdAsOf);
        }

        [TestMethod]
        public void GetById_EmptyId_Returns_Null()
        {
            // Arrange
            var repository = new FakeCompensationRepository();
            var service = NewService(repository);

            // Execute
            var forEmptyId = service.GetById("");
            var forNullId = service.GetById(null);
            var forEmptyIdAsOf = service.GetById("", Today);

            // Assert
            Assert.IsNull(forEmptyId);
            Assert.IsNull(forNullId);
            Assert.IsNull(forEmptyIdAsOf);
            Assert.AreEqual(0, repository.GetByIdCalls, "An empty id never reaches the repository.");
        }

        [TestMethod]
        public void Exists_PassesEmployeeIdAndCalendarDayThrough_AndSkipsEmptyId()
        {
            // Arrange
            var effectiveDate = new DateTime(2026, 1, 1);
            var repository = new FakeCompensationRepository { ExistsResult = true };
            var service = NewService(repository);

            // Execute
            var forStoredId = service.Exists("Stored_Id", effectiveDate);
            var forEmptyId = service.Exists("", effectiveDate);

            // Assert
            Assert.IsTrue(forStoredId);
            Assert.AreEqual("Stored_Id", repository.ExistsEmployeeId);
            Assert.AreEqual(effectiveDate, repository.ExistsEffectiveDate);
            Assert.IsFalse(forEmptyId, "An empty id never reaches the repository and is never a conflict.");
            Assert.AreEqual(1, repository.ExistsCalls);
        }

        private static CompensationService NewService(ICompensationRepository repository)
        {
            return new CompensationService(
                NullLogger<CompensationService>.Instance,
                repository,
                new FixedClock(Today));
        }

        private class FakeCompensationRepository : ICompensationRepository
        {
            public Compensation Added { get; private set; }
            public int AddCalls { get; private set; }
            public int SaveCalls { get; private set; }
            public int GetByIdCalls { get; private set; }
            public string GetByIdEmployeeId { get; private set; }
            public DateTime? GetByIdAsOf { get; private set; }
            public Compensation GetByIdResult { get; set; }
            public int ExistsCalls { get; private set; }
            public string ExistsEmployeeId { get; private set; }
            public DateTime? ExistsEffectiveDate { get; private set; }
            public bool ExistsResult { get; set; }

            public Compensation Add(Compensation compensation)
            {
                Added = compensation;
                AddCalls++;
                return compensation;
            }

            public Compensation GetById(string employeeId, DateTime asOf)
            {
                GetByIdCalls++;
                GetByIdEmployeeId = employeeId;
                GetByIdAsOf = asOf;
                return GetByIdResult;
            }

            public bool Exists(string employeeId, DateTime effectiveDate)
            {
                ExistsCalls++;
                ExistsEmployeeId = employeeId;
                ExistsEffectiveDate = effectiveDate;
                return ExistsResult;
            }

            public Task SaveAsync()
            {
                // The service calls SaveAsync().Wait(), so unlike the fake that was added for Task 1, this one has to
                // hand back a completed task rather than throwing an error.

                SaveCalls++;
                return Task.CompletedTask;
            }
        }
    }
}
