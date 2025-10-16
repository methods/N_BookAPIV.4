using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;
using BookApi.NET.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace BookApi.NET.Tests;

public class BooksControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    public BooksControllerIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact] // (4) The xUnit equivalent of @Test
    public async Task GetBookById_WhenBookExists_ReturnsOKAndBookContent()
    {
        // GIVEN a book exists in the database
        using var scope = _factory.Services.CreateScope();
        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();

        Book testBook = new Book("Test Book", "Test Author", "Test Synopsis");
        await bookRepository.CreateAsync(testBook);
        var bookId = testBook.Id;

        // WHEN we call the GET /books/{id} endpoint
        var response = await _client.GetAsync($"/books/{bookId}");

        // THEN the response status code should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the JSON response body should match the given book
        var responseBody = await response.Content.ReadFromJsonAsync<BookOutput>();
        Assert.NotNull(responseBody);
        Assert.Equal(testBook.Title, responseBody.Title);
        Assert.Equal(testBook.Id, responseBody.Id);
    }

    [Fact]
    public async Task GetBookById_WhenBookDoesNotExist_Returns404()
    {
        // GIVEN a non-existent bookId
        var nonExistentBookId = Guid.NewGuid();

        // WHEN we call the GET /books/{id} endpoint
        var response = await _client.GetAsync($"/books/{nonExistentBookId}");

        // THEN the response status code should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // AND the response body contains the correct error message
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal($"Book not found with id: {nonExistentBookId}", error.Error);
    }

    [Fact]
    public async Task PostBook_WhenBookIsValid_CreatesBookAndReturns202()
    {
        // GIVEN a valid BookInput DTO
        var bookInput = new BookInput
        {
            Title = "Neuromancer",
            Author = "William Gibson",
            Synopsis = "A classic cyberpunk novel."
        };

        // WHEN the POST /books endpoint is called
        var response = await _client.PostAsJsonAsync("/books", bookInput);

        // THEN the response status code should be 201 Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // AND the response body should contain the new book object with bookId
        var responseBody = await response.Content.ReadFromJsonAsync<BookOutput>();
        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty, responseBody.Id);
        Assert.Equal("Neuromancer", responseBody.Title);

        // AND the location header should point to the new resource
        Assert.NotNull(response.Headers.Location);
        string expectedLocation = $"/books/{responseBody.Id}";
        Assert.EndsWith(expectedLocation, response.Headers.Location.ToString());
    }
}

public record ErrorDto(string Error);