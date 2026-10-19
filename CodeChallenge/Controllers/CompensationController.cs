using CodeChallenge.Models.Requests;
using CodeChallenge.Services;
using CodeChallenge.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CodeChallenge.Controllers
{
    [ApiController]
    [Route("api/compensation")]
    public class CompensationController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IEmployeeService _employeeService;
        private readonly ICompensationService _compensationService;

        public CompensationController(
            ILogger<CompensationController> logger,
            IEmployeeService employeeService,
            ICompensationService compensationService)
        {
            _logger = logger;
            _employeeService = employeeService;
            _compensationService = compensationService;
        }

        [HttpPost]
        public IActionResult CreateCompensation([FromBody] CreateCompensationRequest request)
        {
            // The request model is validated before this method runs. The attributes on the request model and
            // [ApiController] turn a missing or malformed body, a missing employee.employeeId, a missing or negative
            // salary or an effectiveDate that is missing or not a YYYY-MM-DD date into a 400, so no lookup occurs for
            // a bad request. The two checks that need the database stay here, next to the status codes they produce,
            // the same way ReplaceEmployee does its own 404 check, and the service only persists. An unknown employee
            // gets a 404 before the duplicate check runs, so a bad ID never reports a 409 conflict. Since one employee
            // can have many compensations, the duplicate check looks at the employee and the effective date together,
            // which make up the row's key. I run that check before the insert rather than catching the failure,
            // because the in-memory provider's duplicate-key error is not an EF exception and the failed save is not
            // rolled back.

            var employeeId = request.Employee.EmployeeId;
            _logger.LogDebug($"Received compensation create request for employee '{employeeId}'");

            var employee = _employeeService.GetById(employeeId);
            if (employee == null)
                return NotFound();

            if (_compensationService.Exists(employeeId, DateUtils.ParseCalendarDate(request.EffectiveDate)))
                return Conflict();

            var compensation = _compensationService.Create(employee, request);

            // A 201 response carries a Location header that a client can follow to fetch the thing it just created. The
            // GET URL identifies an employee rather than a row, and on its own it returns whichever compensation is in
            // effect today. So if the new row is back-dated behind a newer one, or future-dated and not yet in effect,
            // the plain URL would return a different compensation from the one this request created. To avoid that, I
            // add the new row's effective date to the URL as an asOf parameter. That URL always resolves to exactly the
            // row that was just created, whether it was back-dated, future-dated or current, because the row took
            // effect on that day and no two rows for an employee can share a date.

            var asOf = DateUtils.FormatCalendarDate(compensation.EffectiveDate);

            return CreatedAtRoute("getCompensationById", new { id = employeeId, asOf }, compensation);
        }

        [HttpGet("{id}", Name = "getCompensationById")]
        public IActionResult GetCompensationById(
            string id,
            [FromQuery, CalendarDate(ErrorMessage = "asOf must be a date in the form YYYY-MM-DD.")] string asOf)
        {
            // Without the asOf parameter, this returns the employee's compensation that is in effect today. Adding asOf
            // returns the one in effect on that day. An empty asOf binds as null and counts as missing. A value that is
            // not a YYYY-MM-DD date fails validation and is a 400 before this runs, so the parse below cannot fail.

            _logger.LogDebug($"Received compensation get request for employee '{id}'");

            var compensation = asOf == null
                ? _compensationService.GetById(id)
                : _compensationService.GetById(id, DateUtils.ParseCalendarDate(asOf));

            if (compensation == null)
            {
                // An unknown employee and an employee with nothing in effect are both a 404, since in both cases
                // there is no compensation to return. The two are easy to confuse, most of all right after a
                // future-dated POST, when the row exists but has not taken effect yet. So when the employee does
                // exist, I put a detail message in the 404 that says no compensation is in effect on that day and
                // points at asOf. An unknown employee keeps the plain 404 that the rest of this API returns.

                if (_employeeService.GetById(id) == null)
                    return NotFound();

                var day = asOf == null ? "today" : "on " + asOf;
                var detail = $"Employee '{id}' has no compensation in effect {day}. A compensation is in effect " +
                    "from its effectiveDate onward. Pass ?asOf=YYYY-MM-DD to read the one in effect on " +
                    "another day.";

                return Problem(statusCode: StatusCodes.Status404NotFound, detail: detail);
            }

            return Ok(compensation);
        }
    }
}
