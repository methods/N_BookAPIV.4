using Microsoft.AspNetCore.Mvc;
using BookApi.NET.Controllers.Generated;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BookApi.NET.Services;
using BookApi.NET.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BookApi.NET.Controllers;

[ApiController]
[Authorize]
public class BooksController : BooksControllerBase
{
    private readonly BookService _bookService;
    private readonly BookMapper _bookMapper;
    public BooksController(BookService bookService, BookMapper bookMapper)
    {
        _bookService = bookService;
        _bookMapper = bookMapper;
    }

    [Authorize(Roles = "Admin")]
    public override async Task<IActionResult> BooksDelete([BindRequired] Guid bookId)
    {
        await _bookService.DeleteBookAsync(bookId);
        return NoContent();
    }

    [AllowAnonymous]
    public override async Task<ActionResult<Generated.BookOutput>> BooksGet([BindRequired] Guid bookId)
    {
        var book = await _bookService.GetBookByIdAsync(bookId);

        var bookOutput = _bookMapper.ToBookOutput(book);

        return bookOutput;
    }

    [AllowAnonymous]
    public override async Task<ActionResult<Generated.BookListResponse>> BooksGet([FromQuery] int? offset = 0, [FromQuery] int? limit = 20)
    {

        int effectiveOffset = offset ?? 0;
        int effectiveLimit = limit ?? 0;

        var (books, totalCount) = await _bookService.GetBooksAsync(effectiveOffset, effectiveLimit);

        var responseDTO = _bookMapper.ToBookListResponse(books, totalCount, effectiveOffset, effectiveLimit);

        return responseDTO;
    }

    public override async Task<ActionResult<Generated.BookOutput>> BooksPost([BindRequired, FromBody] Generated.BookInput body)
    {
        var createdBook = await _bookService.CreateBookAsync(body);

        var bookOutput = _bookMapper.ToBookOutput(createdBook);

        return CreatedAtAction(nameof(BooksGet), new { bookId = bookOutput.Id }, bookOutput);
    }

    public override async Task<ActionResult<Generated.BookOutput>> BooksPut([BindRequired, FromBody] Generated.BookInput body, [BindRequired] Guid bookId)
    {
        var updatedBook = await _bookService.UpdateBookAsync(body, bookId);

        var bookOutput = _bookMapper.ToBookOutput(updatedBook);

        return bookOutput;
    }
}