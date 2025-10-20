using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;

namespace BookApi.NET.Services;

public class BookMapper
{
    public BookOutput ToBookOutput(Book book)
    {
        return new BookOutput
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Synopsis = book.Synopsis,
            Links = null
        };
    }

    public BookListResponse ToBookListResponse(
            List<Book> books,
            long totalCount,
            int offset,
            int limit)
    {
        // Convert the list of Books to a list of BookOutput DTOs
        var bookDtos = books.Select(ToBookOutput).ToList();

        return new BookListResponse
        {
            Items = bookDtos,
            TotalCount = (int)totalCount,
            Offset = offset,
            Limit = limit
        };
    }
}