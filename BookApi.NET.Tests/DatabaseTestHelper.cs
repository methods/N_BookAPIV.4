using BookApi.NET.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BookApi.NET.Tests;

public static class DatabaseTestHelper
{
    public static async Task CleanDatabaseAsync(IServiceProvider services)
    {
        // Get the MongoDB client
        var mongoClient = services.GetRequiredService<IMongoClient>();

        var dbSettings = services.GetRequiredService<IOptions<BookstoreDbSettings>>();
        var dbName = dbSettings.Value.DatabaseName;

        await mongoClient.DropDatabaseAsync(dbName);
    }
}