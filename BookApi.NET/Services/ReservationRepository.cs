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

    public async Task<Reservation?> UpdateAsync(Reservation reservation)
    {
        return await _reservationsCollection.FindOneAndReplaceAsync<Reservation>(
        r => r.Id == reservation.Id,
        reservation,
        new FindOneAndReplaceOptions<Reservation>
        {
            ReturnDocument = ReturnDocument.After
        });
    }

    public async Task<(List<Reservation> Reservations, long TotalCount)> GetAllAsync(int offset, int limit, Guid? userId)
    {
        var filter = Builders<Reservation>.Filter.Empty;

        if (userId.HasValue)
        {
            filter &= Builders<Reservation>.Filter.Eq(r => r.UserId, userId.Value);
        }

        // Create separate 'Facet's that can be aggregated into a single pipeline and run simultaneously
        // This eliminates potential race conditions when making 2 separate calls
        var countFacet = AggregateFacet.Create("metadata",
        PipelineDefinition<Reservation, AggregateCountResult>.Create(new[]
        {
            PipelineStageDefinitionBuilder.Count<Reservation>()
        }));

        var dataFacet = AggregateFacet.Create("data",
        PipelineDefinition<Reservation, Reservation>.Create(new[]
        {
            PipelineStageDefinitionBuilder.Skip<Reservation>(offset),
            PipelineStageDefinitionBuilder.Limit<Reservation>(limit)
        }));

        var aggregation = await _reservationsCollection.Aggregate()
            .Match(filter)
            .Facet(countFacet, dataFacet)
            .FirstOrDefaultAsync();

        var reservations = aggregation.Facets.First(x => x.Name == "data").Output<Reservation>().ToList();
        var totalCount = aggregation.Facets.First(x => x.Name == "metadata").Output<AggregateCountResult>()?.FirstOrDefault()?.Count ?? 0;

        return (reservations, totalCount);
    }
    
    public async Task<bool> HasReservationsForBookAsync(Guid bookId)
    {
        return await _reservationsCollection.Find(x => x.BookId == bookId).AnyAsync();
    }

    public Task<IEnumerable<Reservation>> GetByBookIdAsync(Guid bookId)
    {
        // To be implemented
        throw new NotImplementedException();
    }
}