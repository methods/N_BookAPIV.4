using BookApi.NET.Models;
using Xunit;

namespace BookApi.NET.Tests;

public class ReservationUnitTests
{
    [Fact]
    public void Cancel_WhenReservationIsActive_ChangesStatusToCancelled()
    {
        // GIVEN an active reservation
        var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(ReservationStatus.Active, reservation.Status); // Verify precondition

        // WHEN Cancel is called
        reservation.Cancel();

        // THEN the status should be Cancelled
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void Cancel_WhenReservationIsAlreadyCancelled_ThrowsInvalidOperationException()
    {
        // GIVEN a reservation that is already cancelled
        var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid());
        reservation.Cancel(); // First cancellation works
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status); // Verify precondition

        // WHEN Cancel is called again
        var act = () => reservation.Cancel();

        // THEN an InvalidOperationException should be thrown
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("Only an active reservation can be cancelled.", exception.Message);
    }
}