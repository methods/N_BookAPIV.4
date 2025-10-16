using Microsoft.AspNetCore.Mvc;
using BookApi.NET.Controllers.Generated;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BookApi.NET.Services;
using BookApi.NET.Models;

namespace BookApi.NET.Controllers;

[ApiController]
public class BooksController : BooksControllerBase
{
    private readonly BookService _bookService;
    private readonly BookMapper _bookMapper;
    public BooksController(BookService bookService, BookMapper bookMapper)
    {
        _bookService = bookService;
        _bookMapper = bookMapper;
    }

    public override Task<IActionResult> BooksDelete([BindRequired] Guid bookId)
    {
        throw new NotImplementedException();
    }

    public override async Task<ActionResult<Generated.BookOutput>> BooksGet([BindRequired] Guid bookId)
    {
        var book = await _bookService.GetBookByIdAsync(bookId);

        var bookOutput = _bookMapper.ToBookOutput(book);

        return bookOutput;
    }

    public override Task<ActionResult<Generated.BookListResponse>> BooksGet([FromQuery] int? offset = 0, [FromQuery] int? limit = 20)
    {
        throw new NotImplementedException();
    }


    public override async Task<ActionResult<Generated.BookOutput>> BooksPost([BindRequired, FromBody] Generated.BookInput body)
    {
        var createdBook = await _bookService.CreateBookAsync(body);

        var bookOutput = _bookMapper.ToBookOutput(createdBook);

        return CreatedAtAction(nameof(BooksGet), new { bookId = bookOutput.Id }, bookOutput);
    }

    public override Task<ActionResult<Generated.BookOutput>> BooksPut([BindRequired, FromBody] Generated.BookInput body, [BindRequired] Guid bookId)
    {
        throw new NotImplementedException();
    }
}