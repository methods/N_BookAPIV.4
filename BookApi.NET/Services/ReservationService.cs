using BookApi.NET.Models;
using Microsoft.AspNetCore.Authorization;

namespace BookApi.NET.Services;

public class ReservationNotFoundException : Exception
{
    public ReservationNotFoundException(Guid reservationId) : base($"Reservation not found with id: {reservationId}") { }
}

public class BookHasReservationsException : Exception
{
    public BookHasReservationsException(Guid bookId) : base($"Cannot delete book with ID {bookId} because it has active reservations.") { }
}

public class ReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IBookRepository _bookRepository;

    public ReservationService(IBookRepository bookRepository, IReservationRepository reservationRepository)
    {
        _bookRepository = bookRepository;
        _reservationRepository = reservationRepository;
    }

    [Authorize]
    public async Task<Reservation> CreateReservationAsync(Guid bookId, Guid userId)
    {
        // Check that the book exists
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book is null)
        {
            throw new BookNotFoundException(bookId);
        }

        // TODO: Check for existing reservation, to be added after GET route is complete

        var reservation = new Reservation(bookId, userId);

        await _reservationRepository.AddAsync(reservation);

        return reservation;
    }

    [Authorize]
    public async Task<Reservation> GetReservationByIdAsync(Guid bookId, Guid reservationId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation is null || reservation.BookId != bookId)
        {
            throw new ReservationNotFoundException(reservationId);
        }

        return reservation;
    }

    [Authorize]
    public async Task<Reservation> CancelReservationAsync(Guid bookId, Guid reservationId)
    {
        var reservation = await GetReservationByIdAsync(bookId, reservationId);
        reservation.Cancel();

        var updatedReservationInDb = await _reservationRepository.UpdateAsync(reservation);
        if (updatedReservationInDb is null || updatedReservationInDb.Status != ReservationStatus.Cancelled)
        {
            throw new InvalidOperationException($"Failed to update and retrieve reservation with ID {reservationId}");
        }

        return updatedReservationInDb;
    }

    [Authorize]
    public async Task<(List<Reservation> Reservations, long TotalCount)> GetAllAsync(int offset, int limit, Guid? userId)
    {
        var reservationsListAndCount = await _reservationRepository.GetAllAsync(offset, limit, userId);
        return reservationsListAndCount;
    }

}