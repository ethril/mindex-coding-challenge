using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

using CodeChallenge.Models;
using CodeChallenge.Models.Requests;

using CodeChallenge.Tests.Integration.Extensions;
using CodeChallenge.Tests.Integration.Helpers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeChallenge.Tests.Integration
{
    [TestClass]
    public class CompensationControllerTests
    {
        private const string JohnLennonEmployeeId = "16a596ae-edd3-4847-99fe-c4518e82c86f";
        private const string PaulMcCartneyEmployeeId = "b7839309-3348-463b-a7e3-5de1c168beb3";
        private const string RingoStarrEmployeeId = "03aa1462-ffa9-4978-901b-7c001562cf6f";
        private const string PeteBestEmployeeId = "62c1084e-6e34-4630-93fd-9153afb65309";
        private const string GeorgeHarrisonEmployeeId = "c0c2293d-16bd-4603-8e08-638a9d18b22c";

        // The tests in this class share one server and one database, so every test that stores a compensation uses its
        // own employee, either a different seed employee or one it creates first. That way the order MSTest runs them
        // in never matters. The two values below, ValidSalary and ValidEffectiveDate, are deliberately valid. The tests
        // that target the 404 and 409 branches send them in the request body so the request passes validation.
        // Otherwise, a 400 would come back first and hide the branch under test. The server also pins today to
        // ApiTestServer.Today, 2026-12-31, so every 2026 date in this class has already taken effect and the 2030
        // dates stand for raises that have not.
        private const decimal ValidSalary = 1000m;
        private const string ValidEffectiveDate = "2026-01-01";

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
        public void CreateCompensation_Returns_Created()
        {
            // Arrange
            var effectiveDate = new DateTime(2026, 1, 1);
            var request = new CreateCompensationRequest
            {
                Employee = new EmployeeReference { EmployeeId = JohnLennonEmployeeId },
                Salary = 125000m,
                EffectiveDate = "2026-01-01",
            };
            var expectedFirstName = "John";
            var expectedLastName = "Lennon";
            var expectedProperties = new[] { "employee", "salary", "effectiveDate" };
            var requestContent = new JsonSerialization().ToJson(request);

            // Execute
            var postRequestTask = _httpClient.PostAsync("api/compensation",
                new StringContent(requestContent, Encoding.UTF8, "application/json"));
            var response = postRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            StringAssert.EndsWith(response.Headers.Location.ToString(),
                $"api/compensation/{JohnLennonEmployeeId}?asOf=2026-01-01");

            var body = ParseBody(response);
            CollectionAssert.AreEquivalent(expectedProperties,
                body.Properties().Select(property => property.Name).ToList());

            var newCompensation = response.DeserializeContent<Compensation>();
            Assert.AreEqual(JohnLennonEmployeeId, newCompensation.Employee.EmployeeId);
            Assert.AreEqual(expectedFirstName, newCompensation.Employee.FirstName);
            Assert.AreEqual(expectedLastName, newCompensation.Employee.LastName);
            Assert.IsNull(newCompensation.Employee.DirectReports,
                "Reports are not loaded, the same as GET /api/employee/{id}.");
            Assert.AreEqual(request.Salary, newCompensation.Salary);
            Assert.AreEqual(effectiveDate, newCompensation.EffectiveDate);

            var locationResponse = _httpClient.GetAsync(response.Headers.Location).Result;
            Assert.AreEqual(HttpStatusCode.OK, locationResponse.StatusCode);
            var storedCompensation = locationResponse.DeserializeContent<Compensation>();
            Assert.AreEqual(JohnLennonEmployeeId, storedCompensation.Employee.EmployeeId,
                "Following the Location should return the compensation as stored.");
            Assert.AreEqual(request.Salary, storedCompensation.Salary);
            Assert.AreEqual(effectiveDate, storedCompensation.EffectiveDate);
        }

        [TestMethod]
        public void CreateCompensation_DoesNotModifyEmployee()
        {
            // Arrange
            var request = new
            {
                employee = new
                {
                    employeeId = GeorgeHarrisonEmployeeId,
                    firstName = "Changed",
                    position = "Changed",
                },
                salary = ValidSalary,
                effectiveDate = ValidEffectiveDate,
            };
            var expectedFirstName = "George";
            var expectedPosition = "Developer III";
            var requestContent = new JsonSerialization().ToJson(request);

            // Execute
            var postRequestTask = _httpClient.PostAsync("api/compensation",
                new StringContent(requestContent, Encoding.UTF8, "application/json"));
            var postResponse = postRequestTask.Result;
            var getRequestTask = _httpClient.GetAsync($"api/employee/{GeorgeHarrisonEmployeeId}");
            var getResponse = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);
            var newCompensation = postResponse.DeserializeContent<Compensation>();
            Assert.AreEqual(expectedFirstName, newCompensation.Employee.FirstName);
            Assert.AreEqual(expectedPosition, newCompensation.Employee.Position);

            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            var employee = getResponse.DeserializeContent<Employee>();
            Assert.AreEqual(expectedFirstName, employee.FirstName);
            Assert.AreEqual(expectedPosition, employee.Position);
        }

        [TestMethod]
        public void CreateCompensation_UnknownEmployee_Returns_NotFound()
        {
            // Arrange
            var employeeId = "Invalid_Id";

            // Execute
            var postResponse = PostCompensation(employeeId, ValidSalary, ValidEffectiveDate);
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var getResponse = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, postResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode,
                "Nothing should be stored for an unknown employee.");
        }

        [TestMethod]
        public void CreateCompensation_SameEffectiveDate_Returns_Conflict()
        {
            // Arrange
            var employeeId = RingoStarrEmployeeId;
            var firstSalary = 1000m;
            var secondSalary = 2000m;
            var firstResponse = PostCompensation(employeeId, firstSalary, ValidEffectiveDate);
            Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);

            // Execute
            var secondResponse = PostCompensation(employeeId, secondSalary, ValidEffectiveDate);
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var getResponse = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.Conflict, secondResponse.StatusCode);

            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            var compensation = getResponse.DeserializeContent<Compensation>();
            Assert.AreEqual(firstSalary, compensation.Salary, "The first compensation should be unchanged.");
        }

        [TestMethod]
        public void CreateCompensation_LaterEffectiveDate_Returns_Created_And_GetReturnsIt()
        {
            // Arrange
            var employeeId = CreateEmployee("Tommy", "Moore");
            var firstDate = new DateTime(2026, 1, 1);
            var laterDate = new DateTime(2026, 7, 1);
            var firstResponse = PostCompensation(employeeId, 1000m, firstDate);
            Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);

            // Execute
            var secondResponse = PostCompensation(employeeId, 2000m, laterDate);
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var getResponse = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, secondResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            var compensation = getResponse.DeserializeContent<Compensation>();
            Assert.AreEqual(2000m, compensation.Salary);
            Assert.AreEqual(laterDate, compensation.EffectiveDate);
        }

        [TestMethod]
        public void CreateCompensation_EarlierEffectiveDate_Returns_Created_And_GetStillReturnsLatest()
        {
            // Arrange
            var employeeId = CreateEmployee("Andy", "White");
            var earlierDate = new DateTime(2025, 1, 1);
            var laterDate = new DateTime(2026, 1, 1);
            var firstResponse = PostCompensation(employeeId, 2000m, laterDate);
            Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);

            // Execute
            var secondResponse = PostCompensation(employeeId, 1000m, earlierDate);
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var getResponse = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, secondResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            var compensation = getResponse.DeserializeContent<Compensation>();
            Assert.AreEqual(2000m, compensation.Salary);
            Assert.AreEqual(laterDate, compensation.EffectiveDate);

            var locationResponse = _httpClient.GetAsync(secondResponse.Headers.Location).Result;
            Assert.AreEqual(HttpStatusCode.OK, locationResponse.StatusCode);
            var created = locationResponse.DeserializeContent<Compensation>();
            Assert.AreEqual(1000m, created.Salary, "Following the Location should return the row just created.");
            Assert.AreEqual(earlierDate, created.EffectiveDate);
        }

        [TestMethod]
        public void CreateCompensation_EffectiveDateNotACalendarDay_Returns_BadRequest()
        {
            // Arrange
            var employeeId = CreateEmployee("Ringo", "Stand-in");
            var notCalendarDays = new[]
            {
                "2026-02-01T10:30:00",
                "2026-02-01T10:30:00Z",
                "2026-02-01T00:00:00+09:00",
                "2026-2-1",
                "02/01/2026",
                "2026-02-30",
                " 2026-02-01",
            };
            var expectedMessage = "effectiveDate must be a date in the form YYYY-MM-DD.";

            foreach (var notCalendarDay in notCalendarDays)
            {
                // Execute
                var response = PostCompensation(employeeId, ValidSalary, notCalendarDay);

                // Assert
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
                    $"'{notCalendarDay}' should be rejected.");
                var errors = ValidationErrors(response);
                CollectionAssert.AreEquivalent(
                    new[] { "EffectiveDate" }, errors.Properties().Select(p => p.Name).ToList());
                Assert.AreEqual(expectedMessage, errors["EffectiveDate"].Single().Value<string>());
            }

            var getResponse = _httpClient.GetAsync($"api/compensation/{employeeId}?asOf=2026-02-01").Result;
            Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode, "Nothing should have been stored.");
        }

        [TestMethod]
        public void CreateCompensation_SameEffectiveDate_DifferentEmployees_Returns_Created()
        {
            // Arrange
            var firstEmployeeId = CreateEmployee("Chas", "Newby");
            var secondEmployeeId = CreateEmployee("Norman", "Chapman");

            // Execute
            var firstResponse = PostCompensation(firstEmployeeId, ValidSalary, ValidEffectiveDate);
            var secondResponse = PostCompensation(secondEmployeeId, ValidSalary, ValidEffectiveDate);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.Created, secondResponse.StatusCode);
        }

        [TestMethod]
        public void CreateCompensation_MissingEmployee_Returns_BadRequest()
        {
            // Arrange
            var request = new CreateCompensationRequest
            {
                Employee = null,
                Salary = ValidSalary,
                EffectiveDate = ValidEffectiveDate,
            };
            var requestContent = new JsonSerialization().ToJson(request);

            // Execute
            var postRequestTask = _httpClient.PostAsync("api/compensation",
                new StringContent(requestContent, Encoding.UTF8, "application/json"));
            var response = postRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            CollectionAssert.AreEquivalent(new[] { "Employee" }, ValidationErrorKeys(response));
        }

        [TestMethod]
        public void CreateCompensation_EmptyEmployeeId_Returns_BadRequest()
        {
            // Arrange
            var request = new CreateCompensationRequest
            {
                Employee = new EmployeeReference { EmployeeId = "" },
                Salary = ValidSalary,
                EffectiveDate = ValidEffectiveDate,
            };
            var expectedMessage = "employee.employeeId is required.";
            var requestContent = new JsonSerialization().ToJson(request);

            // Execute
            var postRequestTask = _httpClient.PostAsync("api/compensation",
                new StringContent(requestContent, Encoding.UTF8, "application/json"));
            var response = postRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var errors = ValidationErrors(response);
            CollectionAssert.AreEquivalent(
                new[] { "Employee.EmployeeId" }, errors.Properties().Select(p => p.Name).ToList());
            Assert.AreEqual(expectedMessage, errors["Employee.EmployeeId"].Single().Value<string>());
        }

        [TestMethod]
        public void CreateCompensation_NegativeSalary_Returns_BadRequest()
        {
            // Arrange
            var employeeId = PeteBestEmployeeId;
            var salary = -1m;

            // Execute
            var response = PostCompensation(employeeId, salary, ValidEffectiveDate);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            CollectionAssert.AreEquivalent(new[] { "Salary" }, ValidationErrorKeys(response));
        }

        [TestMethod]
        public void CreateCompensation_MissingEffectiveDate_Returns_BadRequest()
        {
            // Arrange
            var request = new
            {
                employee = new { employeeId = PeteBestEmployeeId },
                salary = ValidSalary,
            };
            var requestContent = new JsonSerialization().ToJson(request);

            // Execute
            var postRequestTask = _httpClient.PostAsync("api/compensation",
                new StringContent(requestContent, Encoding.UTF8, "application/json"));
            var response = postRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            CollectionAssert.AreEquivalent(new[] { "EffectiveDate" }, ValidationErrorKeys(response));
        }

        [TestMethod]
        public void CreateCompensation_MalformedSalary_Returns_BadRequest()
        {
            // Arrange
            var request = new
            {
                employee = new { employeeId = PeteBestEmployeeId },
                salary = "Invalid_Salary",
                effectiveDate = ValidEffectiveDate,
            };
            var requestContent = new JsonSerialization().ToJson(request);

            // Execute
            var postRequestTask = _httpClient.PostAsync("api/compensation",
                new StringContent(requestContent, Encoding.UTF8, "application/json"));
            var response = postRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            CollectionAssert.AreEquivalent(new[] { "$.salary" }, ValidationErrorKeys(response));
        }

        [TestMethod]
        public void CreateCompensation_MissingSalary_Returns_BadRequest()
        {
            // Arrange
            var request = new
            {
                employee = new { employeeId = PeteBestEmployeeId },
                effectiveDate = ValidEffectiveDate,
            };
            var requestContent = new JsonSerialization().ToJson(request);

            // Execute
            var postRequestTask = _httpClient.PostAsync("api/compensation",
                new StringContent(requestContent, Encoding.UTF8, "application/json"));
            var response = postRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            CollectionAssert.AreEquivalent(new[] { "Salary" }, ValidationErrorKeys(response));
        }

        [TestMethod]
        public void CreateCompensation_ZeroSalary_Returns_Created()
        {
            // Arrange
            var employeeId = CreateEmployee("Jimmie", "Nicol");

            // Execute
            var postResponse = PostCompensation(employeeId, 0m, ValidEffectiveDate);
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var getResponse = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.AreEqual(0m, getResponse.DeserializeContent<Compensation>().Salary);
        }

        [TestMethod]
        public void GetCompensationById_Returns_Ok()
        {
            // Arrange
            var employeeId = PaulMcCartneyEmployeeId;
            var salary = 85000.50m; // $85,000.50
            var effectiveDate = new DateTime(2026, 3, 15);
            var expectedFirstName = "Paul";
            var expectedLastName = "McCartney";
            var postResponse = PostCompensation(employeeId, salary, effectiveDate);
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var compensation = response.DeserializeContent<Compensation>();
            Assert.AreEqual(employeeId, compensation.Employee.EmployeeId);
            Assert.AreEqual(expectedFirstName, compensation.Employee.FirstName);
            Assert.AreEqual(expectedLastName, compensation.Employee.LastName);
            Assert.IsNull(compensation.Employee.DirectReports);
            Assert.AreEqual(salary, compensation.Salary);
            Assert.AreEqual(effectiveDate, compensation.EffectiveDate);
            Assert.AreEqual(DateTimeKind.Unspecified, compensation.EffectiveDate.Kind,
                "A calendar day comes back as midnight with no zone.");
        }

        [TestMethod]
        public void GetCompensationById_NoCompensation_Returns_NotFound()
        {
            // Arrange
            var employeeId = CreateEmployee("Stuart", "Sutcliffe");

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            StringAssert.Contains(ParseBody(response)["detail"].Value<string>(),
                $"Employee '{employeeId}' has no compensation in effect today.",
                "The employee exists, so the 404 should say that nothing is in effect.");
        }

        [TestMethod]
        public void GetCompensationById_UnknownEmployee_Returns_NotFound()
        {
            // Arrange
            var employeeId = "Invalid_Id";

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.IsNull(ParseBody(response)["detail"], "An unknown employee gets the plain 404.");
        }

        [TestMethod]
        public void GetCompensationById_AfterEmployeeReplaced_Returns_ReplacementEmployee()
        {
            // Arrange
            var employeeId = CreateEmployee("Billy", "Preston");
            var postResponse = PostCompensation(employeeId, ValidSalary, ValidEffectiveDate);
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);

            var replacementEmployee = new Employee
            {
                EmployeeId = employeeId,
                FirstName = "Billy",
                LastName = "Preston",
                Position = "Keyboardist",
                Department = "Music",
            };
            var requestContent = new JsonSerialization().ToJson(replacementEmployee);
            var putRequestTask = _httpClient.PutAsync($"api/employee/{employeeId}",
                new StringContent(requestContent, Encoding.UTF8, "application/json"));
            var putResponse = putRequestTask.Result;
            Assert.AreEqual(HttpStatusCode.OK, putResponse.StatusCode);

            // Execute
            var getRequestTask = _httpClient.GetAsync($"api/compensation/{employeeId}");
            var response = getRequestTask.Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var compensation = response.DeserializeContent<Compensation>();
            Assert.AreEqual(employeeId, compensation.Employee.EmployeeId);
            Assert.AreEqual(replacementEmployee.Position, compensation.Employee.Position);
            Assert.AreEqual(ValidSalary, compensation.Salary);
        }

        [TestMethod]
        public void GetCompensationById_FutureEffectiveDate_IsNotReturnedUntilItTakesEffect()
        {
            // Arrange
            var employeeId = CreateEmployee("Klaus", "Voormann");
            var currentDate = new DateTime(2026, 1, 1);
            var futureDate = new DateTime(2030, 1, 1);
            Assert.AreEqual(HttpStatusCode.Created, PostCompensation(employeeId, 1000m, currentDate).StatusCode);
            Assert.AreEqual(HttpStatusCode.Created, PostCompensation(employeeId, 2000m, futureDate).StatusCode);

            // Execute
            var todayResponse = _httpClient.GetAsync($"api/compensation/{employeeId}").Result;
            var futureResponse = _httpClient.GetAsync($"api/compensation/{employeeId}?asOf=2030-01-01").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, todayResponse.StatusCode);
            var today = todayResponse.DeserializeContent<Compensation>();
            Assert.AreEqual(1000m, today.Salary, "The future raise should not be reported yet.");
            Assert.AreEqual(currentDate, today.EffectiveDate);

            Assert.AreEqual(HttpStatusCode.OK, futureResponse.StatusCode);
            var future = futureResponse.DeserializeContent<Compensation>();
            Assert.AreEqual(2000m, future.Salary);
            Assert.AreEqual(futureDate, future.EffectiveDate);
        }

        [TestMethod]
        public void GetCompensationById_OnlyFutureCompensation_Returns_NotFound_AndLocationFindsIt()
        {
            // Arrange
            var employeeId = CreateEmployee("Eric", "Clapton");
            var futureDate = new DateTime(2030, 6, 1);
            var postResponse = PostCompensation(employeeId, ValidSalary, futureDate);
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);

            // Execute
            var todayResponse = _httpClient.GetAsync($"api/compensation/{employeeId}").Result;
            var locationResponse = _httpClient.GetAsync(postResponse.Headers.Location).Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, todayResponse.StatusCode,
                "Nothing has taken effect yet, so there is no compensation today.");
            StringAssert.Contains(ParseBody(todayResponse)["detail"].Value<string>(),
                $"Employee '{employeeId}' has no compensation in effect today.",
                "The 404 should explain that the future-dated compensation has not taken effect.");
            Assert.AreEqual(HttpStatusCode.OK, locationResponse.StatusCode,
                "The Location carries the row's own date as asOf, so it finds the row just created.");
            Assert.AreEqual(futureDate, locationResponse.DeserializeContent<Compensation>().EffectiveDate);
        }

        [TestMethod]
        public void GetCompensationById_AsOf_ReturnsCompensationInEffectOnThatDay()
        {
            // Arrange
            var employeeId = CreateEmployee("Nicky", "Hopkins");
            var firstDate = new DateTime(2026, 1, 1);
            var secondDate = new DateTime(2026, 6, 1);
            Assert.AreEqual(HttpStatusCode.Created, PostCompensation(employeeId, 1000m, firstDate).StatusCode);
            Assert.AreEqual(HttpStatusCode.Created, PostCompensation(employeeId, 2000m, secondDate).StatusCode);

            // Execute
            var beforeFirst = _httpClient.GetAsync($"api/compensation/{employeeId}?asOf=2025-12-31").Result;
            var betweenTheTwo = _httpClient.GetAsync($"api/compensation/{employeeId}?asOf=2026-03-15").Result;
            var onTheSecondDay = _httpClient.GetAsync($"api/compensation/{employeeId}?asOf=2026-06-01").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, beforeFirst.StatusCode, "Nothing was in effect yet.");
            StringAssert.Contains(ParseBody(beforeFirst)["detail"].Value<string>(),
                $"Employee '{employeeId}' has no compensation in effect on 2025-12-31.");
            Assert.AreEqual(HttpStatusCode.OK, betweenTheTwo.StatusCode);
            Assert.AreEqual(1000m, betweenTheTwo.DeserializeContent<Compensation>().Salary);
            Assert.AreEqual(HttpStatusCode.OK, onTheSecondDay.StatusCode);
            Assert.AreEqual(2000m, onTheSecondDay.DeserializeContent<Compensation>().Salary,
                "A compensation is in effect from its own effective date onward.");
        }

        /// <summary>
        /// Verifies that the test server is using the pinned clock.
        /// </summary>
        [TestMethod]
        public void GetCompensationById_UsesTheTestServersPinnedDay()
        {
            // Arrange
            var employeeId = CreateEmployee("Jackie", "Lomax");
            var today = ApiTestServer.Today;
            var tomorrow = today.AddDays(1);
            Assert.AreEqual(HttpStatusCode.Created, PostCompensation(employeeId, 1000m, today).StatusCode);
            Assert.AreEqual(HttpStatusCode.Created, PostCompensation(employeeId, 2000m, tomorrow).StatusCode);

            // Execute
            var response = _httpClient.GetAsync($"api/compensation/{employeeId}").Result;
            var emptyAsOfResponse = _httpClient.GetAsync($"api/compensation/{employeeId}?asOf=").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var compensation = response.DeserializeContent<Compensation>();
            Assert.AreEqual(1000m, compensation.Salary, "The row dated today is in effect, and tomorrow's is not yet.");
            Assert.AreEqual(today, compensation.EffectiveDate);
            Assert.AreEqual(HttpStatusCode.OK, emptyAsOfResponse.StatusCode);
            Assert.AreEqual(today, emptyAsOfResponse.DeserializeContent<Compensation>().EffectiveDate,
                "An empty asOf means today.");
        }

        [TestMethod]
        public void GetCompensationById_AsOfNotACalendarDay_Returns_BadRequest()
        {
            // Arrange
            var notCalendarDays = new[] { "not-a-date", "2026-01-01T00:00:00", "2026-01-01T00:00:00Z", "2026-1-1" };
            var expectedMessage = "asOf must be a date in the form YYYY-MM-DD.";

            foreach (var notCalendarDay in notCalendarDays)
            {
                // Execute
                var response = _httpClient.GetAsync(
                    $"api/compensation/{PaulMcCartneyEmployeeId}?asOf={notCalendarDay}").Result;

                // Assert
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
                    $"'{notCalendarDay}' should be rejected.");
                var errors = ValidationErrors(response);
                CollectionAssert.AreEquivalent(new[] { "asOf" }, errors.Properties().Select(p => p.Name).ToList());
                Assert.AreEqual(expectedMessage, errors["asOf"].Single().Value<string>());
            }
        }

        private static HttpResponseMessage PostCompensation(string employeeId, decimal salary, string effectiveDate)
        {
            var request = new CreateCompensationRequest
            {
                Employee = new EmployeeReference { EmployeeId = employeeId },
                Salary = salary,
                EffectiveDate = effectiveDate,
            };
            var requestContent = new JsonSerialization().ToJson(request);

            return _httpClient.PostAsync("api/compensation",
                new StringContent(requestContent, Encoding.UTF8, "application/json")).Result;
        }

        private static HttpResponseMessage PostCompensation(string employeeId, decimal salary, DateTime effectiveDate)
        {
            return PostCompensation(
                employeeId, salary, effectiveDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        private static string CreateEmployee(string firstName, string lastName)
        {
            var employee = new Employee
            {
                FirstName = firstName,
                LastName = lastName,
                Position = "Developer I",
                Department = "Engineering",
            };
            var requestContent = new JsonSerialization().ToJson(employee);

            var response = _httpClient.PostAsync("api/employee",
                new StringContent(requestContent, Encoding.UTF8, "application/json")).Result;
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode,
                "Creating the employee the test needs failed.");

            return response.DeserializeContent<Employee>().EmployeeId;
        }

        /// <summary>
        /// Reads the errors object out of the problem-details body that a 400 carries.
        /// </summary>
        private static JObject ValidationErrors(HttpResponseMessage response)
        {
            var body = ParseBody(response);
            var errors = body["errors"] as JObject;
            Assert.IsNotNull(errors, "A 400 from validation should carry an errors object.");

            return errors;
        }

        private static string[] ValidationErrorKeys(HttpResponseMessage response)
        {
            return ValidationErrors(response).Properties().Select(p => p.Name).ToArray();
        }

        /// <summary>
        /// Parses a response body as JSON with Newtonsoft's date recognition turned off, so a date comes back as the
        /// exact text that was on the wire rather than a DateTime formatted for the current culture.
        /// </summary>
        private static JObject ParseBody(HttpResponseMessage response)
        {
            using var reader = new JsonTextReader(new StringReader(response.Content.ReadAsStringAsync().Result))
            {
                DateParseHandling = DateParseHandling.None,
            };

            return JObject.Load(reader);
        }
    }
}
