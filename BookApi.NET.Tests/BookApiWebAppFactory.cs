using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Authentication;

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

        builder.ConfigureTestServices(services =>
        {
            var authDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthenticationService));
            if (authDescriptor is not null)
            {
                services.Remove(authDescriptor);
            }

            // var schemeProviderDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthenticationSchemeProvider));
            // if (schemeProviderDescriptor != null)
            // {
            //     services.Remove(schemeProviderDescriptor);
            // }

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options =>
                {
                });
        });
    }
    
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
    }

    public async Task CleanDatabaseAsync()
    {
        // Get a client and drop the entire test database
        using var scope = Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        await client.DropDatabaseAsync(DatabaseName);
    }
}