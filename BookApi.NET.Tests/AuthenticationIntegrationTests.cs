using System.Net;
using System.Net.Http.Json;
using BookApi.NET.Controllers.Generated;
using Xunit;

namespace BookApi.NET.Tests;

public class AuthenticationIntegrationTests : IClassFixture<AnonymousBookApiWebAppFactory>, IAsyncLifetime
{
    private readonly AnonymousBookApiWebAppFactory _factory;
    private readonly HttpClient _anonymousClient;

    public AuthenticationIntegrationTests(AnonymousBookApiWebAppFactory factory)
    {
        _factory = factory;
        _anonymousClient = _factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.CleanDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProtectedEndpoint_WhenUserIsAnonymous_ReturnsUnauthorized()
    {
        // GIVEN a protected endpoint and an anonymous client
        var bookInput = new BookInput { Title = "Unauthorized Book", Author = "Anon", Synopsis = "..." };

        // WHEN the protected endpoint is called
        var response = await _anonymousClient.PostAsJsonAsync("/books", bookInput);

        // THEN the response should be 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}