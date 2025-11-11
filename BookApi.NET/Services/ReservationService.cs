using System.Security.Claims;
using BookApi.NET.Common;
using BookApi.NET.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReservationService(IBookRepository bookRepository, IReservationRepository reservationRepository, IHttpContextAccessor httpContextAccessor)
    {
        _bookRepository = bookRepository;
        _reservationRepository = reservationRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal GetCurrentUser() => _httpContextAccessor.HttpContext!.User;

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

    public async Task<Reservation> GetReservationByIdAsync(Guid bookId, Guid reservationId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation is null || reservation.BookId != bookId)
        {
            throw new ReservationNotFoundException(reservationId);
        }

        var currentUser = GetCurrentUser();
        var isUserOwner = reservation.UserId == Guid.Parse(currentUser.FindFirst(CustomClaimTypes.InternalUserId)!.Value);
        var isUserAdmin = currentUser.IsInRole(AppRoles.Admin);

        if (!isUserOwner && !isUserAdmin)
        {
            throw new ReservationNotFoundException(reservationId);
        }


        return reservation;
    }

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

    public async Task<(List<Reservation> Reservations, long TotalCount)> GetAllAsync(int offset, int limit, Guid? userId)
    {
        var currentUser = GetCurrentUser();
        var isCurrentUserAdmin = currentUser.IsInRole(AppRoles.Admin);
        Guid? userIdFilter;

        if (isCurrentUserAdmin)
        {
            userIdFilter = userId;
        }
        else
        {
            var currentUserId = currentUser.FindFirst(CustomClaimTypes.InternalUserId)?.Value;
            if (currentUserId is null)
            {
                throw new ApplicationException("User is authenticated but internal user ID claim is missing.");
            }
            userIdFilter = Guid.Parse(currentUserId);
        }

        var reservationsListAndCount = await _reservationRepository.GetAllAsync(offset, limit, userIdFilter);
        return reservationsListAndCount;
    }

}