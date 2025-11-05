           using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookApi.NET.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; init; }

    [BsonRequired]
    public string ExternalId { get; private set; }

    [BsonRequired]
    public string Email { get; set; }

    public string FullName { get; set; }

    [BsonRequired]
    public string Role { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    public User(string externalId, string email, string fullname)
    {
        Id = Guid.NewGuid();
        ExternalId = externalId;
        Email = email;
        FullName = fullname;
        Role = "User";
        CreatedAt = DateTime.UtcNow;
    }
}