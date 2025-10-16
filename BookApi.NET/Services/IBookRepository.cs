using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;

namespace BookApi.NET.Services;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id);
    Task CreateAsync(Book newBook);
    Task UpdateAsync(Book updatedBook);
}