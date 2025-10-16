using BookApi.NET.Controllers.Generated;
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

    public async Task UpdateAsync(Book updatedBook)
    {
        // Set a filter for the Book that is to be updated
        var filter = Builders<Book>.Filter.Eq(b => b.Id, updatedBook.Id);

        // Set the update that is to be carried out
        var update = Builders<Book>.Update
            .Set(b => b.Title, updatedBook.Title)
            .Set(b => b.Author, updatedBook.Author)
            .Set(b => b.Synopsis, updatedBook.Synopsis);

        await _booksCollection.UpdateOneAsync(filter, update);
    }
}