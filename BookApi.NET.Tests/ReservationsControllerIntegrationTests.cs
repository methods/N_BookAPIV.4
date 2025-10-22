using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;
using BookApi.NET.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace BookApi.NET.Tests;

public class ReservationsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReservationsControllerIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static readonly Guid TestUserId = new("11111111-1111-1111-1111-111111111111");

    public async Task InitializeAsync()
    {
        await DatabaseTestHelper.CleanDatabaseAsync(_factory.Services);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateReservation_WhenBookExists_ReturnsCreatedAndReservation()
    {
        // GIVEN a book exists in the database
        using var scope = _factory.Services.CreateScope();
        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();

        Book testBook = new Book("Test Book", "Test Author", "Test Synopsis");
        await bookRepository.CreateAsync(testBook);
        var bookId = testBook.Id;

        // AND a hard-coded userId
        // TODO: replace with a valid userId from user context once authentication is enabled
        var userId = TestUserId;

        // WHEN the POST reservation endpoint is called
        var response = await _client.PostAsync($"/books/{bookId}/reservations", null);

        // THEN the response status should be 201 Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // AND the response body should contain the new reservation object with reservationId and the submitted bookId and userId
        var responseBody = await response.Content.ReadFromJsonAsync<ReservationOutput>();
        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty, responseBody.Id);
        Assert.Equal(bookId, responseBody.BookId);
        Assert.Equal(userId, responseBody.UserId);

        // AND the location header should point to the new resource
        Assert.NotNull(response.Headers.Location);
        string expectedLocation = $"/books/{bookId}/reservations/{responseBody.Id}";
        Assert.EndsWith(expectedLocation, response.Headers.Location.ToString());
        Console.WriteLine(response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateReservation_WhenBookDoesNotExist_ReturnsNotFound()
    {
        // GIVEN a book Id that does not exist in the database
        var nonExistentBookId = Guid.NewGuid();

        // WHEN the POST reservation endpoint is called
        var response = await _client.PostAsync($"/books/{nonExistentBookId}/reservations", null);

        // THEN the response should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReservationById_WhenReservationExists_ReturnsOkAndReservation()
    {
        // GIVEN a book and a reservation for it exist in the database
        using var scope = _factory.Services.CreateScope();
        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();

        var testBook = new Book("Test Book", "Test Author", "Test Synopsis");
        await bookRepository.CreateAsync(testBook);

        var testReservation = new Reservation(testBook.Id, TestUserId);
        await reservationRepository.AddAsync(testReservation);

        // WHEN the GET reservation endpoint is called with the correct IDs
        var response = await _client.GetAsync($"/books/{testBook.Id}/reservations/{testReservation.Id}");

        // THEN the response status should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // AND the response body should contain the correct reservation details
        var reservationOutput = await response.Content.ReadFromJsonAsync<ReservationOutput>();
        Assert.NotNull(reservationOutput);
        Assert.Equal(testReservation.Id, reservationOutput.Id);
        Assert.Equal(testBook.Id, reservationOutput.BookId);
        Assert.Equal(TestUserId, reservationOutput.UserId);
        Assert.Equal("Active", reservationOutput.State);
    }
}