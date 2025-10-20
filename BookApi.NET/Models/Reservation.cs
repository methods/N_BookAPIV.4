using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookApi.NET.Models;

public class Reservation
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; init; }

    [BsonRepresentation(BsonType.String)]
    public Guid BookId { get; private set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; private set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReservedAt { get; private set; }

    [BsonRepresentation(BsonType.String)]
    public ReservationStatus Status { get; private set; }

    public Reservation()
    {
        Id = Guid.NewGuid();
        BookId = Guid.Empty;
        UserId = Guid.Empty;
        ReservedAt = DateTime.UtcNow;
        Status = ReservationStatus.Active;
    }

    public Reservation(Guid bookId, Guid userId)
    {
        if (bookId == Guid.Empty)
            throw new ArgumentException("Book ID cannot be empty.", nameof(bookId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        Id = Guid.NewGuid();
        BookId = bookId;
        UserId = userId;
        ReservedAt = DateTime.Now;
        Status = ReservationStatus.Active;
    }
    
    public void Cancel ()
    {
        if (Status != ReservationStatus.Active)
        {
            throw new InvalidOperationException("Only an active reservation can be cancelled.");
        }
        Status = ReservationStatus.Cancelled;
    }
}