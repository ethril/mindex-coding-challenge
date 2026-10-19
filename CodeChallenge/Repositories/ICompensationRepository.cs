using System;
using System.Threading.Tasks;
using CodeChallenge.Models;

namespace CodeChallenge.Repositories
{
    public interface ICompensationRepository
    {
        /// <summary>
        /// Gets the employee's compensation in effect on a given day.
        /// </summary>
        /// <param name="employeeId">The employee's unique identifier.</param>
        /// <param name="asOf">The day to look up.</param>
        /// <returns>The compensation, or null if none has taken effect by that day.</returns>
        Compensation GetById(string employeeId, DateTime asOf);

        /// <summary>
        /// Reports whether the employee already has a compensation that takes effect on the given day.
        /// </summary>
        /// <param name="employeeId">The employee's unique identifier.</param>
        /// <param name="effectiveDate">The day to check.</param>
        /// <returns>True if one exists, and false if not.</returns>
        bool Exists(string employeeId, DateTime effectiveDate);

        /// <summary>
        /// Stages a compensation to be stored by the next call to <see cref="SaveAsync"/>.
        /// </summary>
        /// <param name="compensation">The compensation to store.</param>
        /// <returns>The same compensation.</returns>
        Compensation Add(Compensation compensation);

        /// <summary>
        /// Writes every pending change in the context to the store.
        /// </summary>
        /// <returns>A task that completes once the changes are written.</returns>
        Task SaveAsync();
    }
}
