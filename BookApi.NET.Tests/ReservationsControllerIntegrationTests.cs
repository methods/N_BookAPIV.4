using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;
using BookApi.NET.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace BookApi.NET.Tests;

public class ReservationsControllerIntegrationTests : IClassFixture<AuthenticatedBookApiWebFactory>, IAsyncLifetime
{
    private readonly AuthenticatedBookApiWebFactory _factory;
    private readonly HttpClient _client;

    public ReservationsControllerIntegrationTests(AuthenticatedBookApiWebFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static readonly Guid TestUserId1 = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestUserId2 = new("22222222-2222-2222-2222-222222222222");

    public async Task InitializeAsync()
    {
        await _factory.CleanDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateReservation_WhenBookExists_ReturnsCreatedAndReservation()
    {
        // GIVEN a book exists in the database
        var testBook = await CreateBookViaApiAsync("Test Book");
        var bookId = testBook.Id;

        // AND an Admin userId from the test client
        var userId = TestUsers.Admin.Id;

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
        // GIVEN a book and a regular user reservation for it exist in the database
        var book = await CreateBookViaApiAsync("A Book With No Reservations");
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var testReservation = await CreateReservationViaApiAsync(clientUser1, book.Id);

        // WHEN the GET reservation endpoint is called with the correct IDs
        var response = await clientUser1.GetAsync($"/books/{book.Id}/reservations/{testReservation.Id}");

        // THEN the response status should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the response body should contain the correct reservation details
        var reservationOutput = await response.Content.ReadFromJsonAsync<ReservationOutput>();
        Assert.NotNull(reservationOutput);
        Assert.Equal(testReservation.Id, reservationOutput.Id);
        Assert.Equal(book.Id, reservationOutput.BookId);
        Assert.Equal(testReservation.UserId, reservationOutput.UserId);
        Assert.Equal("Active", reservationOutput.State);
    }

    [Fact]
    public async Task GetReservationById_WhenReservationExists_ButUserIsNotReservationOwner_ReturnsNotFound()
    {
        // GIVEN a book and a regular user reservation for it exist in the database
        var book = await CreateBookViaApiAsync("A Book With No Reservations");
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var testReservation = await CreateReservationViaApiAsync(clientUser1, book.Id);

        // AND another regular user who is NOT the owner of the reservation
        var clientUser2 = _factory.CreateClientFor(TestUsers.User2);

        // WHEN the non-owner used calls the GET reservation endpointwith the correct IDs
        var response = await clientUser2.GetAsync($"/books/{book.Id}/reservations/{testReservation.Id}");

        // THEN the response status should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

        [Fact]
    public async Task GetReservationById_WhenReservationExists_AndUserIsNotOwnerButIsAdmin_ReturnsOkAndReservation()
    {
        // GIVEN a book and a regular user reservation for it exist in the database
        var book = await CreateBookViaApiAsync("A Book With No Reservations");
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var testReservation = await CreateReservationViaApiAsync(clientUser1, book.Id);

        // AND a separate admin user client
        var adminClient = _factory.CreateClient();

        // WHEN the GET reservation endpoint is called with the correct IDs by the admin
        var response = await adminClient.GetAsync($"/books/{book.Id}/reservations/{testReservation.Id}");

        // THEN the response status should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the response body should contain the correct reservation details
        var reservationOutput = await response.Content.ReadFromJsonAsync<ReservationOutput>();
        Assert.NotNull(reservationOutput);
        Assert.Equal(testReservation.Id, reservationOutput.Id);
        Assert.Equal(book.Id, reservationOutput.BookId);
        Assert.Equal(testReservation.UserId, reservationOutput.UserId);
        Assert.Equal("Active", reservationOutput.State);
    }

    [Fact]
    public async Task GetReservationById_WhenReservationDoesNotExist_ReturnsNotFound()
    {
        // GIVEN a book exists in the database
        var book = await CreateBookViaApiAsync("A Book With No Reservations");
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
        var book1 = await CreateBookViaApiAsync("Book One");
        var book2 = await CreateBookViaApiAsync("Book Two");

        using var scope = _factory.Services.CreateScope();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var reservationForBook1 = new Reservation(book1.Id, TestUserId1);
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
        var book = await CreateBookViaApiAsync("Book to Cancel");
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var reservation = await CreateReservationViaApiAsync(clientUser1, book.Id);

        // WHEN the Delete endpoint is called
        var response = await clientUser1.DeleteAsync($"/books/{book.Id}/reservations/{reservation.Id}");

        // THEN the response status code should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the response body should show the reservation is now cancelled
        var cancelledReservationDto = await response.Content.ReadFromJsonAsync<ReservationOutput>();
        Assert.NotNull(cancelledReservationDto);
        Assert.Equal(reservation.Id, cancelledReservationDto.Id);
        Assert.Equal("Cancelled", cancelledReservationDto.State);

        // AND the reservation in the database should be updated to Cancelled
        using var scope = _factory.Services.CreateScope();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var dbReservation = await reservationRepository.GetByIdAsync(reservation.Id);
        Assert.NotNull(dbReservation);
        Assert.Equal(ReservationStatus.Cancelled, dbReservation.Status);
    }
    
    [Fact]
    public async Task CancelReservation_WhenReservationExistsAndIsActive_ButUserIsNotOwner_ReturnsNotFound()
    {
        // GIVEN a book and an active reservation for it in the database
        var book = await CreateBookViaApiAsync("Book to Cancel");
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var reservation = await CreateReservationViaApiAsync(clientUser1, book.Id);

        // AND another regular user who is NOT the owner of the reservation
        var clientUser2 = _factory.CreateClientFor(TestUsers.User2);

        // WHEN the Delete endpoint is called by the non-owner user
        var response = await clientUser2.DeleteAsync($"/books/{book.Id}/reservations/{reservation.Id}");

        // THEN the response status code should be Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // AND the reservation in the database should still be Active
        using var scope = _factory.Services.CreateScope(); 
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var dbReservation = await reservationRepository.GetByIdAsync(reservation.Id);
        Assert.NotNull(dbReservation);
        Assert.Equal(ReservationStatus.Active, dbReservation.Status);
    }

    [Fact]
    public async Task CancelReservation_WhenReservationDoesNotExist_ReturnsNotFound()
    {
        // GIVEN a book exists, but the reservation ID does not
        var book = await CreateBookViaApiAsync("A Book");
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
        var book1 = await CreateBookViaApiAsync("Book One");
        var book2 = await CreateBookViaApiAsync("Book Two");
        var adminClient = _factory.CreateClient();
        var reservationForBook1 = await CreateReservationViaApiAsync(adminClient, book1.Id);

        // WHEN the DELETE endpoint is called on book2 with the reservation from book1
        var response = await _client.DeleteAsync($"/books/{book2.Id}/reservations/{reservationForBook1.Id}");

        // THEN the response should be 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_WhenReservationIsAlreadyCancelled_ReturnsConflict()
    {
        // GIVEN a book with a reservation that is already cancelled
        var book = await CreateBookViaApiAsync("A Book");
        var adminClient = _factory.CreateClient();
        var reservation = await CreateReservationViaApiAsync(adminClient, book.Id);
        var response = await _client.DeleteAsync($"/books/{book.Id}/reservations/{reservation.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // WHEN the DELETE endpoint is called again on the already-cancelled reservation
        var secondResponse = await _client.DeleteAsync($"/books/{book.Id}/reservations/{reservation.Id}");

        // THEN the response status should be 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ListReservations_WhenFilteredByUserId_ReturnsOnlyThatUsersReservations()
    {
        // GIVEN two different authenticated clients
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var clientUser2 = _factory.CreateClientFor(TestUsers.User2);
        var adminClient = _factory.CreateClient(); // Default client is the Admin

        // AND a book created by the admin
        var book = await CreateBookViaApiAsync("A Shared Book");

        // AND reservations created by User 1 and User 2
        for(int i=0; i<15; i++) { await CreateReservationViaApiAsync(clientUser1, book.Id); }
        for(int i=0; i<10; i++) { await CreateReservationViaApiAsync(clientUser2, book.Id); }

        // WHEN the admin GETs the reservations list, filtering by User 1's ID
        var response = await adminClient.GetAsync($"/reservations?userId={TestUsers.User1.Id}");

        // THEN the response should be successful and contain only User 1's 15 reservations
        response.EnsureSuccessStatusCode();
        var listResponse = await response.Content.ReadFromJsonAsync<ReservationListResponse>();
        Assert.NotNull(listResponse);
        Assert.Equal(15, listResponse.TotalCount);
        Assert.All(listResponse.Items, item => Assert.Equal(TestUsers.User1.Id, item.UserId));
    }

    [Fact]
    public async Task ListReservations_WhenReservationsExist_ReturnsFirstPageOfReservations()
    {
        // GIVEN two different authenticated clients
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var clientUser2 = _factory.CreateClientFor(TestUsers.User2);
        var adminClient = _factory.CreateClient(); // Default client is the Admin

        // AND a book created by the admin
        var book = await CreateBookViaApiAsync("A Shared Book");

        // AND 25 total reservations created by User 1 and User 2
        for (int i = 0; i < 15; i++) { await CreateReservationViaApiAsync(clientUser1, book.Id); }
        for (int i = 0; i < 10; i++) { await CreateReservationViaApiAsync(clientUser2, book.Id); }

        // WHEN a GET request is made to the /reservations endpoint
        var response = await adminClient.GetAsync("/reservations");

        // THEN the response status should be 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // AND the response body should be a paginated list
        var listResponse = await response.Content.ReadFromJsonAsync<ReservationListResponse>();
        Assert.NotNull(listResponse);
        Assert.Equal(25, listResponse.TotalCount); // Total number of reservations in the DB
        Assert.Equal(20, listResponse.Items.Count); // Default page size is 20
        Assert.Equal(0, listResponse.Offset);
        Assert.Equal(20, listResponse.Limit);
    }

    [Fact]
    public async Task ListReservations_AsRegularUser_ReturnsOnlyOwnReservations()
    {
        // GIVEN two different authenticated clients
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var clientUser2 = _factory.CreateClientFor(TestUsers.User2);

        // AND a book created by the admin
        var book = await CreateBookViaApiAsync("A Shared Book");

        // AND reservations created by User 1 and User 2
        for (int i = 0; i < 15; i++) { await CreateReservationViaApiAsync(clientUser1, book.Id); }
        for (int i = 0; i < 10; i++) { await CreateReservationViaApiAsync(clientUser2, book.Id); }

        // WHEN User1 GETs the reservations list
        var response = await clientUser1.GetAsync("/reservations");

        // THEN the response should be successful and contain only User 1's 15 reservations
        response.EnsureSuccessStatusCode();
        var listResponse = await response.Content.ReadFromJsonAsync<ReservationListResponse>();
        Assert.NotNull(listResponse);
        Assert.Equal(15, listResponse.TotalCount);
        Assert.All(listResponse.Items, item => Assert.Equal(TestUsers.User1.Id, item.UserId));
    }

    [Fact]
    public async Task ListReservations_AsRegularUser_FilteredByOtherUserId_ReturnsOnlyOwnReservations()
    {
        // GIVEN two different authenticated clients
        var clientUser1 = _factory.CreateClientFor(TestUsers.User1);
        var clientUser2 = _factory.CreateClientFor(TestUsers.User2);
        var adminClient = _factory.CreateClient(); // Default client is the Admin

        // AND a book created by the admin
        var book = await CreateBookViaApiAsync("A Shared Book");

        // AND 25 total reservations created by User 1 and User 2
        for (int i = 0; i < 15; i++) { await CreateReservationViaApiAsync(clientUser1, book.Id); }
        for (int i = 0; i < 10; i++) { await CreateReservationViaApiAsync(clientUser2, book.Id); }

        // WHEN User1 GET's the reservations list, FILTERED using User2's id
        var response = await clientUser1.GetAsync($"/reservations?userId={TestUsers.User2.Id}");

        // THEN the response should be successful, ignore the filter, and contain only User 1's 15 reservations
        response.EnsureSuccessStatusCode();
        var listResponse = await response.Content.ReadFromJsonAsync<ReservationListResponse>();
        Assert.NotNull(listResponse);
        Assert.Equal(15, listResponse.TotalCount);
        Assert.All(listResponse.Items, item => Assert.Equal(TestUsers.User1.Id, item.UserId));
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

    private async Task<ReservationOutput> CreateReservationViaApiAsync(HttpClient client, Guid bookId)
    {
        var response = await client.PostAsync($"/books/{bookId}/reservations", null);
        response.EnsureSuccessStatusCode();
        
        var createdReservation = await response.Content.ReadFromJsonAsync<ReservationOutput>();
        Assert.NotNull(createdReservation);

        return createdReservation;
    }
}