using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;
using BookApi.NET.Services;
using Xunit;

namespace BookApi.NET.Tests;

public class ReservationMapperUnitTests
{
    private readonly ReservationMapper _sut; // System Under Test

    public ReservationMapperUnitTests()
    {
        _sut = new ReservationMapper();
    }

    [Fact]
    public void ToReservationListResponse_CorrectlyMapsDataToDto()
    {
        // GIVEN a list of reservation models and pagination data
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var reservations = new List<Reservation>
        {
            new Reservation(bookId, userId),
            new Reservation(bookId, userId)
        };
        long totalCount = 100;
        int offset = 20;
        int limit = 10;

        // WHEN ToReservationListResponse is called
        var result = _sut.ToReservationListResponse(reservations, totalCount, offset, limit);

        // THEN the resulting DTO should have the correct properties
        Assert.NotNull(result);
        Assert.IsType<ReservationListResponse>(result);
        
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(offset, result.Offset);
        Assert.Equal(limit, result.Limit);
        
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items.Count);
        
        // AND the items in the list should be mapped correctly
        Assert.Equal(reservations[0].Id, result.Items[0].Id);
        Assert.Equal("Active", result.Items[0].State);
    }
}