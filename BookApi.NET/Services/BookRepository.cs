using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using MongoDB.Driver;

namespace BookApi.NET.Services;

public class BookRepository : IBookRepository
{
    private readonly IMongoCollection<Book> _booksCollection;

    public BookRepository(IOptions<BookstoreDbSettings> dbSettings, IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
        _booksCollection = database.GetCollection<Book>("books");
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

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Set a filter for the Book that is to be deleted
        var filter = Builders<Book>.Filter.Eq(b => b.Id, id);

        var result = await _booksCollection.DeleteOneAsync(filter);

        return result.DeletedCount == 1;
    }

    public async Task<(List<Book> Books, long TotalCount)> GetAllAsync(int offset, int limit)
    {
        var totalCount = await _booksCollection.CountDocumentsAsync(FilterDefinition<Book>.Empty);

        var books = await _booksCollection.Find(FilterDefinition<Book>.Empty)
            .Skip(offset)
            .Limit(limit)
            .ToListAsync();

        return (books, totalCount);
    }
}