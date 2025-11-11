using BookApi.NET.Common;
using BookApi.NET.Controllers.Generated;
using BookApi.NET.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BookApi.NET.Controllers;

[ApiController]
[Authorize]
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
    
    public override async Task<ActionResult<ReservationOutput>> ReservationsDelete([BindRequired] Guid bookId, [BindRequired] Guid reservationId)
    {
        // TODO: Check if the authenticated user is the owner of the reservation
        var cancelledReservation = await _reservationService.CancelReservationAsync(bookId, reservationId);

        var reservationDto = _reservationMapper.ToReservationOutput(cancelledReservation);

        return Ok(reservationDto);
    }

    public override async Task<ActionResult<ReservationListResponse>> ReservationsGet([FromQuery] int? offset = 0, [FromQuery] int? limit = 20, [FromQuery] Guid? userId = null)
    {
        int effectiveOffset = offset.GetValueOrDefault(0);
        int effectiveLimit = limit.GetValueOrDefault(20);

        var (reservations, totalCount) = await _reservationService.GetAllAsync(effectiveOffset, effectiveLimit, userId);

        var responseDTO = _reservationMapper.ToReservationListResponse(reservations, totalCount, effectiveOffset, effectiveLimit);

        return responseDTO;
    }

    public override async Task<ActionResult<ReservationOutput>> ReservationsGet([BindRequired] Guid bookId, [BindRequired] Guid reservationId)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(bookId, reservationId);

        var reservationOutput = _reservationMapper.ToReservationOutput(reservation);

        return reservationOutput;
    }

    public override async Task<ActionResult<ReservationOutput>> ReservationsPost([BindRequired] Guid bookId)
    {
        var userIdString = User.FindFirst(CustomClaimTypes.InternalUserId)?.Value;
        if (userIdString is null || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("User ID claim not found.");
        }
        var newReservation = await _reservationService.CreateReservationAsync(bookId, userId);

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