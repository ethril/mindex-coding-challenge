using System;
using System.Linq;
using System.Threading.Tasks;
using CodeChallenge.Data;
using CodeChallenge.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeChallenge.Repositories
{
    public class CompensationRepository : ICompensationRepository
    {
        private readonly EmployeeContext _employeeContext;
        private readonly ILogger<ICompensationRepository> _logger;

        public CompensationRepository(ILogger<ICompensationRepository> logger, EmployeeContext employeeContext)
        {
            _employeeContext = employeeContext;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Compensation Add(Compensation compensation)
        {
            _employeeContext.Compensations.Add(compensation);
            return compensation;
        }

        /// <inheritdoc/>
        public Compensation GetById(string employeeId, DateTime asOf)
        {
            // I added Include here because nothing else loads the employee in a fresh context. Without it, the
            // response wouldn't carry the employee. Also, an employee can have several rows, so I kept the ones that
            // have taken effect by the given day, ordered by effective date, and took the latest.

            return _employeeContext.Compensations
                .Include(compensation => compensation.Employee)
                .Where(compensation => compensation.EmployeeId == employeeId && compensation.EffectiveDate <= asOf)
                .OrderByDescending(compensation => compensation.EffectiveDate)
                .FirstOrDefault();
        }

        /// <inheritdoc/>
        public bool Exists(string employeeId, DateTime effectiveDate)
        {
            return _employeeContext.Compensations
                .Any(compensation =>
                    compensation.EmployeeId == employeeId && compensation.EffectiveDate == effectiveDate);
        }

        /// <inheritdoc/>
        public Task SaveAsync()
        {
            return _employeeContext.SaveChangesAsync();
        }
    }
}
