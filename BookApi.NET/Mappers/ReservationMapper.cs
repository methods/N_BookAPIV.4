using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;

namespace BookApi.NET.Services;

public class ReservationMapper
{
    public ReservationOutput ToReservationOutput(Reservation reservation)
    {
        return new ReservationOutput
        {
            Id = reservation.Id,
            BookId = reservation.BookId,
            UserId = reservation.UserId,
            ReservedAt = new DateTimeOffset(reservation.ReservedAt, TimeSpan.Zero),
            State = reservation.Status.ToString()
        };
    }
}