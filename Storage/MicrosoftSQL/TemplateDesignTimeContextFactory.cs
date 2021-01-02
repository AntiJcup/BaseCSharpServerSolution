using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BaseApi.Storage
{
    public abstract class TemplateDesignTimeContextFactory<TDBContext> : IDesignTimeDbContextFactory<TDBContext>
            where TDBContext : TemplateMicrosoftSQLDbContext
    {
        public TDBContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var builder = new DbContextOptionsBuilder<TDBContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            connectionString = connectionString.Replace("<UID>", Environment.GetEnvironmentVariable("SQL_UID"));
            connectionString = connectionString.Replace("<PWD>", Environment.GetEnvironmentVariable("SQL_PWD"));
            builder.UseSqlServer(connectionString);
            return Activator.CreateInstance(typeof(TDBContext), new object[] { builder.Options }) as TDBContext;
        }
    }
}