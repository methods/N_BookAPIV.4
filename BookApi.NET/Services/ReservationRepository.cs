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
    public async Task AddAsync(Reservation reservation)
    {
        await _reservationsCollection.InsertOneAsync(reservation);
    }
    
    public async Task<Reservation?> GetByIdAsync(Guid Id) =>
        await _reservationsCollection.Find(x => x.Id == Id).FirstOrDefaultAsync();

    public Task<IEnumerable<Reservation>> GetByBookIdAsync(Guid bookId)
    {
        // To be implemented
        throw new NotImplementedException();
    }
}