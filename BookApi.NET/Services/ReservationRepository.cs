using BookApi.NET.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BookApi.NET.Services;

public class ReservationRepository : IReservationRepository
{
    private readonly IMongoCollection<Reservation> _reservationsCollection;

    public ReservationRepository(IOptions<BookstoreDbSettings> dbSettings, IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
        _reservationsCollection = database.GetCollection<Reservation>("reservations");
    }
        public Task AddAsync(Reservation reservation)
    {
        // To be implemented
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Reservation>> GetByBookIdAsync(Guid bookId)
    {
        // To be implemented
        throw new NotImplementedException();
    }
}