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

    public ReservationListResponse ToReservationListResponse(
            List<Reservation> reservations,
            long totalCount,
            int offset,
            int limit)
    {
        var reservationDtos = reservations.Select(ToReservationOutput).ToList();

        return new ReservationListResponse
        {
            Items = reservationDtos,
            TotalCount = (int)totalCount,
            Offset = offset,
            Limit = limit
        };
    }
}