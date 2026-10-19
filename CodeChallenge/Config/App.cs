using System;

using CodeChallenge.Data;
using CodeChallenge.Repositories;
using CodeChallenge.Services;
using CodeChallenge.Utils;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CodeChallenge.Config
{
    public class App
    {
        public WebApplication Configure(string[] args)
        {
            args ??= Array.Empty<string>();

            var builder = WebApplication.CreateBuilder(args);

            builder.UseEmployeeDB();
            
            AddServices(builder.Services);

            var app = builder.Build();

            var env = builder.Environment;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                SeedEmployeeDB(app.Services);
            }

            app.UseAuthorization();

            app.MapControllers();

            return app;
        }

        private void AddServices(IServiceCollection services)
        {

            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IReportingStructureService, ReportingStructureService>();
            services.AddScoped<ICompensationService, CompensationService>();
            services.AddScoped<ICompensationRepository, CompensationRepository>();
            services.AddSingleton<IClock, SystemClock>();

            services.AddControllers();
        }

        private void SeedEmployeeDB(IServiceProvider services)
        {
            // The starter built its own EmployeeContext here, hard-coded to the "EmployeeDB" database, so it could only
            // seed that one database. I've opted to resolve the context from dependency injection instead, so the seed
            // data goes wherever the registered context points, allowing a unique database for each integration test
            // server. Without this, every test server shared the same seeded database.

            using var scope = services.CreateScope();
            new EmployeeDataSeeder(
                scope.ServiceProvider.GetRequiredService<EmployeeContext>()
            ).Seed().Wait();
        }
    }
}