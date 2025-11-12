using BookApi.NET.Controllers.Generated;
using BookApi.NET.Models;
using DnsClient.Protocol;

namespace BookApi.NET.Services;

public class BookMapper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BookMapper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public BookOutput ToBookOutput(Book book)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return BookOutputWithoutLinks(book); // In case of a non-context request scenario
        }

        var request = httpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var links = new BookLinks
        {
            Self = $"{baseUrl}/books/{book.Id}",
            Reservations = $"{baseUrl}/books/{book.Id}/reservations"
        };
        return new BookOutput
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Synopsis = book.Synopsis,
            Links = links
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

    private BookOutput BookOutputWithoutLinks(Book book)
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
}