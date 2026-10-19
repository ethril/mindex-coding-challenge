using System;
using CodeChallenge.Models;
using CodeChallenge.Models.Requests;
using CodeChallenge.Repositories;
using CodeChallenge.Utils;
using Microsoft.Extensions.Logging;

namespace CodeChallenge.Services
{
    public class CompensationService : ICompensationService
    {
        private readonly ICompensationRepository _compensationRepository;
        private readonly IClock _clock;
        private readonly ILogger<CompensationService> _logger;

        public CompensationService(
            ILogger<CompensationService> logger,
            ICompensationRepository compensationRepository,
            IClock clock)
        {
            _compensationRepository = compensationRepository;
            _clock = clock;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Compensation Create(Employee employee, CreateCompensationRequest request)
        {
            // The request model carries plain values and never an Employee object, so this is where the Compensation
            // entity gets built. I set its Employee property to the employee the controller already loaded from the
            // database, which is the instance EF is tracking for this request. Because the request never contained its
            // own copy of the employee, EF never sees two objects for the same row, which is the problem the request
            // model prevents. This also means the response shows the employee as stored rather than whatever a client
            // sent. I also set EmployeeId directly. EF would fill it in from the Employee property during Add, but that
            // property is part of the primary key, so I would rather the entity be complete before EF sees it than
            // depend on that happening. This also lets the unit test check the key without EF involved. Next, the
            // effectiveDate arrives as yyyy-MM-dd text that the CalendarDate attribute has already checked, and
            // DateUtils.TryParseCalendarDate turns it into midnight with no kind. The controller reads the same text
            // the same way before its duplicate check, so the stored value and the check cannot disagree, and a
            // second body naming the same day is a 409 rather than a failed save. If the text is not a date, which
            // cannot happen over HTTP, this returns null like the other missing values rather than throwing.

            if (employee == null || request?.Salary == null)
                return null;
            if (!DateUtils.TryParseCalendarDate(request.EffectiveDate, out var effectiveDate))
                return null;

            var compensation = new Compensation
            {
                EmployeeId = employee.EmployeeId,
                Employee = employee,
                Salary = request.Salary.Value,
                EffectiveDate = effectiveDate,
            };

            _compensationRepository.Add(compensation);
            _compensationRepository.SaveAsync().Wait();

            return compensation;
        }

        /// <inheritdoc/>
        public Compensation GetById(string employeeId)
        {
            // We need the current date to determine what compensation is in effect today. Instead of calling
            // DateTime.Today directly, I use the IClock that was injected into the service's constructor. In the
            // running app, that is SystemClock, which returns the server's local date, so the behavior is the same as
            // calling DateTime.Today. The indirection exists only for tests, which register a FixedClock in place of
            // SystemClock so today is a known, fixed date and future-dated cases return the same answer no matter when
            // the suite runs.

            return GetById(employeeId, _clock.Today);
        }

        /// <inheritdoc/>
        public Compensation GetById(string employeeId, DateTime asOf)
        {
            if (string.IsNullOrEmpty(employeeId))
                return null;

            return _compensationRepository.GetById(employeeId, asOf);
        }

        /// <inheritdoc/>
        public bool Exists(string employeeId, DateTime effectiveDate)
        {
            if (string.IsNullOrEmpty(employeeId))
                return false;

            return _compensationRepository.Exists(employeeId, effectiveDate);
        }
    }
}
