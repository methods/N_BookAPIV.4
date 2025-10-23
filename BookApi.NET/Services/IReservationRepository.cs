using BookApi.NET.Models;

namespace BookApi.NET.Services;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation);
    Task<Reservation?> GetByIdAsync(Guid Id);
    Task<Reservation?> UpdateAsync(Reservation reservation);

    Task<IEnumerable<Reservation>> GetByBookIdAsync(Guid bookId);
}