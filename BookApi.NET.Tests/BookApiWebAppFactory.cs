using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace BookApi.NET.Tests;

public class BookApiWebFactory : WebApplicationFactory<Program>
{
    public readonly String DatabaseName = $"book-api-test-db-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BookstoreDbSettings:DatabaseName"] = DatabaseName
            });
        });
    }

    public async Task CleanDatabaseAsync()
    {
        // Get a client and drop the entire test database
        using var scope = Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        await client.DropDatabaseAsync(DatabaseName);
    }
}