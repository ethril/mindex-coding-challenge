using System;
using CodeChallenge.Models;
using CodeChallenge.Models.Requests;

namespace CodeChallenge.Services
{
    public interface ICompensationService
    {
        /// <summary>
        /// Returns the employee's current compensation, using the latest effective date that is today or earlier.
        /// </summary>
        /// <param name="employeeId">The employee's unique identifier.</param>
        /// <returns>
        /// The compensation, with its employee loaded, or null if the ID is empty, no employee has that ID, or none of
        /// the employee's compensations has taken effect yet.
        /// </returns>
        Compensation GetById(string employeeId);

        /// <summary>
        /// Gets the compensation in effect for an employee on a given day, which is the one with the latest effective
        /// date on or before it.
        /// </summary>
        /// <param name="employeeId">The employee's unique identifier.</param>
        /// <param name="asOf">The day to ask about.</param>
        /// <returns>
        /// The compensation, with its employee loaded, or null if the ID is empty, no employee has that ID, or none of
        /// the employee's compensations had taken effect by that day.
        /// </returns>
        Compensation GetById(string employeeId, DateTime asOf);

        /// <summary>
        /// Reports whether the employee already has a compensation that takes effect on the given calendar day.
        /// </summary>
        /// <param name="employeeId">The employee's unique identifier.</param>
        /// <param name="effectiveDate">The day to check, at midnight.</param>
        /// <returns>True if one exists, and false if not, including when the ID is empty.</returns>
        bool Exists(string employeeId, DateTime effectiveDate);

        /// <summary>
        /// Stores a compensation for an employee on the day named by the request.
        /// </summary>
        /// <param name="employee">The stored employee the compensation belongs to.</param>
        /// <param name="request">The salary and effective date to store.</param>
        /// <returns>
        /// The stored compensation, or null if either argument is null, the request has no salary, or its effective
        /// date is not a YYYY-MM-DD date.
        /// </returns>
        Compensation Create(Employee employee, CreateCompensationRequest request);
    }
}
