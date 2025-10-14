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
}