using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookApi.NET.Models;

public class Book
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; init; }

    public String Title { get; set; }

    public String Author { get; set; }

    public String Synopsis { get; set; }


    [JsonConstructor]
    public Book()
    {
        Id = Guid.NewGuid();
        Title = String.Empty;
        Author = String.Empty;
        Synopsis = String.Empty;
    }

    public Book(string title, string author, string synopsis)
    {
        Id = Guid.NewGuid();
        Title = title;
        Author = author;
        Synopsis = synopsis ?? string.Empty;
    }
}