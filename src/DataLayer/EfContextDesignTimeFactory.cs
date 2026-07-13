using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Fistix.TaskManager.DataLayer;

public class EfContextDesignTimeFactory : IDesignTimeDbContextFactory<EfContext>
{
    public EfContext CreateDbContext(string[] args)
    {
        var basePath = ResolveConfigBasePath();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("MainDb")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__MainDb")
            ?? "Host=localhost;Port=5433;Database=taskdb;Username=taskuser;Password=taskpass";

        var optionsBuilder = new DbContextOptionsBuilder<EfContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new EfContext(optionsBuilder.Options);
    }

    private static string ResolveConfigBasePath()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "WebApi"),
            Directory.GetCurrentDirectory()
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                return candidate;
        }

        return Directory.GetCurrentDirectory();
    }
}
