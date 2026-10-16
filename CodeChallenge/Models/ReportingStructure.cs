namespace CodeChallenge.Models
{
    public class ReportingStructure
    {
        /// <summary>
        /// The employee whose reporting structure this is, with <see cref="Employee.DirectReports"/> filled in at
        /// every level below them.
        /// </summary>
        public Employee Employee { get; set; }

        /// <summary>
        /// The total number of employees under <see cref="Employee"/> in the organizational hierarchy.
        /// </summary>
        public int NumberOfReports { get; set; }
    }
}
