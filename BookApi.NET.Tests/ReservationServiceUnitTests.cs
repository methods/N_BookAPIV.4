using BookApi.NET.Models;
using BookApi.NET.Services;
using Moq;

namespace BookApi.NET.Tests;

public class ReservationServiceUnitTests
{
    private readonly Mock<IBookRepository> _mockBookRepository;
    private readonly Mock<IReservationRepository> _mockReservationRepository;
    private readonly ReservationService _mockReservationService;

    public ReservationServiceUnitTests()
    {
        _mockBookRepository = new Mock<IBookRepository>();
        _mockReservationRepository = new Mock<IReservationRepository>();
        _mockReservationService = new ReservationService(_mockBookRepository.Object, _mockReservationRepository.Object);
    }

    [Fact]
    public async Task GetReservationByIdAsync_WhenBookIdDoesNotMatch_ThrowsReservationNotFoundException()
    {
        // GIVEN a mock reservation for a mock book for a random user
        var mockBookId = Guid.NewGuid();
        var randomUser = Guid.NewGuid();
        var mockReservation = new Reservation(mockBookId, randomUser);

        // AND non-matching BookId
        var differentBookId = Guid.NewGuid();

        // AND a mock repository configured to return the mockReservation
        _mockReservationRepository
            .Setup(repo => repo.GetByIdAsync(mockReservation.Id))
            .ReturnsAsync(mockReservation);

        // WHEN the service function is called with the correct ReservationId but the wrong BookId
        // THEN a ReservationNotFound exception should be thrown
        await Assert.ThrowsAsync<ReservationNotFoundException>(() =>
            _mockReservationService.GetReservationByIdAsync(differentBookId, mockReservation.Id));
    }
    
    [Fact]
    public async Task CancelReservationAsync_WhenUpdateFailsAndReturnsNull_ThrowsInvalidOperationException()
    {
        // GIVEN a valid bookId and reservationId
        var bookId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        
        // AND an existing active reservation
        var existingReservation = new Reservation(bookId, Guid.NewGuid()) { Id = reservationId };

        // AND the repository is configured to find the reservation successfully
        _mockReservationRepository
            .Setup(repo => repo.GetByIdAsync(reservationId))
            .ReturnsAsync(existingReservation);
        
        // BUT the repository's UpdateAsync method is configured to fail by returning null
        _mockReservationRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<Reservation>()))
            .ReturnsAsync((Reservation?)null);

        // WHEN CancelReservationAsync is called
        var act = () => _mockReservationService.CancelReservationAsync(bookId, reservationId);

        // THEN it should throw an InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }
}