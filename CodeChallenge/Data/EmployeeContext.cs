using CodeChallenge.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeChallenge.Data
{
    public class EmployeeContext : DbContext
    {
        /// <summary>
        /// The name of the shadow property on Employee that holds the ID of the employee's manager.
        /// </summary>
        public const string ManagerIdProperty = "ManagerId";

        public EmployeeContext(DbContextOptions<EmployeeContext> options) : base(options)
        {

        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Compensation> Compensations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // A compensation is identified by the employee it belongs to and the date it takes effect, so I made that
            // pair the key, and the employee's ID doubles as the foreign key to the Employee row. I considered giving
            // Compensation a surrogate key instead, but it would have added a field that then needs hiding from the
            // JSON, and it would have allowed two rows on the same day for one employee. With the pair as the key, one
            // compensation per employee per effective date becomes a rule of the store rather than a convention in the
            // service, and the read never has to break a tie when it picks the latest row, because no two rows for an
            // employee can share a date. EmployeeId is part of the primary key, so it can never be null, and the
            // relationship to Employee is required. The IsRequired call is there to make that explicit to a reader. The
            // default delete behavior for a required relationship is Cascade, which means that if an Employee were
            // removed in a context that also had its compensations loaded, EF would delete them too. That matters
            // because PUT /api/employee/{id} replaces an employee by deleting the row and inserting a new one with the
            // same ID, and the compensations should survive that and attach to the new row. So I set ClientNoAction,
            // which tells EF to leave compensations alone when an employee is removed. If an employee delete endpoint
            // is ever added, this setting would need to be revisited, either by deleting the compensations along with
            // the employee or by refusing the delete while any exist.

            modelBuilder.Entity<Compensation>(entity =>
            {
                entity.HasKey(compensation => new { compensation.EmployeeId, compensation.EffectiveDate });
                entity.HasOne(compensation => compensation.Employee)
                    .WithMany()
                    .HasForeignKey(compensation => compensation.EmployeeId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.ClientNoAction);
            });

            // Employee.DirectReports has no inverse navigation and no foreign key property, so by convention EF adds a
            // shadow foreign key to Employee and names it EmployeeId1. EmployeeRepository now queries that column to
            // load a reporting hierarchy one level at a time, so I configured the relationship here to give the column
            // a name that says what it holds, rather than have the repository depend on a generated one. Nothing else
            // about the relationship changes. ManagerId is still a shadow property, so it never appears in the JSON,
            // and the relationship is still optional, because the employee at the top of a hierarchy has no manager,
            // which also keeps the ClientSetNull delete behavior the convention gave it.

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasMany(employee => employee.DirectReports)
                    .WithOne()
                    .HasForeignKey(ManagerIdProperty);
            });
        }
    }
}
