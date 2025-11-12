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

public class BooksControllerIntegrationTests : IClassFixture<AuthenticatedBookApiWebFactory>, IAsyncLifetime 
{
    private readonly HttpClient _client;
    private readonly AuthenticatedBookApiWebFactory _factory;
    public BooksControllerIntegrationTests(AuthenticatedBookApiWebFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    private static readonly Guid TestUserId1 = new("11111111-1111-1111-1111-111111111111");
    public async Task InitializeAsync()
    {
        await _factory.CleanDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetBookById_WhenBookExists_ReturnsOKAndBookContent()
    {
        // GIVEN a book exists in the database
        var testBook = await CreateBookViaApiAsync("Test Book");
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

        // AND include HATEOAS links
        Assert.NotNull(responseBody.Links);

        // AND the should be correct
        var expectedBaseUrl = _client.BaseAddress!.ToString().TrimEnd('/');
        var expectedSelfLink = $"{expectedBaseUrl}/books/{bookId}";
        var expectedReservationsLink = $"{expectedBaseUrl}/books/{bookId}/reservations";

        Assert.Equal(expectedSelfLink, responseBody.Links.Self);
        Assert.Equal(expectedReservationsLink, responseBody.Links.Reservations);
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
    public async Task PostBook_WhenBookIsValid_CreatesBookAndReturns201()
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

    [Fact]
    public async Task PutBook_WhenBookAndBookInputAreValid_ReturnsOKAndModifiedBookContent()
    {
        // GIVEN a book exists in the database
        using var scope = _factory.Services.CreateScope();
        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();

        Book testBook = new Book("Test Book", "Test Author", "Test Synopsis");
        await bookRepository.CreateAsync(testBook);
        var bookId = testBook.Id;

        // AND a valid BookInput DTO
        var bookInput = new BookInput
        {
            Title = "Modified Book",
            Author = "Modified Author",
            Synopsis = "Modified Synopsis"
        };

        // WHEN the PUT /books endpoint is called
        var response = await _client.PutAsJsonAsync($"/books/{bookId}", bookInput);

        // THEN the response status code should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the JSON response body should match the modified book
        var responseBody = await response.Content.ReadFromJsonAsync<BookOutput>();
        Assert.NotNull(responseBody);
        Assert.Equal(bookInput.Title, responseBody.Title);
        Assert.NotEqual(Guid.Empty, responseBody.Id);
    }

    [Fact]
    public async Task PutBook_WhenBookDoesNotExist_ReturnsNotFound()
    {
        // GIVEN a non-existent book ID
        var nonExistentId = Guid.NewGuid();

        // AND a valid DTO for the update
        var bookInput = new BookInput
        {
            Title = "Modified Book",
            Author = "Modified Author",
            Synopsis = "Modified Synopsis"
        };

        // WHEN a PUT request is made to that non-existent ID
        var response = await _client.PutAsJsonAsync($"/books/{nonExistentId}", bookInput);

        // THEN the response status code should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_WhenBookExists_Returns204Deleted()
    {
        // GIVEN a book exists in the database
        using var scope = _factory.Services.CreateScope();
        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();

        Book testBook = new Book("Test Book", "Test Author", "Test Synopsis");
        await bookRepository.CreateAsync(testBook);
        var bookId = testBook.Id;

        // WHEN a DELETE request is made to that book's Id
        var response = await _client.DeleteAsync($"/books/{bookId}");

        // THEN the response status code should be 204 Deleted
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // AND the book should be gone from the database
        var deletedBook = await bookRepository.GetByIdAsync(bookId);
        Assert.Null(deletedBook);
    }

    [Fact]
    public async Task GetBooks_WhenBooksExist_ReturnsOKAndListObjectOfBooks()
    {
        // GIVEN multiple books exist in the database
        using var scope = _factory.Services.CreateScope();
        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();

        for (int i = 1; i <= 15; i++)
        {
            await bookRepository.CreateAsync(new Book("Book " + i, "Author " + i, "Synopsis " + i));
        }

        // WHEN a request is made for the 2nd page of books
        var response = await _client.GetAsync("/books?offset=5&limit=5");

        // THEN the response status code should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the response body should contain a list of books with pagination information
        var responseBody = await response.Content.ReadFromJsonAsync<BookListResponse>();

        Assert.NotNull(responseBody);
        Assert.Equal(15, responseBody.TotalCount);
        Assert.Equal(5, responseBody.Offset);
        Assert.Equal(5, responseBody.Limit);
        Assert.NotNull(responseBody.Items);
        Assert.Equal(5, responseBody.Items.Count);
        Assert.Equal("Book 6", responseBody.Items[0].Title);
    }

    [Fact]
    public async Task GetBooks_ReturnsPaginatedBooks_WithHateoasLinksForEachBook()
    {
        // GIVEN two books exist in the database
        var book1 = await CreateBookViaApiAsync("Book One");
        var book2 = await CreateBookViaApiAsync("Book Two");

        // WHEN the anonymous GET /books endpoint is called
        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync("/books");

        // THEN the response should be successful
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listResponse = await response.Content.ReadFromJsonAsync<BookListResponse>();
        Assert.NotNull(listResponse);
        Assert.Equal(2, listResponse.Items.Count);

        // AND every book in the list must have correct HATEOAS links
        var expectedBaseUrl = anonymousClient.BaseAddress!.ToString().TrimEnd('/');
        
        // Check the first book
        var book1Dto = listResponse.Items.First(b => b.Id == book1.Id);
        Assert.NotNull(book1Dto.Links);
        Assert.Equal($"{expectedBaseUrl}/books/{book1.Id}", book1Dto.Links.Self);
        Assert.Equal($"{expectedBaseUrl}/books/{book1.Id}/reservations", book1Dto.Links.Reservations);
        
        // Check the second book
        var book2Dto = listResponse.Items.First(b => b.Id == book2.Id);
        Assert.NotNull(book2Dto.Links);
        Assert.Equal($"{expectedBaseUrl}/books/{book2.Id}", book2Dto.Links.Self);
        Assert.Equal($"{expectedBaseUrl}/books/{book2.Id}/reservations", book2Dto.Links.Reservations);
    }

    [Fact]
    public async Task DeleteBook_WhenBookExists_AndHasReservations_ReturnsConflict()
    {
        // GIVEN a book exists in the database
        using var scope = _factory.Services.CreateScope();
        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();

        Book testBook = new Book("Test Book", "Test Author", "Test Synopsis");
        await bookRepository.CreateAsync(testBook);
        var bookId = testBook.Id;

        // AND a reservation for that book
        Reservation reservation = new Reservation(bookId, TestUserId1);
        await reservationRepository.AddAsync(reservation);

        // WHEN a DELETE request is made to that book's Id
        var response = await _client.DeleteAsync($"/books/{bookId}");

        // THEN the response status code should be 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // AND the book should still be in the database
        var notDeletedBook = await bookRepository.GetByIdAsync(bookId);
        Assert.NotNull(notDeletedBook);
    }

    [Fact]
    public async Task DeleteBook_AsRegularUser_ReturnsForbidden()
    {
        // GIVEN a client authenticated as a non-admin user
        var regUserClient = _factory.CreateClientFor(TestUsers.User1);

        // AND a book in the database
        var book = await CreateBookViaApiAsync("Book to Delete");

        // WHEN the regular user attempts to DELETE the book
        var response = await regUserClient.DeleteAsync($"/books/{book.Id}");

        // THEN the response should indicate access denied
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden || 
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected Forbidden (403) or NotFound (404), but received {response.StatusCode}");

        // AND the book should still be in the database
        var getResponse = await _client.GetAsync($"/books/{book.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task PostBook_AsRegularUser_ReturnsForbidden()
    {
        // GIVEN a client authenticated as a non-admin user
        var regUserClient = _factory.CreateClientFor(TestUsers.User1);

        // AND a valid BookInput DTO
        var bookInput = new BookInput
        {
            Title = "Test Book",
            Author = "Test Author",
            Synopsis = "A book that should not be created."
        };

        // WHEN the user attempts to POST the Book
        var response = await regUserClient.PostAsJsonAsync("/books", bookInput);

        // THEN the response should indicate access denied
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden || 
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected Forbidden (403) or NotFound (404), but received {response.StatusCode}");
    }

    [Fact]
    public async Task UpdateBook_AsRegularUser_ReturnsForbidden()
    {
        // GIVEN a client authenticated as a non-admin user
        var regUserClient = _factory.CreateClientFor(TestUsers.User1);

        // AND a book in the database
        var book = await CreateBookViaApiAsync("Book to Stay Same");

        // AND a valid BookInput DTO
        var bookInput = new BookInput
        {
            Title = "Modified Book",
            Author = "Modified Author",
            Synopsis = "Modified Synopsis"
        };

        // WHEN the user attempts to modify the book via the PUT endpoint
        var response = await regUserClient.PutAsJsonAsync($"/books/{book.Id}", bookInput);

        // THEN the response should indicate access denied
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden || 
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected Forbidden (403) or NotFound (404), but received {response.StatusCode}");

        // AND the book should be unmodified
        var getResponse = await _client.GetAsync($"/books/{book.Id}");
        var unmodifiedBook = await getResponse.Content.ReadFromJsonAsync<BookOutput>();
        Assert.NotNull(unmodifiedBook);
        Assert.Equal("Book to Stay Same", unmodifiedBook.Title);
    }

    // Helper methods to reduce test setup duplication
    private async Task<BookOutput> CreateBookViaApiAsync(string title)
    {
        var bookInput = new BookInput
        {
            Title = title,
            Author = "Test Author",
            Synopsis = "A test synopsis."
        };

        var response = await _client.PostAsJsonAsync("/books", bookInput);
        response.EnsureSuccessStatusCode();

        var createdBook = await response.Content.ReadFromJsonAsync<BookOutput>();
        Assert.NotNull(createdBook);
        return createdBook;
    }
}

public record ErrorDto(string Error);