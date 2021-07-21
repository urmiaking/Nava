using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nava.Common.Utilities;
using Nava.Data;
using Nava.Data.Repositories;
using Nava.Entities.MongoDb;
using Nava.Services.DataInitializer;

namespace Nava.WebFramework.Configuration
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseHsts(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (!env.IsDevelopment())
                //app.UseHsts();
        }

        public static IApplicationBuilder IntializeDatabase(this IApplicationBuilder app)
        {
            Assert.NotNull(app, nameof(app));

            //Use C# 8 using variables
            using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>(); //Service locator

            //Dos not use Migrations, just Create Database with latest changes
            //dbContext.Database.EnsureCreated();
            //Applies any pending migrations for the context to the database like (Update-Database)
            dbContext.Database.Migrate();

            var dataInitializers = scope.ServiceProvider.GetServices<IDataInitializer>();
            foreach (var dataInitializer in dataInitializers)
                dataInitializer.InitializeData();

            return app;
        }
    }
}