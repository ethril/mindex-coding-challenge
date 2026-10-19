using System;
using System.Net.Http;
using System.Threading.Tasks;

using CodeChallenge.Data;
using CodeChallenge.Utils;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeChallenge.Tests.Integration.Helpers
{
    public class ApiTestServer : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The day the test server treats as today.
        /// </summary>
        public static readonly DateTime Today = new(2026, 12, 31);

        private WebApplicationFactory<Program> applicationFactory;

        public ApiTestServer()
        {
            // Modified to use a private in-memory database that is seeded with the employee data at startup. Without
            // this, every test server in a test run shares the app's single in-memory "EmployeeDB" database, so data
            // changed by one test class would remain changed when the next class runs. This resulted in the existing
            // update test's PUT leaving John Lennon with 1 reachable report instead of 4 for any test class that ran
            // after it. I've also replaced the clock with a fixed one, so any read that depends on today gives the same
            // answer no matter when the tests run.

            var databaseName = Guid.NewGuid().ToString();

            applicationFactory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<EmployeeContext>>();
                    services.AddDbContext<EmployeeContext>(options => options.UseInMemoryDatabase(databaseName));

                    services.RemoveAll<IClock>();
                    services.AddSingleton<IClock>(new FixedClock(Today));
                }));
        }

        public HttpClient NewClient()
        {
            return applicationFactory.CreateClient();
        }


        public ValueTask DisposeAsync()
        {
            return ((IAsyncDisposable)applicationFactory).DisposeAsync();
        }

        public void Dispose()
        {
            ((IDisposable)applicationFactory).Dispose();
        }
    }
}
