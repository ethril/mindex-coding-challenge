using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

using CodeChallenge.Models;

using CodeChallenge.Tests.Integration.Extensions;
using CodeChallenge.Tests.Integration.Helpers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeChallenge.Tests.Integration
{
    [TestClass]
    public class ReportingStructureControllerTests
    {
        private const string JohnLennonEmployeeId = "16a596ae-edd3-4847-99fe-c4518e82c86f";
        private const string RingoStarrEmployeeId = "03aa1462-ffa9-4978-901b-7c001562cf6f";
        private const string PaulMcCartneyEmployeeId = "b7839309-3348-463b-a7e3-5de1c168beb3";
        private const string PeteBestEmployeeId = "62c1084e-6e34-4630-93fd-9153afb65309";
        private const string GeorgeHarrisonEmployeeId = "c0c2293d-16bd-4603-8e08-638a9d18b22c";

        private static HttpClient _httpClient;
        private static ApiTestServer _testServer;

        [ClassInitialize]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Style", "IDE0060:Remove unused parameter",
            Justification = "MSTest requires [ClassInitialize] methods to take a TestContext parameter.")]
        public static void InitializeClass(TestContext context)
        {
            _testServer = new ApiTestServer();
            _httpClient = _testServer.NewClient();
        }

        [ClassCleanup]
        public static void CleanUpTest()
        {
            _httpClient.Dispose();
            _testServer.Dispose();
        }

        [TestMethod]
        public void GetReportingStructureById_Returns_Ok()
        {
            // Arrange
            var employeeId = JohnLennonEmployeeId;
            var expectedFirstName = "John";
            var expectedLastName = "Lennon";
            var expectedCount = 4;

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/reportingStructure/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var reportingStructure = response.DeserializeContent<ReportingStructure>();
            Assert.AreEqual(expectedFirstName, reportingStructure.Employee.FirstName);
            Assert.AreEqual(expectedLastName, reportingStructure.Employee.LastName);
            Assert.AreEqual(expectedCount, reportingStructure.NumberOfReports);
        }

        [TestMethod]
        public void GetReportingStructureById_DirectReportsOnly_Returns_DirectReportCount()
        {
            // Arrange
            var employeeId = RingoStarrEmployeeId;
            var expectedFirstName = "Ringo";
            var expectedLastName = "Starr";
            var expectedCount = 2;

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/reportingStructure/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var reportingStructure = response.DeserializeContent<ReportingStructure>();
            Assert.AreEqual(expectedFirstName, reportingStructure.Employee.FirstName);
            Assert.AreEqual(expectedLastName, reportingStructure.Employee.LastName);
            Assert.AreEqual(expectedCount, reportingStructure.NumberOfReports);
        }

        [TestMethod]
        public void GetReportingStructureById_NoReports_Returns_Zero()
        {
            // Arrange
            var employeeId = PaulMcCartneyEmployeeId;
            var expectedFirstName = "Paul";
            var expectedLastName = "McCartney";
            var expectedCount = 0;

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/reportingStructure/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var reportingStructure = response.DeserializeContent<ReportingStructure>();
            Assert.AreEqual(expectedFirstName, reportingStructure.Employee.FirstName);
            Assert.AreEqual(expectedLastName, reportingStructure.Employee.LastName);
            Assert.AreEqual(expectedCount, reportingStructure.NumberOfReports);
        }

        [TestMethod]
        public void GetReportingStructureById_Returns_NotFound()
        {
            // Arrange
            var employeeId = "Invalid_Id";

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/reportingStructure/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public void GetReportingStructureById_Returns_FullReportingHierarchy()
        {
            // Arrange
            var employeeId = JohnLennonEmployeeId;
            var expectedJohnLennonDirectReportIds = new[] { PaulMcCartneyEmployeeId, RingoStarrEmployeeId };
            var expectedRingoStarrDirectReportIds = new[] { PeteBestEmployeeId, GeorgeHarrisonEmployeeId };

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/reportingStructure/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var johnLennon = response.DeserializeContent<ReportingStructure>().Employee;
            Assert.AreEqual(JohnLennonEmployeeId, johnLennon.EmployeeId);
            CollectionAssert.AreEquivalent(expectedJohnLennonDirectReportIds, GetDirectReportIds(johnLennon));

            var paulMcCartney = johnLennon.DirectReports.Single(e => e.EmployeeId == PaulMcCartneyEmployeeId);
            Assert.AreEqual(0, GetDirectReportIds(paulMcCartney).Count, "Paul McCartney should have no reports.");

            var ringoStarr = johnLennon.DirectReports.Single(e => e.EmployeeId == RingoStarrEmployeeId);
            CollectionAssert.AreEquivalent(expectedRingoStarrDirectReportIds, GetDirectReportIds(ringoStarr));

            var peteBest = ringoStarr.DirectReports.Single(e => e.EmployeeId == PeteBestEmployeeId);
            Assert.AreEqual(0, GetDirectReportIds(peteBest).Count, "Pete Best should have no reports.");

            var georgeHarrison = ringoStarr.DirectReports.Single(e => e.EmployeeId == GeorgeHarrisonEmployeeId);
            Assert.AreEqual(0, GetDirectReportIds(georgeHarrison).Count, "George Harrison should have no reports.");
        }

        private static List<string> GetDirectReportIds(Employee employee)
        {
            Assert.IsNotNull(
                employee.DirectReports,
                $"Direct reports for {employee.FirstName} {employee.LastName} were not loaded.");

            return employee.DirectReports.Select(e => e.EmployeeId).ToList();
        }
    }
}
