using BookApi.NET.Models;
using MongoDB.Driver;

namespace BookApi.NET.Services;

public class BookRepository : IBookRepository
{
    private readonly IMongoCollection<Book> _booksCollection;

    public BookRepository()
    {
        var mongoClient = new MongoClient("mongodb://localhost:27017");
        var mongoDatabase = mongoClient.GetDatabase("book-api-dotnet-dev");
        _booksCollection = mongoDatabase.GetCollection<Book>("books");
    }

    public async Task<Book?> GetByIdAsync(Guid id) =>
        await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Book newBook) =>
        await _booksCollection.InsertOneAsync(newBook);
}