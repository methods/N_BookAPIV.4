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

public class ReservationsControllerIntegrationTests : IClassFixture<BookApiWebFactory>, IAsyncLifetime
{
    private readonly BookApiWebFactory _factory;
    private readonly HttpClient _client;

    public ReservationsControllerIntegrationTests(BookApiWebFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static readonly Guid TestUserId = new("11111111-1111-1111-1111-111111111111");

    public async Task InitializeAsync()
    {
        await _factory.CleanDatabaseAsync();
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
        var book = await CreateBookInDbAsync("A Book With No Reservations");
        using var scope = _factory.Services.CreateScope();;
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var testReservation = new Reservation(book.Id, TestUserId);
        await reservationRepository.AddAsync(testReservation);

        // WHEN the GET reservation endpoint is called with the correct IDs
        var response = await _client.GetAsync($"/books/{book.Id}/reservations/{testReservation.Id}");

        // THEN the response status should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the response body should contain the correct reservation details
        var reservationOutput = await response.Content.ReadFromJsonAsync<ReservationOutput>();
        Assert.NotNull(reservationOutput);
        Assert.Equal(testReservation.Id, reservationOutput.Id);
        Assert.Equal(book.Id, reservationOutput.BookId);
        Assert.Equal(TestUserId, reservationOutput.UserId);
        Assert.Equal("Active", reservationOutput.State);
    }

    [Fact]
    public async Task GetReservationById_WhenReservationDoesNotExist_ReturnsNotFound()
    {
        // GIVEN a book exists in the database
        var book = await CreateBookInDbAsync("A Book With No Reservations");
        // AND a non-existent reservation ID
        var nonExistentReservationId = Guid.NewGuid();

        // WHEN the GET endpoint is called with the non-existent reservationId
        var response = await _client.GetAsync($"/books/{book.Id}/reservations/{nonExistentReservationId}");

        // THEN the response status should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReservationById_WhenBookIdDoesNotMatch_ReturnsNotFound()
    {
        // GIVEN two different books, and a reservation for the first book
        var book1 = await CreateBookInDbAsync("Book One");
        var book2 = await CreateBookInDbAsync("Book Two");

        using var scope = _factory.Services.CreateScope();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var reservationForBook1 = new Reservation(book1.Id, TestUserId);
        await reservationRepository.AddAsync(reservationForBook1);

        // WHEN the GET endpoint is called for book2, but with the reservationId from book1
        var response = await _client.GetAsync($"/books/{book2.Id}/reservations/{reservationForBook1.Id}");

        // THEN the response status should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_WhenReservationExistsAndIsActive_ReturnsOkAndCancelledReservation()
    {
        // GIVEN a book and an active reservation for it in the database
        var book = await CreateBookInDbAsync("Book to Cancel");
        var reservation = new Reservation(book.Id, TestUserId);
        Assert.Equal(ReservationStatus.Active, reservation.Status); // Check the reservation's status is set to active
        using var scope = _factory.Services.CreateScope();          // Add the reservation to the database
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        await reservationRepository.AddAsync(reservation);

        // WHEN the Delete endpoint is called
        var response = await _client.DeleteAsync($"/books/{book.Id}/reservations/{reservation.Id}");

        // THEN the response status code should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the response body should show the reservation is now cancelled
        var cancelledReservationDto = await response.Content.ReadFromJsonAsync<ReservationOutput>();
        Assert.NotNull(cancelledReservationDto);
        Assert.Equal(reservation.Id, cancelledReservationDto.Id);
        Assert.Equal("Cancelled", cancelledReservationDto.State);

        // AND the reservation in the database should be updated to Cancelled
        var dbReservation = await reservationRepository.GetByIdAsync(reservation.Id);
        Assert.NotNull(dbReservation);
        Assert.Equal(ReservationStatus.Cancelled, dbReservation.Status);
    }

    [Fact]
    public async Task CancelReservation_WhenReservationDoesNotExist_ReturnsNotFound()
    {
        // GIVEN a book exists, but the reservation ID does not
        var book = await CreateBookInDbAsync("A Book");
        var nonExistentReservationId = Guid.NewGuid();

        // WHEN the DELETE endpoint is called
        var response = await _client.DeleteAsync($"/books/{book.Id}/reservations/{nonExistentReservationId}");

        // THEN the response should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_WhenBookIdDoesNotMatch_ReturnsNotFound()
    {
        // GIVEN two books, and a reservation for the first one
        var book1 = await CreateBookInDbAsync("Book One");
        var book2 = await CreateBookInDbAsync("Book Two");
        var reservationForBook1 = await CreateReservationInDbAsync(book1.Id, TestUserId);

        // WHEN the DELETE endpoint is called on book2 with the reservation from book1
        var response = await _client.DeleteAsync($"/books/{book2.Id}/reservations/{reservationForBook1.Id}");

        // THEN the response should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_WhenReservationIsAlreadyCancelled_ReturnsConflict()
    {
        // GIVEN a book with a reservation that is already cancelled
        var book = await CreateBookInDbAsync("A Book");
        var reservation = await CreateReservationInDbAsync(book.Id, TestUserId);
        reservation.Cancel();
        using var scope = _factory.Services.CreateScope();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        await reservationRepository.UpdateAsync(reservation);
        
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status); // Sanity check to ensure the setup is correct

        // WHEN the DELETE endpoint is called again on the already-cancelled reservation
        var response = await _client.DeleteAsync($"/books/{book.Id}/reservations/{reservation.Id}");

        // THEN the response status should be 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Helper methods to reduce test setup duplication
    private async Task<Book> CreateBookInDbAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();
        var book = new Book(title, "Test Author", "Synopsis");
        await bookRepository.CreateAsync(book);
        return book;
    }

    private async Task<Reservation> CreateReservationInDbAsync(Guid bookId, Guid userId)
{
    using var scope = _factory.Services.CreateScope();
    var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
    var reservation = new Reservation(bookId, userId);
    await reservationRepository.AddAsync(reservation);
    return reservation;
}
}