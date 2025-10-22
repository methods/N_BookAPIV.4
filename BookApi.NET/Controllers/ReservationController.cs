using BookApi.NET.Controllers.Generated;
using BookApi.NET.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BookApi.NET.Controllers;

[ApiController]
public class ReservationController : ReservationsControllerBase
{
    private readonly ReservationService _reservationService;
    private readonly ReservationMapper _reservationMapper;

    private static readonly Guid TestUserId = new("11111111-1111-1111-1111-111111111111");

    public ReservationController(ReservationService reservationService, ReservationMapper reservationMapper)
    {
        _reservationService = reservationService;
        _reservationMapper = reservationMapper;
    }
    public override Task<ActionResult<ReservationOutput>> ReservationsDelete([BindRequired] Guid bookId, [BindRequired] Guid reservationId)
    {
        throw new NotImplementedException();
    }

    public override Task<ActionResult<ReservationListResponse>> ReservationsGet([FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] Guid? userId)
    {
        throw new NotImplementedException();
    }

    public override async Task<ActionResult<ReservationOutput>> ReservationsGet([BindRequired] Guid bookId, [BindRequired] Guid reservationId)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(bookId, reservationId);

        var reservationOutput = _reservationMapper.ToReservationOutput(reservation);

        return reservationOutput;
    }

    public override async Task<ActionResult<ReservationOutput>> ReservationsPost([BindRequired] Guid bookId)
    {
        var newReservation = await _reservationService.CreateReservationAsync(bookId, TestUserId);

        var reservationDto = _reservationMapper.ToReservationOutput(newReservation);

        // var locationUri = $"/books/{newReservation.BookId}/reservations/{newReservation.Id}";

        return

        // Created(locationUri, reservationDto);

        CreatedAtAction(
            nameof(ReservationsGet),
            new { bookId = newReservation.BookId, reservationId = newReservation.Id },
            reservationDto
        );
    }
}