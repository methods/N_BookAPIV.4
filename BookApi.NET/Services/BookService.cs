using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;

namespace BookApi.NET.Services;

public class BookNotFoundException : Exception
{
    public BookNotFoundException(Guid Id) : base($"Book not found with id: {Id}") { }
}

public class BookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<Book> GetBookByIdAsync(Guid Id)
    {
        var book = await _bookRepository.GetByIdAsync(Id);
        if (book is null)
        {
            throw new BookNotFoundException(Id);
        }
        return book;
    }

    public async Task<Book> CreateBookAsync(BookInput bookInput)
    {
        Book newBook = new Book(bookInput.Title, bookInput.Author, bookInput.Synopsis);
        await _bookRepository.CreateAsync(newBook);

        return newBook;
    }

    public async Task<Book> UpdateBookAsync(BookInput bookInput, Guid Id)
    {
        var bookToUpdate = await GetBookByIdAsync(Id);

        bookToUpdate.Title = bookInput.Title;
        bookToUpdate.Author = bookInput.Author;
        bookToUpdate.Synopsis = bookInput.Synopsis;

        await _bookRepository.UpdateAsync(bookToUpdate);
        return bookToUpdate;
    }

    public async Task DeleteBookAsync(Guid Id)
    {
        bool bookDeleted = await _bookRepository.DeleteAsync(Id);
        if (!bookDeleted)
        {
            throw new BookNotFoundException(Id);
        }
    }
}