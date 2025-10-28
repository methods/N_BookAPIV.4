using BookApi.NET.Models;

namespace BookApi.NET.Services;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation);
    Task<Reservation?> GetByIdAsync(Guid Id);
    Task<Reservation?> UpdateAsync(Reservation reservation);
    Task<(List<Reservation> Reservations, long TotalCount)> GetAllAsync(int offset, int limit, Guid? userId);
    Task<bool> HasReservationsForBookAsync(Guid bookId);

    Task<IEnumerable<Reservation>> GetByBookIdAsync(Guid bookId);
}