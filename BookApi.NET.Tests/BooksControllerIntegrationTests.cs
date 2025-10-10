using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions; // (1) Import for logging

namespace BookApi.NET.Tests;

// (1) Inherit from IClassFixture<WebApplicationFactory<...>>
public class BooksControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BooksControllerIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
    }

    [Fact] // (4) The xUnit equivalent of @Test
    public async Task GetBookById_WhenCalled_ReturnsNotImplemented()
    {
        // GIVEN a random book ID
        var bookId = Guid.NewGuid();

        // WHEN we call the GET /books/{id} endpoint
        var response = await _client.GetAsync($"/books/{bookId}");

        // THEN the response status code should be 500 Internal Server Error
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}